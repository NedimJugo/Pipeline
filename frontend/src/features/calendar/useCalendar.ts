import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query';
import { calendarApi } from './calendar-api';

export const CALENDAR_QUERY_KEY = ['calendar'];

export function useCalendarEvents(from?: string, to?: string) {
  return useQuery({
    queryKey: [...CALENDAR_QUERY_KEY, 'events', from, to],
    queryFn: () => calendarApi.getEvents(from, to),
  });
}

export function useCalendarFeedUrl() {
  return useQuery({
    queryKey: [...CALENDAR_QUERY_KEY, 'feed-url'],
    queryFn: () => calendarApi.getFeedUrl(),
  });
}

export function useRotateCalendarToken() {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: () => calendarApi.rotateToken(),
    onSuccess: (data) => {
      queryClient.setQueryData([...CALENDAR_QUERY_KEY, 'feed-url'], data);
    },
  });
}
