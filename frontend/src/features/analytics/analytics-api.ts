import { api } from '@/lib/api-client';
import {
  AnalyticsOverview,
  FunnelStage,
  BreakdownMetric,
  StageDuration,
  WeeklyVelocity,
  InsightCard,
} from './types';

export const analyticsApi = {
  getOverview: async (days?: number | null) => {
    const params = days ? { days } : {};
    const { data } = await api.get<AnalyticsOverview>('/api/analytics/overview', { params });
    return data;
  },

  getFunnel: async (days?: number | null) => {
    const params = days ? { days } : {};
    const { data } = await api.get<FunnelStage[]>('/api/analytics/funnel', { params });
    return data;
  },

  getBySource: async (days?: number | null) => {
    const params = days ? { days } : {};
    const { data } = await api.get<BreakdownMetric[]>('/api/analytics/by-source', { params });
    return data;
  },

  getByDocument: async (days?: number | null) => {
    const params = days ? { days } : {};
    const { data } = await api.get<BreakdownMetric[]>('/api/analytics/by-document', { params });
    return data;
  },

  getByWorkMode: async (days?: number | null) => {
    const params = days ? { days } : {};
    const { data } = await api.get<BreakdownMetric[]>('/api/analytics/by-work-mode', { params });
    return data;
  },

  getStageDurations: async (days?: number | null) => {
    const params = days ? { days } : {};
    const { data } = await api.get<StageDuration[]>('/api/analytics/stage-durations', { params });
    return data;
  },

  getWeeklyVelocity: async (days?: number | null) => {
    const params = days ? { days } : {};
    const { data } = await api.get<WeeklyVelocity[]>('/api/analytics/weekly', { params });
    return data;
  },

  getInsights: async (days?: number | null) => {
    const params = days ? { days } : {};
    const { data } = await api.get<InsightCard[]>('/api/analytics/insights', { params });
    return data;
  },
};
