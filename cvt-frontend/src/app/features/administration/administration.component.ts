import { CommonModule } from '@angular/common';
import { Component, OnInit, inject, signal } from '@angular/core';
import { FormControl, ReactiveFormsModule } from '@angular/forms';
import { AuthService } from '../../core/services/auth.service';
import { UserService } from '../../core/services/user.service';
import { ToastService } from '../../core/services/toast.service';
import { User } from '../../core/models/user.model';
import { extractErrorMessage } from '../../core/utils/http-error.util';
import { UserFormModalComponent } from './components/user-form-modal/user-form-modal.component';
import { ConfirmModalComponent } from '../../shared/components/confirm-modal/confirm-modal.component';

type PendingAction = { type: 'delete' | 'toggle'; user: User };

@Component({
  selector: 'app-administration',
  standalone: true,
  imports: [CommonModule, ReactiveFormsModule, UserFormModalComponent, ConfirmModalComponent],
  templateUrl: './administration.component.html',
})
export class AdministrationComponent implements OnInit {
  private readonly userService = inject(UserService);
  private readonly toastService = inject(ToastService);
  protected readonly authService = inject(AuthService);

  readonly searchControl = new FormControl('', { nonNullable: true });

  readonly isLoading = signal(true);
  readonly errorMessage = signal<string | null>(null);
  readonly users = signal<User[]>([]);

  readonly isFormModalOpen = signal(false);
  readonly userBeingEdited = signal<User | null>(null);

  readonly pendingAction = signal<PendingAction | null>(null);
  readonly isProcessingAction = signal(false);

  readonly filteredUsers = () => {
    const term = this.searchControl.value.trim().toLowerCase();
    if (!term) {
      return this.users();
    }
    return this.users().filter(
      (u) =>
        u.firstName.toLowerCase().includes(term) ||
        u.lastName.toLowerCase().includes(term) ||
        u.email.toLowerCase().includes(term),
    );
  };

  ngOnInit(): void {
    this.loadUsers();
  }

  private loadUsers(): void {
    this.isLoading.set(true);
    this.errorMessage.set(null);

    this.userService.getAll().subscribe({
      next: (users) => {
        this.users.set(users);
        this.isLoading.set(false);
      },
      error: (error) => {
        this.errorMessage.set(
          extractErrorMessage(error, 'Impossible de charger les administrateurs.'),
        );
        this.isLoading.set(false);
      },
    });
  }

  openCreateModal(): void {
    this.userBeingEdited.set(null);
    this.isFormModalOpen.set(true);
  }

  openEditModal(user: User): void {
    this.userBeingEdited.set(user);
    this.isFormModalOpen.set(true);
  }

  closeFormModal(): void {
    this.isFormModalOpen.set(false);
    this.userBeingEdited.set(null);
  }

  onUserSaved(): void {
    const wasEdit = !!this.userBeingEdited();
    this.closeFormModal();
    this.toastService.success(
      wasEdit ? 'Administrateur mis à jour avec succès.' : 'Administrateur ajouté avec succès.',
    );
    this.loadUsers();
  }

  askDeleteConfirmation(user: User): void {
    this.pendingAction.set({ type: 'delete', user });
  }

  askToggleConfirmation(user: User): void {
    this.pendingAction.set({ type: 'toggle', user });
  }

  cancelPendingAction(): void {
    this.pendingAction.set(null);
  }

  confirmPendingAction(): void {
    const action = this.pendingAction();
    if (!action) {
      return;
    }

    this.isProcessingAction.set(true);

    const request$ =
      action.type === 'delete'
        ? this.userService.delete(action.user.id)
        : action.user.isActive
          ? this.userService.deactivate(action.user.id)
          : this.userService.activate(action.user.id);

    request$.subscribe({
      next: () => {
        this.isProcessingAction.set(false);
        this.pendingAction.set(null);
        this.toastService.success(
          action.type === 'delete'
            ? 'Administrateur supprimé avec succès.'
            : action.user.isActive
              ? 'Administrateur désactivé avec succès.'
              : 'Administrateur activé avec succès.',
        );
        this.loadUsers();
      },
      error: (error) => {
        this.isProcessingAction.set(false);
        this.toastService.error(extractErrorMessage(error, "L'opération a échoué."));
        this.pendingAction.set(null);
      },
    });
  }

  isCurrentUser(user: User): boolean {
    return this.authService.currentUser()?.id === user.id;
  }
}
