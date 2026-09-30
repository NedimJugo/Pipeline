import { useQuery } from '@tanstack/react-query';
import { analyticsApi } from './analytics-api';

export const ANALYTICS_QUERY_KEY = ['analytics'];

export function useAnalyticsOverview(days?: number | null) {
  return useQuery({
    queryKey: [...ANALYTICS_QUERY_KEY, 'overview', days],
    queryFn: () => analyticsApi.getOverview(days),
  });
}

export function useAnalyticsFunnel(days?: number | null) {
  return useQuery({
    queryKey: [...ANALYTICS_QUERY_KEY, 'funnel', days],
    queryFn: () => analyticsApi.getFunnel(days),
  });
}

export function useAnalyticsInsights(days?: number | null) {
  return useQuery({
    queryKey: [...ANALYTICS_QUERY_KEY, 'insights', days],
    queryFn: () => analyticsApi.getInsights(days),
  });
}
