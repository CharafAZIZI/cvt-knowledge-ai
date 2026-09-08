import { CommonModule } from '@angular/common';
import { Component, OnInit, inject, signal } from '@angular/core';
import { FormControl, ReactiveFormsModule } from '@angular/forms';
import { debounceTime, distinctUntilChanged } from 'rxjs';
import { AuthService } from '../../core/services/auth.service';
import { DocumentService } from '../../core/services/document.service';
import { ToastService } from '../../core/services/toast.service';
import { CvtDocument } from '../../core/models/document.model';
import { extractErrorMessage } from '../../core/utils/http-error.util';
import { DocumentFormModalComponent } from './components/document-form-modal/document-form-modal.component';
import { ConfirmModalComponent } from '../../shared/components/confirm-modal/confirm-modal.component';

const PAGE_SIZE = 10;

@Component({
  selector: 'app-documents',
  standalone: true,
  imports: [CommonModule, ReactiveFormsModule, DocumentFormModalComponent, ConfirmModalComponent],
  templateUrl: './documents.component.html',
})
export class DocumentsComponent implements OnInit {
  private readonly documentService = inject(DocumentService);
  private readonly toastService = inject(ToastService);
  protected readonly authService = inject(AuthService);

  readonly searchControl = new FormControl('', { nonNullable: true });

  readonly isLoading = signal(true);
  readonly errorMessage = signal<string | null>(null);
  readonly documents = signal<CvtDocument[]>([]);
  readonly currentPage = signal(1);

  readonly isFormModalOpen = signal(false);
  readonly documentBeingEdited = signal<CvtDocument | null>(null);

  readonly documentPendingDeletion = signal<CvtDocument | null>(null);
  readonly isDeleting = signal(false);

  readonly downloadingId = signal<number | null>(null);

  readonly pagedDocuments = () => {
    const start = (this.currentPage() - 1) * PAGE_SIZE;
    return this.documents().slice(start, start + PAGE_SIZE);
  };

  readonly totalPages = () => Math.max(1, Math.ceil(this.documents().length / PAGE_SIZE));

  ngOnInit(): void {
    this.loadDocuments();

    this.searchControl.valueChanges
      .pipe(debounceTime(300), distinctUntilChanged())
      .subscribe((term) => this.runSearch(term));
  }

  private loadDocuments(): void {
    this.isLoading.set(true);
    this.errorMessage.set(null);

    this.documentService.getAll().subscribe({
      next: (documents) => {
        this.documents.set(documents);
        this.currentPage.set(1);
        this.isLoading.set(false);
      },
      error: (error) => {
        this.errorMessage.set(extractErrorMessage(error, 'Impossible de charger les documents.'));
        this.isLoading.set(false);
      },
    });
  }

  private runSearch(term: string): void {
    const trimmed = term.trim();

    if (!trimmed) {
      this.loadDocuments();
      return;
    }

    this.isLoading.set(true);
    this.errorMessage.set(null);

    this.documentService.search(trimmed).subscribe({
      next: (documents) => {
        this.documents.set(documents);
        this.currentPage.set(1);
        this.isLoading.set(false);
      },
      error: (error) => {
        this.errorMessage.set(extractErrorMessage(error, 'La recherche a échoué.'));
        this.isLoading.set(false);
      },
    });
  }

  goToPage(page: number): void {
    if (page < 1 || page > this.totalPages()) {
      return;
    }
    this.currentPage.set(page);
  }

  openCreateModal(): void {
    this.documentBeingEdited.set(null);
    this.isFormModalOpen.set(true);
  }

  openEditModal(document: CvtDocument): void {
    this.documentBeingEdited.set(document);
    this.isFormModalOpen.set(true);
  }

  closeFormModal(): void {
    this.isFormModalOpen.set(false);
    this.documentBeingEdited.set(null);
  }

  onDocumentSaved(): void {
    const wasEdit = !!this.documentBeingEdited();
    this.closeFormModal();
    this.toastService.success(wasEdit ? 'Document mis à jour avec succès.' : 'Document ajouté avec succès.');
    this.loadDocuments();
  }

  askDeleteConfirmation(document: CvtDocument): void {
    this.documentPendingDeletion.set(document);
  }

  cancelDeletion(): void {
    this.documentPendingDeletion.set(null);
  }

  confirmDeletion(): void {
    const document = this.documentPendingDeletion();
    if (!document) {
      return;
    }

    this.isDeleting.set(true);

    this.documentService.delete(document.id).subscribe({
      next: () => {
        this.isDeleting.set(false);
        this.documentPendingDeletion.set(null);
        this.toastService.success('Document supprimé avec succès.');
        this.loadDocuments();
      },
      error: (error) => {
        this.isDeleting.set(false);
        this.toastService.error(extractErrorMessage(error, 'La suppression a échoué.'));
      },
    });
  }

  downloadDocument(document: CvtDocument): void {
    this.downloadingId.set(document.id);

    this.documentService.download(document.id).subscribe({
      next: (blob) => {
        this.downloadingId.set(null);
        const url = window.URL.createObjectURL(blob);
        const link = window.document.createElement('a');
        link.href = url;
        link.download = document.fileName;
        link.click();
        window.URL.revokeObjectURL(url);
      },
      error: (error) => {
        this.downloadingId.set(null);
        this.toastService.error(extractErrorMessage(error, 'Le téléchargement a échoué.'));
      },
    });
  }
}
