import { api } from '@/lib/api-client';
import {
  Document,
  DocumentVersion,
  DocumentType,
  CreateDocumentRequest,
  UpdateDocumentRequest,
  DocumentStatsSummary,
  DocumentVersionStats,
  DocumentDownloadInfo,
} from './types';

export const documentsApi = {
  list: async (type?: DocumentType): Promise<Document[]> => {
    const res = await api.get('/api/documents', {
      params: type ? { type } : undefined,
    });
    return res.data;
  },

  getById: async (id: string): Promise<Document> => {
    const res = await api.get(`/api/documents/${id}`);
    return res.data;
  },

  create: async (request: CreateDocumentRequest): Promise<Document> => {
    const res = await api.post('/api/documents', request);
    return res.data;
  },

  createWithFile: async (data: {
    title: string;
    type: DocumentType;
    description?: string;
    versionLabel?: string;
    notes?: string;
    file: File;
  }): Promise<Document> => {
    const formData = new FormData();
    formData.append('title', data.title);
    formData.append('type', data.type);
    if (data.description) formData.append('description', data.description);
    if (data.versionLabel) formData.append('versionLabel', data.versionLabel);
    if (data.notes) formData.append('notes', data.notes);
    formData.append('file', data.file);

    const res = await api.post('/api/documents/with-file', formData, {
      headers: {
        'Content-Type': 'multipart/form-data',
      },
    });
    return res.data;
  },

  update: async (id: string, request: UpdateDocumentRequest): Promise<Document> => {
    const res = await api.put(`/api/documents/${id}`, request);
    return res.data;
  },

  delete: async (id: string): Promise<void> => {
    await api.delete(`/api/documents/${id}`);
  },

  uploadVersion: async (
    documentId: string,
    data: {
      versionLabel: string;
      notes?: string;
      isDefault?: boolean;
      file: File;
    }
  ): Promise<DocumentVersion> => {
    const formData = new FormData();
    formData.append('versionLabel', data.versionLabel);
    if (data.notes) formData.append('notes', data.notes);
    formData.append('isDefault', String(data.isDefault ?? false));
    formData.append('file', data.file);

    const res = await api.post(`/api/documents/${documentId}/versions`, formData, {
      headers: {
        'Content-Type': 'multipart/form-data',
      },
    });
    return res.data;
  },

  setDefaultVersion: async (versionId: string): Promise<DocumentVersion> => {
    const res = await api.put(`/api/document-versions/${versionId}/default`);
    return res.data;
  },

  deleteVersion: async (versionId: string): Promise<void> => {
    await api.delete(`/api/document-versions/${versionId}`);
  },

  getDownloadInfo: async (versionId: string): Promise<DocumentDownloadInfo> => {
    const res = await api.get(`/api/document-versions/${versionId}/download`);
    return res.data;
  },

  getStreamBlobUrl: async (versionId: string): Promise<string> => {
    const res = await api.get(`/api/document-versions/${versionId}/stream`, {
      responseType: 'blob',
    });
    const contentType = (res.headers['content-type'] as string) || 'application/pdf';
    return window.URL.createObjectURL(new Blob([res.data], { type: contentType }));
  },

  downloadFile: async (versionId: string, fileName: string): Promise<void> => {
    const res = await api.get(`/api/document-versions/${versionId}/stream`, {
      responseType: 'blob',
    });
    const contentType = (res.headers['content-type'] as string) || 'application/octet-stream';
    const blob = new Blob([res.data], {
      type: contentType,
    });
    const url = window.URL.createObjectURL(blob);
    const link = document.createElement('a');
    link.href = url;
    link.setAttribute('download', fileName);
    document.body.appendChild(link);
    link.click();
    link.remove();
    window.URL.revokeObjectURL(url);
  },

  getStatsSummary: async (): Promise<DocumentStatsSummary> => {
    const res = await api.get('/api/documents/stats');
    return res.data;
  },

  getVersionStats: async (type?: DocumentType): Promise<DocumentVersionStats[]> => {
    const res = await api.get('/api/documents/version-stats', {
      params: type ? { type } : undefined,
    });
    return res.data;
  },
};
