import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query';
import { discoveryApi } from './discovery-api';

export const useDiscoveredJobs = (params?: {
  page?: number;
  pageSize?: number;
  status?: string;
  search?: string;
  sourceId?: string;
}) => {
  return useQuery({
    queryKey: ['discoveredJobs', params],
    queryFn: () => discoveryApi.getJobs(params),
  });
};

export const useJobSources = () => {
  return useQuery({
    queryKey: ['jobSources'],
    queryFn: () => discoveryApi.getSources(),
  });
};

export const useSaveJobToWishlist = () => {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: (id: string) => discoveryApi.saveJobToWishlist(id),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['discoveredJobs'] });
      queryClient.invalidateQueries({ queryKey: ['applications'] });
      queryClient.invalidateQueries({ queryKey: ['dashboard'] });
    },
  });
};

export const useDismissJob = () => {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: (id: string) => discoveryApi.dismissJob(id),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['discoveredJobs'] });
    },
  });
};

export const useIngestJobs = () => {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: () => discoveryApi.ingestJobs(),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['discoveredJobs'] });
      queryClient.invalidateQueries({ queryKey: ['jobSources'] });
    },
  });
};
