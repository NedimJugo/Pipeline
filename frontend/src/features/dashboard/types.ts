import { TaskItem } from '@/features/tasks/types';

export interface UpcomingInterview {
  id: string;
  applicationId: string;
  roleTitle: string;
  companyName: string;
  type: string;
  format: string;
  scheduledAt: string;
  countdownText: string;
  prepChecklistTotal: number;
  prepChecklistCompleted: number;
  prepProgressPercent: number;
}

export interface DashboardWeeklyStats {
  applicationsSent: number;
  responses: number;
  interviews: number;
  offers: number;
}

export interface StaleApplication {
  id: string;
  companyName: string;
  roleTitle: string;
  status: string;
  daysSinceUpdate: number;
  staleAfterDays: number;
}

export interface DashboardSummary {
  greeting: string;
  searchStatus: string;
  targetRole?: string | null;
  doToday: TaskItem[];
  upcomingInterviews: UpcomingInterview[];
  thisWeekStats: DashboardWeeklyStats;
  staleApplications: StaleApplication[];
  activeApplicationsCount: number;
  activeOffersCount: number;
}
