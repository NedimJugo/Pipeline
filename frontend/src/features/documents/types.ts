export type DocumentType = 'CV' | 'CoverLetter' | 'Portfolio' | 'Certificate' | 'Other';

export interface DocumentVersionStats {
  versionId: string;
  versionLabel: string;
  documentTitle: string;
  documentType: DocumentType;
  sentCount: number;
  replyCount: number;
  interviewCount: number;
  offerCount: number;
  responseRate: number;
  interviewRate: number;
  offerRate: number;
}

export interface DocumentVersion {
  id: string;
  documentId: string;
  versionLabel: string;
  fileName: string;
  contentType: string;
  sizeBytes: number;
  notes?: string | null;
  isDefault: boolean;
  createdAt: string;
  stats?: DocumentVersionStats | null;
}

export interface Document {
  id: string;
  type: DocumentType;
  title: string;
  description?: string | null;
  defaultVersionId?: string | null;
  defaultVersionLabel?: string | null;
  versionCount: number;
  createdAt: string;
  updatedAt: string;
  versions: DocumentVersion[];
}

export interface CreateDocumentRequest {
  title: string;
  type: DocumentType;
  description?: string;
  versionLabel?: string;
  notes?: string;
}

export interface UpdateDocumentRequest {
  title: string;
  type?: DocumentType;
  description?: string;
}

export interface UploadVersionRequest {
  versionLabel: string;
  notes?: string;
  isDefault?: boolean;
}

export interface DocumentStatsSummary {
  totalDocuments: number;
  totalVersions: number;
  cvVersionStats: DocumentVersionStats[];
  coverLetterStats: DocumentVersionStats[];
}

export interface DocumentDownloadInfo {
  url: string;
  fileName: string;
  contentType: string;
  sizeBytes: number;
}
