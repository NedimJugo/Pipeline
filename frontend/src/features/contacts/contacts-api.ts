import { api } from '@/lib/api-client';
import {
  ContactListItem,
  ContactDetail,
  CreateContactPayload,
  UpdateContactPayload,
  ContactFilter,
  LinkedApplication,
  LinkApplicationContactPayload,
  Interaction,
  LogInteractionPayload,
} from './types';

export const contactsApi = {
  list: async (filters?: ContactFilter): Promise<ContactListItem[]> => {
    const res = await api.get('/api/contacts', { params: filters });
    return res.data;
  },

  getById: async (id: string): Promise<ContactDetail> => {
    const res = await api.get(`/api/contacts/${id}`);
    return res.data;
  },

  create: async (payload: CreateContactPayload): Promise<ContactDetail> => {
    const res = await api.post('/api/contacts', payload);
    return res.data;
  },

  update: async (id: string, payload: UpdateContactPayload): Promise<ContactDetail> => {
    const res = await api.put(`/api/contacts/${id}`, payload);
    return res.data;
  },

  delete: async (id: string): Promise<void> => {
    await api.delete(`/api/contacts/${id}`);
  },

  getInteractions: async (id: string): Promise<Interaction[]> => {
    const res = await api.get(`/api/contacts/${id}/interactions`);
    return res.data;
  },

  getApplications: async (id: string): Promise<LinkedApplication[]> => {
    const res = await api.get(`/api/contacts/${id}/applications`);
    return res.data;
  },

  linkApplication: async (id: string, payload: LinkApplicationContactPayload): Promise<void> => {
    await api.post(`/api/contacts/${id}/link-application`, payload);
  },

  unlinkApplication: async (id: string, applicationId: string): Promise<void> => {
    await api.delete(`/api/contacts/${id}/link-application/${applicationId}`);
  },

  // Interactions API
  logInteraction: async (payload: LogInteractionPayload): Promise<Interaction> => {
    const res = await api.post('/api/interactions', payload);
    return res.data;
  },

  deleteInteraction: async (id: string): Promise<void> => {
    await api.delete(`/api/interactions/${id}`);
  },

  // Application-specific contacts API
  getApplicationContacts: async (applicationId: string): Promise<ContactListItem[]> => {
    const res = await api.get(`/api/applications/${applicationId}/contacts`);
    return res.data;
  },

  linkApplicationContact: async (applicationId: string, payload: LinkApplicationContactPayload): Promise<void> => {
    await api.post(`/api/applications/${applicationId}/contacts`, payload);
  },

  unlinkApplicationContact: async (applicationId: string, contactId: string): Promise<void> => {
    await api.delete(`/api/applications/${applicationId}/contacts/${contactId}`);
  },
};
