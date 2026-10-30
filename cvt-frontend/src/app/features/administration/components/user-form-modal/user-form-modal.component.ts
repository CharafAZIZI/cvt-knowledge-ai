import { CommonModule } from '@angular/common';
import { HttpErrorResponse } from '@angular/common/http';
import { Component, EventEmitter, Input, OnInit, Output, inject, signal } from '@angular/core';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { UserService } from '../../../../core/services/user.service';
import { User } from '../../../../core/models/user.model';
import { extractErrorMessage } from '../../../../core/utils/http-error.util';

@Component({
  selector: 'app-user-form-modal',
  standalone: true,
  imports: [CommonModule, ReactiveFormsModule],
  templateUrl: './user-form-modal.component.html',
})
export class UserFormModalComponent implements OnInit {
  private readonly fb = inject(FormBuilder);
  private readonly userService = inject(UserService);

  @Input() user: User | null = null;
  @Output() closed = new EventEmitter<void>();
  @Output() saved = new EventEmitter<User>();

  readonly isSubmitting = signal(false);
  readonly errorMessage = signal<string | null>(null);

  readonly form = this.fb.nonNullable.group({
    firstName: ['', [Validators.required, Validators.maxLength(100)]],
    lastName: ['', [Validators.required, Validators.maxLength(100)]],
    email: ['', [Validators.required, Validators.email, Validators.maxLength(255)]],
    password: [''],
  });

  get isEditMode(): boolean {
    return !!this.user;
  }

  ngOnInit(): void {
    if (this.user) {
      this.form.patchValue({
        firstName: this.user.firstName,
        lastName: this.user.lastName,
        email: this.user.email,
      });
      // En modification, le mot de passe n'est pas géré ici (endpoint dédié reset/own password).
      this.form.controls.password.clearValidators();
    } else {
      this.form.controls.password.setValidators([Validators.required, Validators.minLength(8)]);
    }
    this.form.controls.password.updateValueAndValidity();
  }

  submit(): void {
    this.errorMessage.set(null);

    if (this.form.invalid) {
      this.form.markAllAsTouched();
      return;
    }

    this.isSubmitting.set(true);
    const { firstName, lastName, email, password } = this.form.getRawValue();

    const request$ = this.isEditMode
      ? this.userService.update(this.user!.id, { firstName, lastName, email })
      : this.userService.create({ firstName, lastName, email, password });

    request$.subscribe({
      next: (user) => {
        this.isSubmitting.set(false);
        this.saved.emit(user);
      },
      error: (error: HttpErrorResponse) => {
        this.isSubmitting.set(false);
        this.errorMessage.set(
          extractErrorMessage(error, "Une erreur est survenue lors de l'enregistrement."),
        );
      },
    });
  }

  close(): void {
    this.closed.emit();
  }
}
