import { DocumentCategory } from '../constants/document-category.constant';

// Reflète exactement DocumentService.DTOs.DocumentResponse
export interface CvtDocument {
  id: number;
  title: string;
  description: string;
  category: DocumentCategory;
  fileName: string;
  fileType: string;
  filePath: string;
  createdAt: string;
  updatedAt: string | null;
  createdBy: number;
  isActive: boolean;
}

// Payload envoyé en multipart/form-data vers POST /api/documents
// (champs attendus par DocumentsController.Create : title, description, file)
export interface CreateDocumentPayload {
  title: string;
  description: string;
  category: DocumentCategory;
  file: File;
}

// Payload envoyé en multipart/form-data vers PUT /api/documents/{id}
// (champs attendus par DocumentsController.Update : title, description, file?)
export interface UpdateDocumentPayload {
  title: string;
  description: string;
  category: DocumentCategory;
  file?: File | null;
}
