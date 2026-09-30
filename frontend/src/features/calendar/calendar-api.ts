import { api } from '@/lib/api-client';
import { CalendarEvent, CalendarFeedUrl } from './types';

export const calendarApi = {
  getEvents: async (from?: string, to?: string) => {
    const params = new URLSearchParams();
    if (from) params.append('from', from);
    if (to) params.append('to', to);

    const { data } = await api.get<CalendarEvent[]>('/api/calendar', { params });
    return data;
  },

  getFeedUrl: async () => {
    const { data } = await api.get<CalendarFeedUrl>('/api/calendar/feed-url');
    return data;
  },

  rotateToken: async () => {
    const { data } = await api.post<CalendarFeedUrl>('/api/calendar/rotate-token');
    return data;
  },
};
