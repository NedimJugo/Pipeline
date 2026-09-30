export type TaskSource = 'Manual' | 'Auto';

export interface TaskItem {
  id: string;
  applicationId?: string | null;
  applicationTitle?: string | null;
  companyName?: string | null;
  contactId?: string | null;
  contactName?: string | null;
  interviewId?: string | null;
  interviewTitle?: string | null;
  title: string;
  notes?: string | null;
  dueAt?: string | null;
  completedAt?: string | null;
  source: TaskSource;
  autoRuleKey?: string | null;
  createdAt: string;
}

export interface CreateTaskRequest {
  title: string;
  notes?: string | null;
  dueAt?: string | null;
  applicationId?: string | null;
  contactId?: string | null;
  interviewId?: string | null;
  source?: TaskSource;
  autoRuleKey?: string | null;
}

export interface UpdateTaskRequest {
  title: string;
  notes?: string | null;
  dueAt?: string | null;
  applicationId?: string | null;
  contactId?: string | null;
  interviewId?: string | null;
}

export interface SnoozeTaskRequest {
  days: number;
}

export interface TaskFilterParams {
  view?: 'today' | 'upcoming' | 'overdue' | 'done' | 'all';
  source?: TaskSource;
  search?: string;
  page?: number;
  pageSize?: number;
}

export interface PagedTasksResult {
  items: TaskItem[];
  totalCount: number;
  page: number;
  pageSize: number;
}
