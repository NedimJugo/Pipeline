import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query';
import { interviewsApi } from './interviews-api';
import {
  InterviewFilter,
  CreateInterviewPayload,
  UpdateInterviewPayload,
  UpdateDebriefPayload,
  UpdatePrepChecklistPayload,
  AddQuestionPayload,
} from './types';

export const useInterviews = (filters?: InterviewFilter) => {
  return useQuery({
    queryKey: ['interviews', filters],
    queryFn: () => interviewsApi.list(filters),
  });
};

export const useInterview = (id: string | undefined) => {
  return useQuery({
    queryKey: ['interview', id],
    queryFn: () => interviewsApi.getById(id!),
    enabled: !!id,
  });
};

export const useApplicationInterviews = (applicationId: string | undefined) => {
  return useQuery({
    queryKey: ['application-interviews', applicationId],
    queryFn: () => interviewsApi.getApplicationInterviews(applicationId!),
    enabled: !!applicationId,
  });
};

export const useCreateInterview = () => {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: (payload: CreateInterviewPayload) => interviewsApi.create(payload),
    onSuccess: (data) => {
      queryClient.invalidateQueries({ queryKey: ['interviews'] });
      queryClient.invalidateQueries({ queryKey: ['application-interviews', data.applicationId] });
      queryClient.invalidateQueries({ queryKey: ['application', data.applicationId] });
      queryClient.invalidateQueries({ queryKey: ['timeline', data.applicationId] });
    },
  });
};

export const useUpdateInterview = () => {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: ({ id, payload }: { id: string; payload: UpdateInterviewPayload }) =>
      interviewsApi.update(id, payload),
    onSuccess: (_data, { id }) => {
      queryClient.invalidateQueries({ queryKey: ['interviews'] });
      queryClient.invalidateQueries({ queryKey: ['interview', id] });
      queryClient.invalidateQueries({ queryKey: ['application-interviews'] });
    },
  });
};

export const useUpdateDebrief = () => {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: ({ id, payload }: { id: string; payload: UpdateDebriefPayload }) =>
      interviewsApi.updateDebrief(id, payload),
    onSuccess: (_data, { id }) => {
      queryClient.invalidateQueries({ queryKey: ['interviews'] });
      queryClient.invalidateQueries({ queryKey: ['interview', id] });
      queryClient.invalidateQueries({ queryKey: ['application-interviews'] });
      queryClient.invalidateQueries({ queryKey: ['timeline'] });
    },
  });
};

export const useUpdatePrepChecklist = () => {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: ({ id, payload }: { id: string; payload: UpdatePrepChecklistPayload }) =>
      interviewsApi.updateChecklist(id, payload),
    onSuccess: (_data, { id }) => {
      queryClient.invalidateQueries({ queryKey: ['interview', id] });
    },
  });
};

export const useAddQuestion = () => {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: ({ interviewId, payload }: { interviewId: string; payload: AddQuestionPayload }) =>
      interviewsApi.addQuestion(interviewId, payload),
    onSuccess: (_data, { interviewId }) => {
      queryClient.invalidateQueries({ queryKey: ['interview', interviewId] });
    },
  });
};

export const useDeleteQuestion = () => {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: ({ interviewId, questionId }: { interviewId: string; questionId: string }) =>
      interviewsApi.deleteQuestion(interviewId, questionId),
    onSuccess: (_data, { interviewId }) => {
      queryClient.invalidateQueries({ queryKey: ['interview', interviewId] });
    },
  });
};

export const useDeleteInterview = () => {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: (id: string) => interviewsApi.delete(id),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['interviews'] });
      queryClient.invalidateQueries({ queryKey: ['application-interviews'] });
      queryClient.invalidateQueries({ queryKey: ['timeline'] });
    },
  });
};
