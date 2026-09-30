export interface FunnelStage {
  stage: string;
  count: number;
  conversionFromPrevious: number;
  conversionFromApplied: number;
}

export interface BreakdownMetric {
  groupKey: string;
  label: string;
  totalApplications: number;
  responses: number;
  responseRate: number;
  interviews: number;
  interviewRate: number;
  offers: number;
  offerRate: number;
}

export interface StageDuration {
  stage: string;
  averageDays: number;
  medianDays: number;
  sampleCount: number;
}

export interface WeeklyVelocity {
  weekLabel: string;
  weekStartDate: string;
  applicationsCount: number;
  interviewsCount: number;
}

export interface RejectionStage {
  stage: string;
  count: number;
  percentage: number;
}

export interface InsightCard {
  id: string;
  title: string;
  description: string;
  type: 'positive' | 'warning' | 'info';
  metric?: string | null;
  minSampleSizeMet: boolean;
}

export interface AnalyticsOverview {
  funnel: FunnelStage[];
  bySource: BreakdownMetric[];
  byDocument: BreakdownMetric[];
  byWorkMode: BreakdownMetric[];
  stageDurations: StageDuration[];
  weeklyVelocity: WeeklyVelocity[];
  rejectionStages: RejectionStage[];
  insights: InsightCard[];
  totalApplications: number;
  activeApplications: number;
  overallResponseRate: number;
  overallInterviewRate: number;
  dateRangeDays?: number | null;
}
