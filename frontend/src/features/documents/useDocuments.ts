import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query';
import { documentsApi } from './documents-api';
import {
  DocumentType,
  CreateDocumentRequest,
  UpdateDocumentRequest,
} from './types';

export const useDocuments = (type?: DocumentType) => {
  return useQuery({
    queryKey: ['documents', type],
    queryFn: () => documentsApi.list(type),
  });
};

export const useDocument = (id?: string) => {
  return useQuery({
    queryKey: ['document', id],
    queryFn: () => documentsApi.getById(id!),
    enabled: !!id,
  });
};

export const useDocumentStatsSummary = () => {
  return useQuery({
    queryKey: ['documents', 'stats'],
    queryFn: () => documentsApi.getStatsSummary(),
  });
};

export const useDocumentVersionStats = (type?: DocumentType) => {
  return useQuery({
    queryKey: ['documents', 'version-stats', type],
    queryFn: () => documentsApi.getVersionStats(type),
  });
};

export const useCreateDocument = () => {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: (request: CreateDocumentRequest) => documentsApi.create(request),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['documents'] });
    },
  });
};

export const useCreateDocumentWithFile = () => {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: (data: {
      title: string;
      type: DocumentType;
      description?: string;
      versionLabel?: string;
      notes?: string;
      file: File;
    }) => documentsApi.createWithFile(data),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['documents'] });
    },
  });
};

export const useUpdateDocument = () => {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: ({ id, request }: { id: string; request: UpdateDocumentRequest }) =>
      documentsApi.update(id, request),
    onSuccess: (_, variables) => {
      queryClient.invalidateQueries({ queryKey: ['documents'] });
      queryClient.invalidateQueries({ queryKey: ['document', variables.id] });
    },
  });
};

export const useDeleteDocument = () => {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: (id: string) => documentsApi.delete(id),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['documents'] });
    },
  });
};

export const useUploadVersion = () => {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: ({
      documentId,
      data,
    }: {
      documentId: string;
      data: {
        versionLabel: string;
        notes?: string;
        isDefault?: boolean;
        file: File;
      };
    }) => documentsApi.uploadVersion(documentId, data),
    onSuccess: (_, variables) => {
      queryClient.invalidateQueries({ queryKey: ['documents'] });
      queryClient.invalidateQueries({ queryKey: ['document', variables.documentId] });
      queryClient.invalidateQueries({ queryKey: ['documents', 'stats'] });
      queryClient.invalidateQueries({ queryKey: ['documents', 'version-stats'] });
    },
  });
};

export const useSetDefaultVersion = () => {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: (versionId: string) => documentsApi.setDefaultVersion(versionId),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['documents'] });
    },
  });
};

export const useDeleteVersion = () => {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: (versionId: string) => documentsApi.deleteVersion(versionId),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['documents'] });
      queryClient.invalidateQueries({ queryKey: ['documents', 'stats'] });
      queryClient.invalidateQueries({ queryKey: ['documents', 'version-stats'] });
    },
  });
};
