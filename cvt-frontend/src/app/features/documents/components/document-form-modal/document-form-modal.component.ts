import { CommonModule } from '@angular/common';
import { HttpErrorResponse } from '@angular/common/http';
import { Component, EventEmitter, Input, OnInit, Output, inject, signal } from '@angular/core';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { DocumentService } from '../../../../core/services/document.service';
import { CvtDocument } from '../../../../core/models/document.model';
import { extractErrorMessage } from '../../../../core/utils/http-error.util';
import { DOCUMENT_CATEGORIES, DocumentCategory } from '../../../../core/constants/document-category.constant';

const MAX_FILE_SIZE_BYTES = 100 * 1024 * 1024; // aligné sur [RequestSizeLimit] côté DocumentsController
const ALLOWED_EXTENSIONS = ['.pdf'];

@Component({
  selector: 'app-document-form-modal',
  standalone: true,
  imports: [CommonModule, ReactiveFormsModule],
  templateUrl: './document-form-modal.component.html',
})
export class DocumentFormModalComponent implements OnInit {
  private readonly fb = inject(FormBuilder);
  private readonly documentService = inject(DocumentService);

  @Input() document: CvtDocument | null = null;
  @Output() closed = new EventEmitter<void>();
  @Output() saved = new EventEmitter<CvtDocument>();

  readonly isSubmitting = signal(false);
  readonly errorMessage = signal<string | null>(null);
  readonly fileError = signal<string | null>(null);
  readonly selectedFile = signal<File | null>(null);
  readonly categoryOptions = DOCUMENT_CATEGORIES;

  readonly form = this.fb.nonNullable.group({
    title: ['', [Validators.required, Validators.maxLength(200)]],
    description: ['', [Validators.maxLength(1000)]],
    category: this.fb.nonNullable.control<DocumentCategory | ''>('', [Validators.required]),
  });

  get isEditMode(): boolean {
    return !!this.document;
  }

  ngOnInit(): void {
    if (this.document) {
      this.form.patchValue({
        title: this.document.title,
        description: this.document.description,
        category: this.document.category,
      });
    }
  }

  onFileSelected(event: Event): void {
    const input = event.target as HTMLInputElement;
    const file = input.files?.[0] ?? null;
    this.fileError.set(null);

    if (!file) {
      this.selectedFile.set(null);
      return;
    }

    const extension = file.name.slice(file.name.lastIndexOf('.')).toLowerCase();
    if (!ALLOWED_EXTENSIONS.includes(extension)) {
      this.fileError.set('Seuls les fichiers PDF sont acceptés.');
      this.selectedFile.set(null);
      input.value = '';
      return;
    }

    if (file.size > MAX_FILE_SIZE_BYTES) {
      this.fileError.set('Le fichier dépasse la taille maximale autorisée (100 Mo).');
      this.selectedFile.set(null);
      input.value = '';
      return;
    }

    this.selectedFile.set(file);
  }

  submit(): void {
    this.errorMessage.set(null);

    if (this.form.invalid) {
      this.form.markAllAsTouched();
      return;
    }

    if (!this.isEditMode && !this.selectedFile()) {
      this.fileError.set('Un fichier PDF est obligatoire.');
      return;
    }

    const { title, description, category } = this.form.getRawValue();
    const selectedCategory = category as DocumentCategory; 
    this.isSubmitting.set(true);

    const request$ = this.isEditMode
      ? this.documentService.update(this.document!.id, {
          title,
          description,
          category: selectedCategory,
          file: this.selectedFile(),
        })
      : this.documentService.create({
          title,
          description,
          category: selectedCategory,
          file: this.selectedFile()!,
        });

    request$.subscribe({
      next: (document) => {
        this.isSubmitting.set(false);
        this.saved.emit(document);
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
