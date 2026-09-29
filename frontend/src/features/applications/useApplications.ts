import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query';
import { applicationsApi } from './applications-api';
import {
  ApplicationFilter,
  ApplicationListItem,
  CreateApplicationPayload,
  UpdateStatusPayload,
} from './types';

export const useApplications = (filters?: ApplicationFilter) => {
  return useQuery({
    queryKey: ['applications', filters],
    queryFn: () => applicationsApi.list(filters),
  });
};

export const useApplication = (id: string | undefined) => {
  return useQuery({
    queryKey: ['application', id],
    queryFn: () => applicationsApi.getById(id!),
    enabled: !!id,
  });
};

export const useCreateApplication = () => {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: (payload: CreateApplicationPayload) => applicationsApi.create(payload),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['applications'] });
    },
  });
};

export const useUpdateApplication = () => {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: ({ id, payload }: { id: string; payload: Partial<CreateApplicationPayload> }) =>
      applicationsApi.update(id, payload),
    onSuccess: (_data, { id }) => {
      queryClient.invalidateQueries({ queryKey: ['applications'] });
      queryClient.invalidateQueries({ queryKey: ['application', id] });
    },
  });
};

export const useUpdateApplicationStatus = () => {
  const queryClient = useQueryClient();

  return useMutation({
    mutationFn: ({ id, payload }: { id: string; payload: UpdateStatusPayload }) =>
      applicationsApi.updateStatus(id, payload),
    onMutate: async ({ id, payload }) => {
      // Cancel outgoing refetches
      await queryClient.cancelQueries({ queryKey: ['applications'] });

      // Snapshot previous value
      const previousApplications = queryClient.getQueryData<ApplicationListItem[]>(['applications']);

      // Optimistically update
      if (previousApplications) {
        queryClient.setQueryData<ApplicationListItem[]>(
          ['applications'],
          previousApplications.map((app) =>
            app.id === id ? { ...app, status: payload.status, daysInStage: 0 } : app
          )
        );
      }

      return { previousApplications };
    },
    onError: (_err, _vars, context) => {
      if (context?.previousApplications) {
        queryClient.setQueryData(['applications'], context.previousApplications);
      }
    },
    onSettled: (_data, _error, { id }) => {
      queryClient.invalidateQueries({ queryKey: ['applications'] });
      queryClient.invalidateQueries({ queryKey: ['application', id] });
      queryClient.invalidateQueries({ queryKey: ['timeline', id] });
    },
  });
};

export const useDeleteApplication = () => {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: (id: string) => applicationsApi.delete(id),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['applications'] });
    },
  });
};

export const useDuplicateApplication = () => {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: (id: string) => applicationsApi.duplicate(id),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['applications'] });
    },
  });
};

export const useApplicationTimeline = (id: string | undefined) => {
  return useQuery({
    queryKey: ['timeline', id],
    queryFn: () => applicationsApi.getTimeline(id!),
    enabled: !!id,
  });
};
