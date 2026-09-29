import { ContactWarmth } from '../contacts/types';

export type InterviewType =
  | 'HR'
  | 'Technical'
  | 'Culture'
  | 'Manager'
  | 'Final'
  | 'Assignment'
  | 'Other';

export type InterviewFormat = 'Phone' | 'Video' | 'Onsite';

export type InterviewStatus = 'Scheduled' | 'Completed' | 'Cancelled' | 'NoShow';

export type InterviewQuestionCategory =
  | 'Behavioral'
  | 'Technical'
  | 'Situational'
  | 'Salary'
  | 'Other';

export interface PrepChecklistItem {
  text: string;
  done: boolean;
}

export interface InterviewContact {
  contactId: string;
  fullName: string;
  role?: string | null;
  email?: string | null;
  linkedInUrl?: string | null;
  warmth: ContactWarmth;
}

export interface InterviewQuestion {
  id: string;
  question: string;
  myAnswer?: string | null;
  category: InterviewQuestionCategory;
  difficulty: number;
  wasPrepared: boolean;
}

export interface InterviewListItem {
  id: string;
  applicationId: string;
  roleTitle: string;
  companyName: string;
  type: InterviewType;
  format: InterviewFormat;
  scheduledAt: string;
  durationMinutes: number;
  status: InterviewStatus;
  location?: string | null;
  meetingLink?: string | null;
  interviewersCount: number;
  questionsCount: number;
  selfRating?: number | null;
  createdAt: string;
}

export interface InterviewDetail {
  id: string;
  applicationId: string;
  roleTitle: string;
  companyName: string;
  type: InterviewType;
  format: InterviewFormat;
  scheduledAt: string;
  durationMinutes: number;
  status: InterviewStatus;
  location?: string | null;
  meetingLink?: string | null;
  prepNotes?: string | null;
  prepChecklist: PrepChecklistItem[];
  selfRating?: number | null;
  wentWell?: string | null;
  toImprove?: string | null;
  thankYouSent: boolean;
  outcomeNotes?: string | null;
  interviewers: InterviewContact[];
  questions: InterviewQuestion[];
  createdAt: string;
  updatedAt: string;
}

export interface CreateInterviewPayload {
  applicationId: string;
  type?: InterviewType;
  format?: InterviewFormat;
  scheduledAt?: string;
  durationMinutes?: number;
  location?: string;
  meetingLink?: string;
  interviewerContactIds?: string[];
  prepNotes?: string;
  customChecklist?: PrepChecklistItem[];
}

export interface UpdateInterviewPayload {
  type: InterviewType;
  format: InterviewFormat;
  scheduledAt: string;
  durationMinutes: number;
  location?: string | null;
  meetingLink?: string | null;
  status?: InterviewStatus;
  interviewerContactIds?: string[];
  prepNotes?: string | null;
}

export interface UpdateDebriefPayload {
  selfRating?: number | null;
  wentWell?: string | null;
  toImprove?: string | null;
  thankYouSent: boolean;
  outcomeNotes?: string | null;
}

export interface UpdatePrepChecklistPayload {
  checklist: PrepChecklistItem[];
}

export interface AddQuestionPayload {
  question: string;
  myAnswer?: string | null;
  category?: InterviewQuestionCategory;
  difficulty?: number;
  wasPrepared?: boolean;
}

export interface InterviewFilter {
  applicationId?: string;
  status?: InterviewStatus;
  type?: InterviewType;
  upcomingOnly?: boolean;
}
