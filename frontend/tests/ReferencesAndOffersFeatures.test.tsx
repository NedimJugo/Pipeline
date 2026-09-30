import React from 'react';
import { describe, it, expect, vi } from 'vitest';
import { render, screen, fireEvent } from '@testing-library/react';
import { QueryClient, QueryClientProvider } from '@tanstack/react-query';
import { ReferenceCard } from '../src/features/references/ReferenceCard';
import { ShareReferenceModal } from '../src/features/references/ShareReferenceModal';
import { OfferScorecardWidget } from '../src/features/offers/OfferScorecardWidget';
import { ReferenceListItem } from '../src/features/references/types';
import { OfferComparisonItem } from '../src/features/offers/types';

const createWrapper = () => {
  const queryClient = new QueryClient({
    defaultOptions: { queries: { retry: false } },
  });
  return ({ children }: { children: React.ReactNode }) => (
    <QueryClientProvider client={queryClient}>{children}</QueryClientProvider>
  );
};

describe('References & Offers Features', () => {
  const mockReferenceAgreed: ReferenceListItem = {
    id: 'ref-1',
    fullName: 'Ada Lovelace',
    relationship: 'Former Director of Computing',
    email: 'ada@lovelace.org',
    phone: '+44 20 7946 0991',
    company: 'Analytical Engines Ltd',
    consent: 'Agreed',
    notes: 'Pioneered algorithm architecture',
    lastNotifiedAt: '2026-09-20T10:00:00Z',
    sharedCount: 3,
    createdAt: '2026-09-01T10:00:00Z',
    updatedAt: '2026-09-20T10:00:00Z',
  };

  const mockReferenceNotAsked: ReferenceListItem = {
    ...mockReferenceAgreed,
    id: 'ref-2',
    fullName: 'Charles Babbage',
    consent: 'NotAsked',
  };

  it('ReferenceCard renders contact details, relationship, and consent badge', () => {
    const onEdit = vi.fn();
    const onShare = vi.fn();
    const onNotify = vi.fn();

    render(
      <ReferenceCard
        reference={mockReferenceAgreed}
        onEdit={onEdit}
        onShare={onShare}
        onNotify={onNotify}
      />,
      { wrapper: createWrapper() }
    );

    expect(screen.getByText('Ada Lovelace')).toBeInTheDocument();
    expect(screen.getByText('Former Director of Computing')).toBeInTheDocument();
    expect(screen.getByText('Analytical Engines Ltd')).toBeInTheDocument();
    expect(screen.getByText('Agreed')).toBeInTheDocument();
    expect(screen.getByText('3 apps')).toBeInTheDocument();
    expect(screen.getByRole('button', { name: /notify/i })).toBeInTheDocument();
    expect(screen.getByRole('button', { name: /share/i })).toBeInTheDocument();
  });

  it('ShareReferenceModal displays warning and enforces confirmation when consent is NotAsked', () => {
    const onClose = vi.fn();

    render(
      <ShareReferenceModal
        isOpen={true}
        onClose={onClose}
        reference={mockReferenceNotAsked}
      />,
      { wrapper: createWrapper() }
    );

    // Warning is displayed
    expect(screen.getByText(/consent warning/i)).toBeInTheDocument();
    expect(screen.getByText(/currently set to/i)).toBeInTheDocument();

    const submitBtn = screen.getByRole('button', { name: /confirm & share/i });
    expect(submitBtn).toBeDisabled();

    // Check confirmation checkbox
    const checkbox = screen.getByRole('checkbox');
    fireEvent.click(checkbox);
    expect(checkbox).toBeChecked();
  });

  it('OfferScorecardWidget renders criteria and calculates composite score', () => {
    const mockOffers: OfferComparisonItem[] = [
      {
        applicationId: 'app-1',
        roleTitle: 'Staff Infrastructure Architect',
        companyId: 'comp-1',
        companyName: 'CloudCorp',
        status: 'Offer',
        workMode: 'Remote',
        employmentType: 'FullTime',
        offerSalary: 210000,
        offerBonus: 30000,
        totalCompensation: 240000,
        currency: 'USD',
        excitementRating: 5,
        priority: 3,
      },
      {
        applicationId: 'app-2',
        roleTitle: 'Principal Systems Engineer',
        companyId: 'comp-2',
        companyName: 'FintechGlobal',
        status: 'Offer',
        workMode: 'Hybrid',
        employmentType: 'FullTime',
        offerSalary: 230000,
        offerBonus: 20000,
        totalCompensation: 250000,
        currency: 'USD',
        excitementRating: 4,
        priority: 2,
      },
    ];

    render(<OfferScorecardWidget offers={mockOffers} />);

    expect(screen.getByText(/weighted decision matrix/i)).toBeInTheDocument();
    expect(screen.getByText('CloudCorp')).toBeInTheDocument();
    expect(screen.getByText('FintechGlobal')).toBeInTheDocument();
    expect(screen.getAllByText(/compensation & equity/i).length).toBeGreaterThanOrEqual(1);
    expect(screen.getByText(/weighted composite score/i)).toBeInTheDocument();
  });
});
