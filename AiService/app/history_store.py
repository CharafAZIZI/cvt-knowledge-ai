import json
import os
import sqlite3
import threading
from datetime import datetime, timezone

from app.config import settings

# SQLite + un verrou applicatif suffit largement ici : volume d'écritures
# faible (un assistant documentaire interne), et ça évite d'ajouter une
# dépendance (SQL Server, etc.) juste pour stocker des conversations.
_lock = threading.Lock()


def _connect() -> sqlite3.Connection:
    os.makedirs(os.path.dirname(settings.chat_history_db_path), exist_ok=True)
    conn = sqlite3.connect(settings.chat_history_db_path, check_same_thread=False)
    conn.row_factory = sqlite3.Row
    conn.execute("PRAGMA foreign_keys = ON")
    return conn


_conn = _connect()


def init_db() -> None:
    with _lock:
        _conn.executescript(
            """
            CREATE TABLE IF NOT EXISTS conversations (
                id INTEGER PRIMARY KEY AUTOINCREMENT,
                user_id TEXT NOT NULL,
                title TEXT NOT NULL,
                created_at TEXT NOT NULL,
                updated_at TEXT NOT NULL
            );

            CREATE TABLE IF NOT EXISTS messages (
                id INTEGER PRIMARY KEY AUTOINCREMENT,
                conversation_id INTEGER NOT NULL
                    REFERENCES conversations(id) ON DELETE CASCADE,
                role TEXT NOT NULL CHECK (role IN ('user', 'assistant')),
                content TEXT NOT NULL,
                sources_json TEXT,
                created_at TEXT NOT NULL
            );

            CREATE INDEX IF NOT EXISTS idx_conversations_user
                ON conversations(user_id);

            CREATE INDEX IF NOT EXISTS idx_messages_conversation
                ON messages(conversation_id);
            """
        )
        _conn.commit()


def _now() -> str:
    return datetime.now(timezone.utc).isoformat()


def _title_from_question(question: str, max_len: int = 60) -> str:
    """Le titre affiché dans la liste des conversations = début de la
    première question (comme ChatGPT), tronqué proprement."""
    question = " ".join(question.split())
    if len(question) <= max_len:
        return question
    return question[: max_len - 1].rstrip() + "…"


def create_conversation(user_id: str, first_question: str) -> int:
    with _lock:
        now = _now()
        cursor = _conn.execute(
            "INSERT INTO conversations (user_id, title, created_at, updated_at) "
            "VALUES (?, ?, ?, ?)",
            (user_id, _title_from_question(first_question), now, now),
        )
        _conn.commit()
        return cursor.lastrowid


def touch_conversation(conversation_id: int) -> None:
    with _lock:
        _conn.execute(
            "UPDATE conversations SET updated_at = ? WHERE id = ?",
            (_now(), conversation_id),
        )
        _conn.commit()


def get_conversation_owner(conversation_id: int) -> str | None:
    row = _conn.execute(
        "SELECT user_id FROM conversations WHERE id = ?",
        (conversation_id,),
    ).fetchone()
    return row["user_id"] if row else None


def list_conversations(user_id: str) -> list[dict]:
    rows = _conn.execute(
        "SELECT id, title, created_at, updated_at FROM conversations "
        "WHERE user_id = ? ORDER BY updated_at DESC",
        (user_id,),
    ).fetchall()
    return [dict(row) for row in rows]


def get_messages(conversation_id: int) -> list[dict]:
    rows = _conn.execute(
        "SELECT role, content, sources_json, created_at FROM messages "
        "WHERE conversation_id = ? ORDER BY id ASC",
        (conversation_id,),
    ).fetchall()

    messages = []
    for row in rows:
        message = dict(row)
        sources_json = message.pop("sources_json")
        message["sources"] = json.loads(sources_json) if sources_json else []
        messages.append(message)

    return messages


def add_message(
    conversation_id: int,
    role: str,
    content: str,
    sources: list[dict] | None = None,
) -> None:
    with _lock:
        _conn.execute(
            "INSERT INTO messages "
            "(conversation_id, role, content, sources_json, created_at) "
            "VALUES (?, ?, ?, ?, ?)",
            (
                conversation_id,
                role,
                content,
                json.dumps(sources, ensure_ascii=False) if sources else None,
                _now(),
            ),
        )
        _conn.commit()


def delete_conversation(conversation_id: int) -> None:
    with _lock:
        _conn.execute("DELETE FROM conversations WHERE id = ?", (conversation_id,))
        _conn.commit()


init_db()
