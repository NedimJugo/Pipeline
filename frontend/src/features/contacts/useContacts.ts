import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query';
import { contactsApi } from './contacts-api';
import {
  ContactFilter,
  CreateContactPayload,
  UpdateContactPayload,
  LinkApplicationContactPayload,
  LogInteractionPayload,
} from './types';

export const useContacts = (filters?: ContactFilter) => {
  return useQuery({
    queryKey: ['contacts', filters],
    queryFn: () => contactsApi.list(filters),
  });
};

export const useContact = (id: string | undefined) => {
  return useQuery({
    queryKey: ['contact', id],
    queryFn: () => contactsApi.getById(id!),
    enabled: !!id,
  });
};

export const useCreateContact = () => {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: (payload: CreateContactPayload) => contactsApi.create(payload),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['contacts'] });
    },
  });
};

export const useUpdateContact = () => {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: ({ id, payload }: { id: string; payload: UpdateContactPayload }) =>
      contactsApi.update(id, payload),
    onSuccess: (_data, { id }) => {
      queryClient.invalidateQueries({ queryKey: ['contacts'] });
      queryClient.invalidateQueries({ queryKey: ['contact', id] });
    },
  });
};

export const useDeleteContact = () => {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: (id: string) => contactsApi.delete(id),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['contacts'] });
    },
  });
};

export const useContactInteractions = (id: string | undefined) => {
  return useQuery({
    queryKey: ['contact-interactions', id],
    queryFn: () => contactsApi.getInteractions(id!),
    enabled: !!id,
  });
};

export const useContactApplications = (id: string | undefined) => {
  return useQuery({
    queryKey: ['contact-applications', id],
    queryFn: () => contactsApi.getApplications(id!),
    enabled: !!id,
  });
};

export const useLinkApplicationContact = () => {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: ({ contactId, payload }: { contactId: string; payload: LinkApplicationContactPayload }) =>
      contactsApi.linkApplication(contactId, payload),
    onSuccess: (_data, { contactId, payload }) => {
      queryClient.invalidateQueries({ queryKey: ['contact', contactId] });
      queryClient.invalidateQueries({ queryKey: ['contact-applications', contactId] });
      queryClient.invalidateQueries({ queryKey: ['application-contacts', payload.applicationId] });
    },
  });
};

export const useUnlinkApplicationContact = () => {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: ({ contactId, applicationId }: { contactId: string; applicationId: string }) =>
      contactsApi.unlinkApplication(contactId, applicationId),
    onSuccess: (_data, { contactId, applicationId }) => {
      queryClient.invalidateQueries({ queryKey: ['contact', contactId] });
      queryClient.invalidateQueries({ queryKey: ['contact-applications', contactId] });
      queryClient.invalidateQueries({ queryKey: ['application-contacts', applicationId] });
    },
  });
};

export const useLogInteraction = () => {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: (payload: LogInteractionPayload) => contactsApi.logInteraction(payload),
    onSuccess: (data) => {
      queryClient.invalidateQueries({ queryKey: ['contacts'] });
      if (data.contactId) {
        queryClient.invalidateQueries({ queryKey: ['contact', data.contactId] });
        queryClient.invalidateQueries({ queryKey: ['contact-interactions', data.contactId] });
      }
      if (data.applicationId) {
        queryClient.invalidateQueries({ queryKey: ['application', data.applicationId] });
        queryClient.invalidateQueries({ queryKey: ['timeline', data.applicationId] });
      }
    },
  });
};

export const useDeleteInteraction = () => {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: (id: string) => contactsApi.deleteInteraction(id),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['contacts'] });
      queryClient.invalidateQueries({ queryKey: ['contact-interactions'] });
      queryClient.invalidateQueries({ queryKey: ['timeline'] });
    },
  });
};

export const useApplicationContacts = (applicationId: string | undefined) => {
  return useQuery({
    queryKey: ['application-contacts', applicationId],
    queryFn: () => contactsApi.getApplicationContacts(applicationId!),
    enabled: !!applicationId,
  });
};

export const useLinkContactToApplication = () => {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: ({ applicationId, payload }: { applicationId: string; payload: { contactId: string; roleInProcess?: string | null } }) =>
      contactsApi.linkApplicationContact(applicationId, payload),
    onSuccess: (_data, { applicationId, payload }) => {
      queryClient.invalidateQueries({ queryKey: ['application-contacts', applicationId] });
      queryClient.invalidateQueries({ queryKey: ['contact', payload.contactId] });
      queryClient.invalidateQueries({ queryKey: ['contact-applications', payload.contactId] });
    },
  });
};

export const useUnlinkContactFromApplication = () => {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: ({ applicationId, contactId }: { applicationId: string; contactId: string }) =>
      contactsApi.unlinkApplicationContact(applicationId, contactId),
    onSuccess: (_data, { applicationId, contactId }) => {
      queryClient.invalidateQueries({ queryKey: ['application-contacts', applicationId] });
      queryClient.invalidateQueries({ queryKey: ['contact', contactId] });
      queryClient.invalidateQueries({ queryKey: ['contact-applications', contactId] });
    },
  });
};
