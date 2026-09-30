import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query';
import { dashboardApi } from './dashboard-api';

export const useDashboard = () => {
  return useQuery({
    queryKey: ['dashboard'],
    queryFn: () => dashboardApi.getDashboard(),
  });
};

export const useEvaluateAutomationRules = () => {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: () => dashboardApi.evaluateAutomationRules(),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['dashboard'] });
      queryClient.invalidateQueries({ queryKey: ['tasks'] });
    },
  });
};
