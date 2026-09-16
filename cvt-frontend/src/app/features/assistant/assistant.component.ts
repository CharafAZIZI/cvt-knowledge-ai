import { CommonModule } from '@angular/common';
import {
  AfterViewChecked,
  Component,
  ElementRef,
  OnInit,
  ViewChild,
  inject,
  signal,
} from '@angular/core';
import { FormsModule } from '@angular/forms';
import { AiService } from '../../core/services/ai.service';
import { AskStreamEvent, ChatMessage, ConversationSummary } from '../../core/models/ai.model';
import { ConfirmModalComponent } from '../../shared/components/confirm-modal/confirm-modal.component';
import { ToastService } from '../../core/services/toast.service';
import { extractErrorMessage } from '../../core/utils/http-error.util';

const GENERIC_ERROR = "L'assistant est momentanément indisponible. Réessayez dans un instant.";

@Component({
  selector: 'app-assistant',
  standalone: true,
  imports: [CommonModule, FormsModule, ConfirmModalComponent],
  templateUrl: './assistant.component.html',
})
export class AssistantComponent implements OnInit, AfterViewChecked {
  private readonly aiService = inject(AiService);
  private readonly toastService = inject(ToastService);

  @ViewChild('scrollAnchor') private scrollAnchor?: ElementRef<HTMLDivElement>;

  readonly conversations = signal<ConversationSummary[]>([]);
  readonly activeConversationId = signal<number | null>(null);
  readonly messages = signal<ChatMessage[]>([]);
  readonly isLoadingConversations = signal(false);
  readonly isLoadingConversation = signal(false);
  readonly isWaitingForAnswer = signal(false);

  // Suppression (avec confirmation) d'une conversation ou de tout l'historique
  readonly conversationPendingDeletion = signal<ConversationSummary | null>(null);
  readonly isDeleteAllConfirmOpen = signal(false);
  readonly isDeleting = signal(false);

  questionInput = '';

  private shouldScrollToBottom = false;

  ngOnInit(): void {
    this.loadConversations();
  }

  ngAfterViewChecked(): void {
    if (this.shouldScrollToBottom) {
      this.scrollAnchor?.nativeElement.scrollIntoView({ behavior: 'smooth' });
      this.shouldScrollToBottom = false;
    }
  }

  loadConversations(): void {
    this.isLoadingConversations.set(true);
    this.aiService.listConversations().subscribe({
      next: (conversations) => {
        this.conversations.set(conversations);
        this.isLoadingConversations.set(false);
      },
      error: () => this.isLoadingConversations.set(false),
    });
  }

  startNewConversation(): void {
    if (this.isWaitingForAnswer()) {
      return;
    }
    this.activeConversationId.set(null);
    this.messages.set([]);
    this.questionInput = '';
  }

  openConversation(conversation: ConversationSummary): void {
    if (this.isWaitingForAnswer() || this.activeConversationId() === conversation.id) {
      return;
    }

    this.activeConversationId.set(conversation.id);
    this.isLoadingConversation.set(true);

    this.aiService.getConversation(conversation.id).subscribe({
      next: (detail) => {
        this.messages.set(
          detail.messages.map((message) => ({
            role: message.role,
            content: message.content,
            sources: message.sources,
            createdAt: new Date(message.created_at),
          })),
        );
        this.isLoadingConversation.set(false);
        this.shouldScrollToBottom = true;
      },
      error: () => {
        this.isLoadingConversation.set(false);
      },
    });
  }

  askDeleteConfirmation(conversation: ConversationSummary, event: Event): void {
    event.stopPropagation();
    this.conversationPendingDeletion.set(conversation);
  }

  cancelDeletion(): void {
    this.conversationPendingDeletion.set(null);
  }

