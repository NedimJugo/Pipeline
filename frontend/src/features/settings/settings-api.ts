import { api } from '@/lib/api-client';
import {
  UserSettingsProfile,
  UpdateProfileRequest,
  UpdatePreferencesRequest,
  CsvImportResult,
} from './types';

export const settingsApi = {
  getProfile: async (): Promise<UserSettingsProfile> => {
    const res = await api.get('/api/me');
    return res.data;
  },

  updateProfile: async (data: UpdateProfileRequest): Promise<UserSettingsProfile> => {
    const res = await api.put('/api/me', data);
    return res.data;
  },

  updatePreferences: async (data: UpdatePreferencesRequest): Promise<UserSettingsProfile> => {
    const res = await api.put('/api/me/preferences', data);
    return res.data;
  },

  exportApplicationsCsv: async (): Promise<void> => {
    const res = await api.get('/api/applications/export', { responseType: 'blob' });
    const url = window.URL.createObjectURL(new Blob([res.data], { type: 'text/csv' }));
    const link = document.createElement('a');
    link.href = url;
    link.setAttribute('download', `pipeline_applications_${new Date().toISOString().slice(0, 10)}.csv`);
    document.body.appendChild(link);
    link.click();
    link.remove();
    window.URL.revokeObjectURL(url);
  },

  importApplicationsCsv: async (file: File): Promise<CsvImportResult> => {
    const formData = new FormData();
    formData.append('file', file);
    const res = await api.post('/api/applications/import', formData, {
      headers: { 'Content-Type': 'multipart/form-data' },
    });
    return res.data;
  },

  exportGdprData: async (): Promise<void> => {
    const res = await api.get('/api/me/export', { responseType: 'blob' });
    const url = window.URL.createObjectURL(new Blob([res.data], { type: 'application/json' }));
    const link = document.createElement('a');
    link.href = url;
    link.setAttribute('download', `pipeline_gdpr_export_${new Date().toISOString().slice(0, 10)}.json`);
    document.body.appendChild(link);
    link.click();
    link.remove();
    window.URL.revokeObjectURL(url);
  },

  deleteAccount: async (): Promise<void> => {
    await api.delete('/api/me');
  },

  seedDemoData: async (): Promise<{ message: string; profile: UserSettingsProfile }> => {
    const res = await api.post('/api/me/seed-demo');
    return res.data;
  },
};
