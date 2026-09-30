import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query';
import { templatesApi } from './templates-api';
import {
  EmailTemplateCategory,
  CreateEmailTemplateRequest,
  UpdateEmailTemplateRequest,
  RenderEmailTemplateRequest,
} from './types';

export const useTemplates = (category?: EmailTemplateCategory) => {
  return useQuery({
    queryKey: ['templates', category],
    queryFn: () => templatesApi.getTemplates(category),
  });
};

export const useTemplate = (id?: string) => {
  return useQuery({
    queryKey: ['template', id],
    queryFn: () => templatesApi.getTemplateById(id!),
    enabled: !!id,
  });
};

export const useCreateTemplate = () => {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: (request: CreateEmailTemplateRequest) => templatesApi.createTemplate(request),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['templates'] });
    },
  });
};

export const useUpdateTemplate = () => {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: ({ id, request }: { id: string; request: UpdateEmailTemplateRequest }) =>
      templatesApi.updateTemplate(id, request),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['templates'] });
    },
  });
};

export const useDeleteTemplate = () => {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: (id: string) => templatesApi.deleteTemplate(id),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['templates'] });
    },
  });
};

export const useRenderTemplate = () => {
  return useMutation({
    mutationFn: ({ id, request }: { id: string; request: RenderEmailTemplateRequest }) =>
      templatesApi.renderTemplate(id, request),
  });
};
