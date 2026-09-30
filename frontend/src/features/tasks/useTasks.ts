import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query';
import { tasksApi } from './tasks-api';
import { CreateTaskRequest, UpdateTaskRequest, TaskFilterParams } from './types';

export const useTasks = (params?: TaskFilterParams) => {
  return useQuery({
    queryKey: ['tasks', params],
    queryFn: () => tasksApi.getTasks(params),
  });
};

export const useTask = (id?: string) => {
  return useQuery({
    queryKey: ['task', id],
    queryFn: () => tasksApi.getTaskById(id!),
    enabled: !!id,
  });
};

export const useCreateTask = () => {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: (request: CreateTaskRequest) => tasksApi.createTask(request),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['tasks'] });
      queryClient.invalidateQueries({ queryKey: ['dashboard'] });
    },
  });
};

export const useUpdateTask = () => {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: ({ id, request }: { id: string; request: UpdateTaskRequest }) =>
      tasksApi.updateTask(id, request),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['tasks'] });
      queryClient.invalidateQueries({ queryKey: ['dashboard'] });
    },
  });
};

export const useDeleteTask = () => {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: (id: string) => tasksApi.deleteTask(id),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['tasks'] });
      queryClient.invalidateQueries({ queryKey: ['dashboard'] });
    },
  });
};

export const useCompleteTask = () => {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: ({ id, isCompleted }: { id: string; isCompleted: boolean }) =>
      tasksApi.completeTask(id, isCompleted),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['tasks'] });
      queryClient.invalidateQueries({ queryKey: ['dashboard'] });
    },
  });
};

export const useSnoozeTask = () => {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: ({ id, days }: { id: string; days: number }) =>
      tasksApi.snoozeTask(id, days),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['tasks'] });
      queryClient.invalidateQueries({ queryKey: ['dashboard'] });
    },
  });
};
