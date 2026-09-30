import { api } from '@/lib/api-client';
import { OfferComparisonItem, OfferComparisonView, UpdateOfferPayload } from './types';

export const offersApi = {
  getAvailableOffers: async () => {
    const { data } = await api.get<OfferComparisonItem[]>('/api/offers');
    return data;
  },

  compareOffers: async (ids?: string[]) => {
    const params = new URLSearchParams();
    if (ids && ids.length > 0) {
      ids.forEach((id) => params.append('ids', id));
    }
    const { data } = await api.get<OfferComparisonView>('/api/offers/compare', { params });
    return data;
  },

  updateOfferDetails: async (applicationId: string, payload: UpdateOfferPayload) => {
    const { data } = await api.put<OfferComparisonItem>(`/api/offers/application/${applicationId}`, payload);
    return data;
  },
};