  confirmDeletion(): void {
    const conversation = this.conversationPendingDeletion();
    if (!conversation) {
      return;
    }

    this.isDeleting.set(true);

    this.aiService.deleteConversation(conversation.id).subscribe({
      next: () => {
        this.isDeleting.set(false);
        this.conversationPendingDeletion.set(null);
        this.conversations.update((list) => list.filter((c) => c.id !== conversation.id));
        if (this.activeConversationId() === conversation.id) {
          this.startNewConversation();
        }
        this.toastService.success('Conversation supprimée.');
      },
      error: (error) => {
        this.isDeleting.set(false);
        this.toastService.error(extractErrorMessage(error, 'La suppression a échoué.'));
      },
    });
  }

  askDeleteAllConfirmation(): void {
    if (this.conversations().length === 0) {
      return;
    }
    this.isDeleteAllConfirmOpen.set(true);
  }

  cancelDeleteAll(): void {
    this.isDeleteAllConfirmOpen.set(false);
  }

  confirmDeleteAll(): void {
    this.isDeleting.set(true);

    this.aiService.deleteAllConversations().subscribe({
      next: () => {
        this.isDeleting.set(false);
        this.isDeleteAllConfirmOpen.set(false);
        this.conversations.set([]);
        this.startNewConversation();
        this.toastService.success('Toutes les conversations ont été supprimées.');
      },
      error: (error) => {
        this.isDeleting.set(false);
        this.toastService.error(extractErrorMessage(error, 'La suppression a échoué.'));
      },
    });
  }

  onKeydown(event: KeyboardEvent): void {
    if (event.key === 'Enter' && !event.shiftKey) {
      event.preventDefault();
      this.sendQuestion();
    }
  }

  sendQuestion(): void {
    const question = this.questionInput.trim();
    if (!question || this.isWaitingForAnswer()) {
      return;
    }

    const isNewConversation = this.activeConversationId() === null;

    this.messages.update((messages) => [
      ...messages,
      { role: 'user', content: question, createdAt: new Date() },
    ]);
    this.questionInput = '';
    this.isWaitingForAnswer.set(true);
    this.shouldScrollToBottom = true;

    // Bulle assistant affichée immédiatement, remplie token par token au
    // fur et à mesure du flux SSE.
    const assistantIndex = this.messages().length;
    this.messages.update((messages) => [
      ...messages,
      { role: 'assistant', content: '', createdAt: new Date() },
    ]);

    this.aiService
      .streamAsk(question, this.activeConversationId(), (event) =>
        this.handleStreamEvent(event, assistantIndex, isNewConversation),
      )
      .catch(() => this.applyErrorToMessage(assistantIndex, GENERIC_ERROR));
  }

  private handleStreamEvent(
    event: AskStreamEvent,
    assistantIndex: number,
    isNewConversation: boolean,
  ): void {
    switch (event.type) {
      case 'start':
        if (event.conversation_id != null) {
          this.activeConversationId.set(event.conversation_id);
        }
        break;

      case 'token':
        this.messages.update((messages) =>
          messages.map((message, index) =>
            index === assistantIndex
              ? { ...message, content: message.content + (event.content ?? '') }
              : message,
          ),
        );
        this.shouldScrollToBottom = true;
        break;

      case 'done':
        this.messages.update((messages) =>
          messages.map((message, index) =>
            index === assistantIndex ? { ...message, sources: event.sources ?? [] } : message,
          ),
        );
        this.isWaitingForAnswer.set(false);
        if (isNewConversation) {
          // La première question vient de créer une conversation côté
          // serveur : on rafraîchit la liste pour la faire apparaître.
          this.loadConversations();
        }
        break;

      case 'error':
        this.applyErrorToMessage(assistantIndex, event.detail ?? GENERIC_ERROR);
        break;
    }
  }

  private applyErrorToMessage(assistantIndex: number, detail: string): void {
    this.messages.update((messages) =>
      messages.map((message, index) =>
        index === assistantIndex ? { ...message, content: detail, isError: true } : message,
      ),
    );
    this.isWaitingForAnswer.set(false);
  }
}