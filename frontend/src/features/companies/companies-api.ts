import { api } from '@/lib/api-client';
import { Company } from '@/features/applications/types';

export const companiesApi = {
  search: async (q?: string): Promise<Company[]> => {
    const res = await api.get('/api/companies/search', { params: { q } });
    return res.data;
  },

  getById: async (id: string): Promise<Company> => {
    const res = await api.get(`/api/companies/${id}`);
    return res.data;
  },

  create: async (payload: Partial<Company>): Promise<Company> => {
    const res = await api.post('/api/companies', payload);
    return res.data;
  },
};
