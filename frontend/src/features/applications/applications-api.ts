import { api } from '@/lib/api-client';
import {
  ApplicationListItem,
  ApplicationDetail,
  CreateApplicationPayload,
  UpdateStatusPayload,
  ApplicationTimelineItem,
  ApplicationFilter,
} from './types';

export const applicationsApi = {
  list: async (filters?: ApplicationFilter): Promise<ApplicationListItem[]> => {
    const res = await api.get('/api/applications', { params: filters });
    return res.data;
  },

  getById: async (id: string): Promise<ApplicationDetail> => {
    const res = await api.get(`/api/applications/${id}`);
    return res.data;
  },

  create: async (payload: CreateApplicationPayload): Promise<ApplicationDetail> => {
    const res = await api.post('/api/applications', payload);
    return res.data;
  },

  update: async (id: string, payload: Partial<CreateApplicationPayload>): Promise<ApplicationDetail> => {
    const res = await api.put(`/api/applications/${id}`, payload);
    return res.data;
  },

  delete: async (id: string): Promise<void> => {
    await api.delete(`/api/applications/${id}`);
  },

  updateStatus: async (id: string, payload: UpdateStatusPayload): Promise<ApplicationDetail> => {
    const res = await api.patch(`/api/applications/${id}/status`, payload);
    return res.data;
  },

  getTimeline: async (id: string): Promise<ApplicationTimelineItem[]> => {
    const res = await api.get(`/api/applications/${id}/timeline`);
    return res.data;
  },

  duplicate: async (id: string): Promise<ApplicationDetail> => {
    const res = await api.post(`/api/applications/${id}/duplicate`);
    return res.data;
  },
};
