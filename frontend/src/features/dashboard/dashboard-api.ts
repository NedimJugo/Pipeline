import { api } from '@/lib/api-client';
import { DashboardSummary } from './types';

export const dashboardApi = {
  getDashboard: async (): Promise<DashboardSummary> => {
    const res = await api.get<DashboardSummary>('/api/dashboard');
    return res.data;
  },

  evaluateAutomationRules: async (): Promise<{ evaluated: boolean; tasksCreated: number }> => {
    const res = await api.post<{ evaluated: boolean; tasksCreated: number }>('/api/automation/evaluate');
    return res.data;
  },
};
