import { api } from '@/lib/api-client';
import {
  InterviewListItem,
  InterviewDetail,
  CreateInterviewPayload,
  UpdateInterviewPayload,
  UpdateDebriefPayload,
  UpdatePrepChecklistPayload,
  AddQuestionPayload,
  InterviewQuestion,
  InterviewFilter,
} from './types';

export const interviewsApi = {
  list: async (filters?: InterviewFilter): Promise<InterviewListItem[]> => {
    const res = await api.get('/api/interviews', { params: filters });
    return res.data;
  },

  getById: async (id: string): Promise<InterviewDetail> => {
    const res = await api.get(`/api/interviews/${id}`);
    return res.data;
  },

  create: async (payload: CreateInterviewPayload): Promise<InterviewDetail> => {
    const res = await api.post('/api/interviews', payload);
    return res.data;
  },

  update: async (id: string, payload: UpdateInterviewPayload): Promise<InterviewDetail> => {
    const res = await api.put(`/api/interviews/${id}`, payload);
    return res.data;
  },

  updateDebrief: async (id: string, payload: UpdateDebriefPayload): Promise<InterviewDetail> => {
    const res = await api.patch(`/api/interviews/${id}/debrief`, payload);
    return res.data;
  },

  updateChecklist: async (id: string, payload: UpdatePrepChecklistPayload): Promise<InterviewDetail> => {
    const res = await api.put(`/api/interviews/${id}/checklist`, payload);
    return res.data;
  },

  addQuestion: async (id: string, payload: AddQuestionPayload): Promise<InterviewQuestion> => {
    const res = await api.post(`/api/interviews/${id}/questions`, payload);
    return res.data;
  },

  deleteQuestion: async (interviewId: string, questionId: string): Promise<void> => {
    await api.delete(`/api/interviews/${interviewId}/questions/${questionId}`);
  },

  delete: async (id: string): Promise<void> => {
    await api.delete(`/api/interviews/${id}`);
  },

  getApplicationInterviews: async (applicationId: string): Promise<InterviewListItem[]> => {
    const res = await api.get(`/api/applications/${applicationId}/interviews`);
    return res.data;
  },

  downloadIcs: async (id: string): Promise<void> => {
    const res = await api.get(`/api/interviews/${id}/calendar.ics`, {
      responseType: 'blob',
    });
    const blob = new Blob([res.data], { type: 'text/calendar;charset=utf-8' });
    const url = window.URL.createObjectURL(blob);
    const link = document.createElement('a');
    link.href = url;
    link.setAttribute('download', `interview-${id}.ics`);
    document.body.appendChild(link);
    link.click();
    link.remove();
    window.URL.revokeObjectURL(url);
  },
};
