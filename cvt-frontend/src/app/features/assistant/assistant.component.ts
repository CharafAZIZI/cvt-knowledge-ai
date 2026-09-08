import { CommonModule } from '@angular/common';
import { AfterViewChecked, Component, ElementRef, ViewChild, inject, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { AiService } from '../../core/services/ai.service';
import { ChatMessage } from '../../core/models/ai.model';
import { extractErrorMessage } from '../../core/utils/http-error.util';

@Component({
  selector: 'app-assistant',
  standalone: true,
  imports: [CommonModule, FormsModule],
  templateUrl: './assistant.component.html',
})
export class AssistantComponent implements AfterViewChecked {
  private readonly aiService = inject(AiService);

  @ViewChild('scrollAnchor') private scrollAnchor?: ElementRef<HTMLDivElement>;

  readonly messages = signal<ChatMessage[]>([]);
  questionInput = '';
  readonly isWaitingForAnswer = signal(false);

  private shouldScrollToBottom = false;

  ngAfterViewChecked(): void {
    if (this.shouldScrollToBottom) {
      this.scrollAnchor?.nativeElement.scrollIntoView({ behavior: 'smooth' });
      this.shouldScrollToBottom = false;
    }
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

    this.messages.update((messages) => [
      ...messages,
      { role: 'user', content: question, createdAt: new Date() },
    ]);
    this.questionInput = '';
    this.isWaitingForAnswer.set(true);
    this.shouldScrollToBottom = true;

    this.aiService.ask(question).subscribe({
      next: (response) => {
        this.messages.update((messages) => [
          ...messages,
          {
            role: 'assistant',
            content: response.answer,
            sources: response.sources,
            createdAt: new Date(),
          },
        ]);
        this.isWaitingForAnswer.set(false);
        this.shouldScrollToBottom = true;
      },
      error: (error) => {
        this.messages.update((messages) => [
          ...messages,
          {
            role: 'assistant',
            content: extractErrorMessage(
              error,
              "L'assistant est momentanément indisponible. Réessayez dans un instant.",
            ),
            isError: true,
            createdAt: new Date(),
          },
        ]);
        this.isWaitingForAnswer.set(false);
        this.shouldScrollToBottom = true;
      },
    });
  }
}
