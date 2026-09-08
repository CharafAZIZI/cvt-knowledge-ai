import { CommonModule } from '@angular/common';
import { Component, OnInit, inject, signal } from '@angular/core';
import { RouterLink } from '@angular/router';
import { AuthService } from '../../core/services/auth.service';
import { DocumentService } from '../../core/services/document.service';
import { CvtDocument } from '../../core/models/document.model';
import { extractErrorMessage } from '../../core/utils/http-error.util';

@Component({
  selector: 'app-dashboard',
  standalone: true,
  imports: [CommonModule, RouterLink],
  templateUrl: './dashboard.component.html',
})
export class DashboardComponent implements OnInit {
  private readonly documentService = inject(DocumentService);
  protected readonly authService = inject(AuthService);

  readonly isLoading = signal(true);
  readonly errorMessage = signal<string | null>(null);
  readonly documents = signal<CvtDocument[]>([]);

  readonly recentDocuments = () =>
    [...this.documents()]
      .sort((a, b) => new Date(b.createdAt).getTime() - new Date(a.createdAt).getTime())
      .slice(0, 5);

  ngOnInit(): void {
    this.loadDocuments();
  }

  private loadDocuments(): void {
    this.isLoading.set(true);
    this.errorMessage.set(null);

    this.documentService.getAll().subscribe({
      next: (documents) => {
        this.documents.set(documents);
        this.isLoading.set(false);
      },
      error: (error) => {
        this.errorMessage.set(extractErrorMessage(error, 'Impossible de charger les documents.'));
        this.isLoading.set(false);
      },
    });
  }
}
