import { api } from '@/lib/api-client';
import {
  IntegrationSettings,
  UpdateIntegrationSettingsRequest,
  TestEmailRequest,
  TestEmailResult,
} from './types';

export const integrationsApi = {
  getIntegrations: async (): Promise<IntegrationSettings> => {
    const res = await api.get<IntegrationSettings>('/api/settings/integrations');
    return res.data;
  },

  updateIntegrations: async (
    data: UpdateIntegrationSettingsRequest
  ): Promise<IntegrationSettings> => {
    const res = await api.put<IntegrationSettings>('/api/settings/integrations', data);
    return res.data;
  },

  testEmail: async (data: TestEmailRequest): Promise<TestEmailResult> => {
    const res = await api.post<TestEmailResult>('/api/settings/integrations/test-email', data);
    return res.data;
  },
};
