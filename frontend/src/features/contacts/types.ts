import { ApplicationStatus } from '../applications/types';

export type ContactType =
  | 'Recruiter'
  | 'HiringManager'
  | 'Interviewer'
  | 'Referrer'
  | 'Peer'
  | 'Other';

export type ContactWarmth = 'Hot' | 'Warm' | 'Cooling' | 'Cold';

export type InteractionChannel =
  | 'Email'
  | 'LinkedIn'
  | 'Phone'
  | 'Video'
  | 'InPerson'
  | 'Message'
  | 'Other';

export type InteractionDirection = 'Inbound' | 'Outbound';

export interface LinkedApplication {
  applicationId: string;
  roleTitle: string;
  companyName: string;
  status: ApplicationStatus;
  roleInProcess?: string | null;
}

export interface Interaction {
  id: string;
  contactId?: string | null;
  contactName?: string | null;
  applicationId?: string | null;
  applicationRole?: string | null;
  companyName?: string | null;
  channel: InteractionChannel;
  direction: InteractionDirection;
  occurredAt: string;
  summary: string;
  sentContent?: string | null;
  attachmentDocumentId?: string | null;
  followUpRequired: boolean;
  followUpDueAt?: string | null;
  createdAt: string;
}

export interface ContactListItem {
  id: string;
  fullName: string;
  role?: string | null;
  companyId?: string | null;
  companyName?: string | null;
  email?: string | null;
  phone?: string | null;
  linkedInUrl?: string | null;
  type: ContactType;
  warmth: ContactWarmth;
  daysSinceLastContact?: number | null;
  lastContactedAt?: string | null;
  nextFollowUpAt?: string | null;
  linkedApplicationsCount: number;
  createdAt: string;
}

export interface ContactDetail {
  id: string;
  fullName: string;
  role?: string | null;
  companyId?: string | null;
  companyName?: string | null;
  email?: string | null;
  phone?: string | null;
  linkedInUrl?: string | null;
  type: ContactType;
  warmth: ContactWarmth;
  daysSinceLastContact?: number | null;
  lastContactedAt?: string | null;
  nextFollowUpAt?: string | null;
  notes?: string | null;
  linkedApplications: LinkedApplication[];
  recentInteractions: Interaction[];
  createdAt: string;
  updatedAt: string;
}

export interface CreateContactPayload {
  fullName: string;
  companyId?: string | null;
  companyName?: string | null;
  role?: string | null;
  email?: string | null;
  phone?: string | null;
  linkedInUrl?: string | null;
  type?: ContactType;
  notes?: string | null;
  applicationId?: string | null;
  roleInProcess?: string | null;
}

export interface UpdateContactPayload {
  fullName: string;
  companyId?: string | null;
  companyName?: string | null;
  role?: string | null;
  email?: string | null;
  phone?: string | null;
  linkedInUrl?: string | null;
  type?: ContactType;
  notes?: string | null;
  nextFollowUpAt?: string | null;
}

export interface ContactFilter {
  search?: string;
  companyId?: string;
  type?: ContactType;
  warmth?: ContactWarmth;
}

export interface LinkApplicationContactPayload {
  applicationId: string;
  roleInProcess?: string | null;
}

export interface LogInteractionPayload {
  summary: string;
  contactId?: string | null;
  applicationId?: string | null;
  channel?: InteractionChannel;
  direction?: InteractionDirection;
  occurredAt?: string | null;
  sentContent?: string | null;
  attachmentDocumentId?: string | null;
  followUpRequired?: boolean;
  followUpDueAt?: string | null;
}
