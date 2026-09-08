import { CommonModule } from '@angular/common';
import { Component, EventEmitter, Input, Output } from '@angular/core';

@Component({
  selector: 'app-confirm-modal',
  standalone: true,
  imports: [CommonModule],
  template: `
    <div class="fixed inset-0 z-50 flex items-center justify-center px-4">
      <div class="absolute inset-0 bg-ink-900/40" (click)="cancel()"></div>

      <div class="relative w-full max-w-sm rounded-xl bg-white p-5 shadow-lg">
        <div class="flex items-start gap-3">
          <span
            class="flex h-10 w-10 shrink-0 items-center justify-center rounded-full"
            [class.bg-red-50]="tone === 'danger'"
            [class.bg-amber-50]="tone === 'warning'"
          >
            <svg
              xmlns="http://www.w3.org/2000/svg"
              width="20"
              height="20"
              viewBox="0 0 24 24"
              fill="none"
              [attr.stroke]="tone === 'danger' ? '#dc2626' : '#d97706'"
              stroke-width="2"
              stroke-linecap="round"
              stroke-linejoin="round"
            >
              <path d="M10.29 3.86 1.82 18a2 2 0 0 0 1.71 3h16.94a2 2 0 0 0 1.71-3L13.71 3.86a2 2 0 0 0-3.42 0Z" />
              <line x1="12" y1="9" x2="12" y2="13" />
              <line x1="12" y1="17" x2="12.01" y2="17" />
            </svg>
          </span>
          <div class="flex-1">
            <h2 class="text-sm font-semibold text-ink-900">{{ title }}</h2>
            <p class="mt-1 text-sm text-ink-500">{{ message }}</p>
          </div>
        </div>

        <div class="mt-5 flex items-center justify-end gap-2">
          <button
            type="button"
            (click)="cancel()"
            class="rounded-md border border-ink-300 px-4 py-2 text-sm font-medium text-ink-700 hover:bg-ink-50"
          >
            Annuler
          </button>
          <button
            type="button"
            (click)="confirm()"
            [disabled]="isProcessing"
            class="rounded-md px-4 py-2 text-sm font-medium text-white disabled:cursor-not-allowed disabled:opacity-60"
            [class.bg-red-600]="tone === 'danger'"
            [class.hover:bg-red-700]="tone === 'danger'"
            [class.bg-amber-600]="tone === 'warning'"
            [class.hover:bg-amber-700]="tone === 'warning'"
          >
            {{ confirmLabel }}
          </button>
        </div>
      </div>
    </div>
  `,
})
export class ConfirmModalComponent {
  @Input() title = 'Confirmer';
  @Input() message = 'Êtes-vous sûr ?';
  @Input() confirmLabel = 'Confirmer';
  @Input() tone: 'danger' | 'warning' = 'danger';
  @Input() isProcessing = false;
  @Output() confirmed = new EventEmitter<void>();
  @Output() cancelled = new EventEmitter<void>();

  confirm(): void {
    this.confirmed.emit();
  }

  cancel(): void {
    this.cancelled.emit();
  }
}
