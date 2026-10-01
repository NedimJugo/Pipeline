import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query';
import { settingsApi } from './settings-api';
import { integrationsApi } from './integrations-api';
import {
  UpdateProfileRequest,
  UpdatePreferencesRequest,
  UpdateIntegrationSettingsRequest,
  TestEmailRequest,
} from './types';

export const useProfile = () => {
  return useQuery({
    queryKey: ['user-profile'],
    queryFn: () => settingsApi.getProfile(),
  });
};

export const useUpdateProfile = () => {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: (data: UpdateProfileRequest) => settingsApi.updateProfile(data),
    onSuccess: (data) => {
      queryClient.setQueryData(['user-profile'], data);
      queryClient.invalidateQueries({ queryKey: ['applications'] });
    },
  });
};

export const useUpdatePreferences = () => {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: (data: UpdatePreferencesRequest) => settingsApi.updatePreferences(data),
    onSuccess: (data) => {
      queryClient.setQueryData(['user-profile'], data);
    },
  });
};

export const useImportCsv = () => {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: (file: File) => settingsApi.importApplicationsCsv(file),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['applications'] });
      queryClient.invalidateQueries({ queryKey: ['analytics'] });
      queryClient.invalidateQueries({ queryKey: ['dashboard'] });
    },
  });
};

export const useSeedDemo = () => {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: () => settingsApi.seedDemoData(),
    onSuccess: (res) => {
      queryClient.setQueryData(['user-profile'], res.profile);
      queryClient.invalidateQueries();
    },
  });
};

export const useDeleteAccount = () => {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: () => settingsApi.deleteAccount(),
    onSuccess: () => {
      localStorage.removeItem('pipeline_token');
      localStorage.removeItem('pipeline_user');
      queryClient.clear();
      window.location.href = '/login';
    },
  });
};

export const useIntegrations = () => {
  return useQuery({
    queryKey: ['user-integrations'],
    queryFn: () => integrationsApi.getIntegrations(),
  });
};

export const useUpdateIntegrations = () => {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: (data: UpdateIntegrationSettingsRequest) =>
      integrationsApi.updateIntegrations(data),
    onSuccess: (data) => {
      queryClient.setQueryData(['user-integrations'], data);
    },
  });
};

export const useTestEmail = () => {
  return useMutation({
    mutationFn: (data: TestEmailRequest) => integrationsApi.testEmail(data),
  });
};

