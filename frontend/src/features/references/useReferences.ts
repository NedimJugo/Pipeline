import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query';
import { referencesApi } from './references-api';
import {
  CreateReferencePayload,
  UpdateReferencePayload,
  ShareReferencePayload,
  ReferenceConsent,
} from './types';

export const REFERENCES_QUERY_KEY = ['references'];

export function useReferences(params?: { search?: string; consent?: ReferenceConsent }) {
  return useQuery({
    queryKey: [...REFERENCES_QUERY_KEY, params],
    queryFn: () => referencesApi.getReferences(params),
  });
}

export function useReference(id?: string) {
  return useQuery({
    queryKey: [...REFERENCES_QUERY_KEY, id],
    queryFn: () => referencesApi.getReferenceById(id!),
    enabled: Boolean(id),
  });
}

export function useCreateReference() {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: (payload: CreateReferencePayload) => referencesApi.createReference(payload),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: REFERENCES_QUERY_KEY });
    },
  });
}

export function useUpdateReference() {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: ({ id, payload }: { id: string; payload: UpdateReferencePayload }) =>
      referencesApi.updateReference(id, payload),
    onSuccess: (_, variables) => {
      queryClient.invalidateQueries({ queryKey: REFERENCES_QUERY_KEY });
      queryClient.invalidateQueries({ queryKey: [...REFERENCES_QUERY_KEY, variables.id] });
    },
  });
}

export function useDeleteReference() {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: (id: string) => referencesApi.deleteReference(id),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: REFERENCES_QUERY_KEY });
    },
  });
}

export function useShareReference() {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: ({ id, payload }: { id: string; payload: ShareReferencePayload }) =>
      referencesApi.shareReference(id, payload),
    onSuccess: (_, variables) => {
      queryClient.invalidateQueries({ queryKey: REFERENCES_QUERY_KEY });
      queryClient.invalidateQueries({ queryKey: [...REFERENCES_QUERY_KEY, variables.id] });
      queryClient.invalidateQueries({ queryKey: ['applications'] });
    },
  });
}

export function useRemoveSharedReference() {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: ({ referenceId, appRefId }: { referenceId: string; appRefId: string }) =>
      referencesApi.removeSharedReference(referenceId, appRefId),
    onSuccess: (_, variables) => {
      queryClient.invalidateQueries({ queryKey: REFERENCES_QUERY_KEY });
      queryClient.invalidateQueries({ queryKey: [...REFERENCES_QUERY_KEY, variables.referenceId] });
      queryClient.invalidateQueries({ queryKey: ['applications'] });
    },
  });
}

export function useRecordNotification() {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: (id: string) => referencesApi.recordNotification(id),
    onSuccess: (_, id) => {
      queryClient.invalidateQueries({ queryKey: REFERENCES_QUERY_KEY });
      queryClient.invalidateQueries({ queryKey: [...REFERENCES_QUERY_KEY, id] });
    },
  });
}

export function useApplicationReferences(applicationId?: string) {
  return useQuery({
    queryKey: ['application-references', applicationId],
    queryFn: () => referencesApi.getApplicationReferences(applicationId!),
    enabled: Boolean(applicationId),
  });
}
