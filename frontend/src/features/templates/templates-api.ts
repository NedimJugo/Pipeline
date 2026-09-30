import { api } from '@/lib/api-client';
import {
  EmailTemplate,
  EmailTemplateCategory,
  CreateEmailTemplateRequest,
  UpdateEmailTemplateRequest,
  RenderEmailTemplateRequest,
  RenderedEmailTemplate,
} from './types';

export const templatesApi = {
  getTemplates: async (category?: EmailTemplateCategory): Promise<EmailTemplate[]> => {
    const res = await api.get<EmailTemplate[]>('/api/templates', {
      params: category ? { category } : undefined,
    });
    return res.data;
  },

  getTemplateById: async (id: string): Promise<EmailTemplate> => {
    const res = await api.get<EmailTemplate>(`/api/templates/${id}`);
    return res.data;
  },

  createTemplate: async (payload: CreateEmailTemplateRequest): Promise<EmailTemplate> => {
    const res = await api.post<EmailTemplate>('/api/templates', payload);
    return res.data;
  },

  updateTemplate: async (id: string, payload: UpdateEmailTemplateRequest): Promise<EmailTemplate> => {
    const res = await api.put<EmailTemplate>(`/api/templates/${id}`, payload);
    return res.data;
  },

  deleteTemplate: async (id: string): Promise<void> => {
    await api.delete(`/api/templates/${id}`);
  },

  renderTemplate: async (
    id: string,
    payload: RenderEmailTemplateRequest
  ): Promise<RenderedEmailTemplate> => {
    const res = await api.post<RenderedEmailTemplate>(`/api/templates/${id}/render`, payload);
    return res.data;
  },
};
