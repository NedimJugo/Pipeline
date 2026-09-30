import { api } from '@/lib/api-client';
import {
  TaskItem,
  CreateTaskRequest,
  UpdateTaskRequest,
  SnoozeTaskRequest,
  TaskFilterParams,
  PagedTasksResult,
} from './types';

export const tasksApi = {
  getTasks: async (params?: TaskFilterParams): Promise<PagedTasksResult> => {
    const res = await api.get<PagedTasksResult>('/api/tasks', { params });
    return res.data;
  },

  getTaskById: async (id: string): Promise<TaskItem> => {
    const res = await api.get<TaskItem>(`/api/tasks/${id}`);
    return res.data;
  },

  createTask: async (payload: CreateTaskRequest): Promise<TaskItem> => {
    const res = await api.post<TaskItem>('/api/tasks', payload);
    return res.data;
  },

  updateTask: async (id: string, payload: UpdateTaskRequest): Promise<TaskItem> => {
    const res = await api.put<TaskItem>(`/api/tasks/${id}`, payload);
    return res.data;
  },

  deleteTask: async (id: string): Promise<void> => {
    await api.delete(`/api/tasks/${id}`);
  },

  completeTask: async (id: string, isCompleted: boolean = true): Promise<TaskItem> => {
    const res = await api.post<TaskItem>(`/api/tasks/${id}/complete`, { isCompleted });
    return res.data;
  },

  snoozeTask: async (id: string, days: number = 1): Promise<TaskItem> => {
    const res = await api.post<TaskItem>(`/api/tasks/${id}/snooze`, { days } as SnoozeTaskRequest);
    return res.data;
  },
};
