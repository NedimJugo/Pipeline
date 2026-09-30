export type CalendarEventType = 'Interview' | 'Task' | 'OfferDeadline' | 'FollowUp';

export interface CalendarEvent {
  id: string;
  title: string;
  type: CalendarEventType;
  startAt: string;
  endAt?: string | null;
  isAllDay: boolean;
  description?: string | null;
  location?: string | null;
  applicationId?: string | null;
  roleTitle?: string | null;
  companyName?: string | null;
  contactId?: string | null;
  contactName?: string | null;
  status?: string | null;
  url?: string | null;
}

export interface CalendarFeedUrl {
  feedUrl: string;
  webcalUrl: string;
  token: string;
}
