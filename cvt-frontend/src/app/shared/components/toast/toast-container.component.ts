import { CommonModule } from '@angular/common';
import { Component, inject } from '@angular/core';
import { ToastService } from '../../../core/services/toast.service';

@Component({
  selector: 'app-toast-container',
  standalone: true,
  imports: [CommonModule],
  template: `
    <div class="fixed top-4 right-4 z-[9999] flex flex-col gap-2 w-[min(360px,calc(100vw-2rem))]">
      @for (toast of toastService.toasts(); track toast.id) {
        <div
          class="flex items-start gap-3 rounded-lg border px-4 py-3 shadow-sm bg-white animate-toast-in"
          [class.border-green-200]="toast.type === 'success'"
          [class.border-red-200]="toast.type === 'error'"
          [class.border-ink-200]="toast.type === 'info'"
        >
          <span
            class="mt-0.5 h-2 w-2 shrink-0 rounded-full"
            [class.bg-green-500]="toast.type === 'success'"
            [class.bg-red-500]="toast.type === 'error'"
            [class.bg-ink-400]="toast.type === 'info'"
          ></span>
          <p class="flex-1 text-sm text-ink-700">{{ toast.message }}</p>
          <button
            type="button"
            class="text-ink-400 hover:text-ink-600 text-sm leading-none"
            (click)="toastService.dismiss(toast.id)"
            aria-label="Fermer la notification"
          >
            &times;
          </button>
        </div>
      }
    </div>
  `,
  styles: [
    `
      @keyframes toast-in {
        from {
          opacity: 0;
          transform: translateY(-6px);
        }
        to {
          opacity: 1;
          transform: translateY(0);
        }
      }
      .animate-toast-in {
        animation: toast-in 0.18s ease-out;
      }
    `,
  ],
})
export class ToastContainerComponent {
  protected readonly toastService = inject(ToastService);
}
