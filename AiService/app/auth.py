import jwt
from fastapi import Header, HTTPException

from app.config import settings

# Claim utilisé par AuthService pour l'identifiant utilisateur
# (ClaimTypes.NameIdentifier). .NET sérialise ce type de claim dans le JWT
# sous cette URI complète (voir AuthService/Services/Implementations/AuthServiceImpl.cs).
_NAME_IDENTIFIER_CLAIM = "http://schemas.xmlsoap.org/ws/2005/05/identity/claims/nameidentifier"


def get_current_user_id(authorization: str | None = Header(default=None)) -> str:
    """
    Extrait l'identifiant utilisateur du token JWT émis par AuthService.

    Le token est déjà validé une première fois par l'ApiGateway (Ocelot),
    mais on le revalide ici par défense en profondeur : rien n'empêche
    d'appeler AiService directement (dev local, réseau interne Docker),
    et c'est cette validation qui nous permet de récupérer l'identifiant
    utilisateur pour cloisonner l'historique des conversations.
    """
    if not authorization or not authorization.lower().startswith("bearer "):
        raise HTTPException(status_code=401, detail="Authentification requise.")

    token = authorization.split(" ", 1)[1].strip()

    try:
        payload = jwt.decode(
            token,
            settings.jwt_key,
            algorithms=["HS256"],
            audience=settings.jwt_audience,
            issuer=settings.jwt_issuer,
        )
    except jwt.PyJWTError as exc:
        raise HTTPException(status_code=401, detail="Token invalide ou expiré.") from exc

    user_id = (
        payload.get(_NAME_IDENTIFIER_CLAIM)
        or payload.get("nameid")
        or payload.get("sub")
    )

    if not user_id:
        raise HTTPException(
            status_code=401,
            detail="Token invalide : identifiant utilisateur manquant.",
        )

    return str(user_id)
