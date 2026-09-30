export type EmailTemplateCategory = 'FollowUp' | 'ThankYou' | 'Negotiation' | 'Withdraw' | 'Other';

export interface EmailTemplate {
  id: string;
  name: string;
  subject: string;
  body: string;
  category: EmailTemplateCategory;
  isSystem: boolean;
  createdAt: string;
}

export interface CreateEmailTemplateRequest {
  name: string;
  subject: string;
  body: string;
  category: EmailTemplateCategory;
}

export interface UpdateEmailTemplateRequest {
  name: string;
  subject: string;
  body: string;
  category: EmailTemplateCategory;
}

export interface RenderEmailTemplateRequest {
  applicationId?: string | null;
  contactId?: string | null;
}

export interface RenderedEmailTemplate {
  subject: string;
  body: string;
}
