import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query';
import { offersApi } from './offers-api';
import { UpdateOfferPayload } from './types';

export const OFFERS_QUERY_KEY = ['offers'];

export function useAvailableOffers() {
  return useQuery({
    queryKey: OFFERS_QUERY_KEY,
    queryFn: () => offersApi.getAvailableOffers(),
  });
}

export function useOfferComparison(applicationIds?: string[]) {
  return useQuery({
    queryKey: ['offer-comparison', applicationIds],
    queryFn: () => offersApi.compareOffers(applicationIds),
  });
}

export function useUpdateOfferDetails() {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: ({ applicationId, payload }: { applicationId: string; payload: UpdateOfferPayload }) =>
      offersApi.updateOfferDetails(applicationId, payload),
    onSuccess: (_, variables) => {
      queryClient.invalidateQueries({ queryKey: OFFERS_QUERY_KEY });
      queryClient.invalidateQueries({ queryKey: ['offer-comparison'] });
      queryClient.invalidateQueries({ queryKey: ['applications'] });
      queryClient.invalidateQueries({ queryKey: ['application', variables.applicationId] });
      queryClient.invalidateQueries({ queryKey: ['dashboard'] });
    },
  });
}
