export type ReferenceConsent = 'NotAsked' | 'Asked' | 'Agreed' | 'Declined';

export interface ApplicationReference {
  id: string;
  applicationId: string;
  roleTitle: string;
  companyName: string;
  sharedAt: string;
  outcome?: string | null;
}

export interface ReferenceListItem {
  id: string;
  fullName: string;
  relationship: string;
  email?: string | null;
  phone?: string | null;
  company?: string | null;
  consent: ReferenceConsent;
  notes?: string | null;
  lastNotifiedAt?: string | null;
  sharedCount: number;
  createdAt: string;
  updatedAt: string;
}

export interface ReferenceDetail {
  id: string;
  fullName: string;
  relationship: string;
  email?: string | null;
  phone?: string | null;
  company?: string | null;
  consent: ReferenceConsent;
  notes?: string | null;
  lastNotifiedAt?: string | null;
  sharedApplications: ApplicationReference[];
  createdAt: string;
  updatedAt: string;
}

export interface CreateReferencePayload {
  fullName: string;
  relationship: string;
  email?: string | null;
  phone?: string | null;
  company?: string | null;
  consent?: ReferenceConsent;
  notes?: string | null;
}

export interface UpdateReferencePayload {
  fullName: string;
  relationship: string;
  email?: string | null;
  phone?: string | null;
  company?: string | null;
  consent?: ReferenceConsent;
  notes?: string | null;
}

export interface ShareReferencePayload {
  applicationId: string;
  outcome?: string | null;
  overrideConsentWarning?: boolean;
}

export interface ShareReferenceResult {
  success: boolean;
  warningTriggered: boolean;
  warningMessage?: string | null;
  sharedRecord?: ApplicationReference | null;
}
