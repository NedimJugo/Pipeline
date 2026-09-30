import { api } from '@/lib/api-client';
import { DiscoveredJob, DiscoveredJobList, JobSource, IngestJobsResult } from './types';

export const discoveryApi = {
  getJobs: async (params?: {
    page?: number;
    pageSize?: number;
    status?: string;
    search?: string;
    sourceId?: string;
  }): Promise<DiscoveredJobList> => {
    const res = await api.get('/api/discovery/jobs', { params });
    return res.data;
  },

  getSources: async (): Promise<JobSource[]> => {
    const res = await api.get('/api/discovery/sources');
    return res.data;
  },

  saveJobToWishlist: async (id: string): Promise<any> => {
    const res = await api.post(`/api/discovery/jobs/${id}/save`);
    return res.data;
  },

  dismissJob: async (id: string): Promise<void> => {
    await api.post(`/api/discovery/jobs/${id}/dismiss`);
  },

  ingestJobs: async (): Promise<IngestJobsResult> => {
    const res = await api.post('/api/discovery/ingest');
    return res.data;
  },
};
