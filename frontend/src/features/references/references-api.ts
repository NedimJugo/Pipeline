import { api } from '@/lib/api-client';
import {
  ReferenceListItem,
  ReferenceDetail,
  CreateReferencePayload,
  UpdateReferencePayload,
  ShareReferencePayload,
  ShareReferenceResult,
  ReferenceConsent,
  ApplicationReference,
} from './types';

export const referencesApi = {
  getReferences: async (params?: { search?: string; consent?: ReferenceConsent }) => {
    const { data } = await api.get<ReferenceListItem[]>('/api/references', { params });
    return data;
  },

  getReferenceById: async (id: string) => {
    const { data } = await api.get<ReferenceDetail>(`/api/references/${id}`);
    return data;
  },

  createReference: async (payload: CreateReferencePayload) => {
    const { data } = await api.post<ReferenceDetail>('/api/references', payload);
    return data;
  },

  updateReference: async (id: string, payload: UpdateReferencePayload) => {
    const { data } = await api.put<ReferenceDetail>(`/api/references/${id}`, payload);
    return data;
  },

  deleteReference: async (id: string) => {
    await api.delete(`/api/references/${id}`);
  },

  shareReference: async (id: string, payload: ShareReferencePayload) => {
    const { data } = await api.post<ShareReferenceResult>(`/api/references/${id}/share`, payload);
    return data;
  },

  removeSharedReference: async (referenceId: string, appRefId: string) => {
    await api.delete(`/api/references/${referenceId}/share/${appRefId}`);
  },

  recordNotification: async (id: string) => {
    await api.post(`/api/references/${id}/notify`);
  },

  getApplicationReferences: async (applicationId: string) => {
    const { data } = await api.get<ApplicationReference[]>(`/api/references/application/${applicationId}`);
    return data;
  },
};
