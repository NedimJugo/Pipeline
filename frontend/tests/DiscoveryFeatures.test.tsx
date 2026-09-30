import { render, screen, fireEvent, waitFor } from '@testing-library/react';
import { describe, it, expect, vi, beforeEach } from 'vitest';
import React from 'react';
import { BrowserRouter } from 'react-router-dom';
import { QueryClient, QueryClientProvider } from '@tanstack/react-query';
import { DiscoveryExtensionBanner } from '@/features/discovery/DiscoveryExtensionBanner';
import { DiscoveredJobCard } from '@/features/discovery/DiscoveredJobCard';
import { DiscoveryPage } from '@/features/discovery/DiscoveryPage';
import { DiscoveredJob } from '@/features/discovery/types';
import { discoveryApi } from '@/features/discovery/discovery-api';

vi.mock('@/features/discovery/discovery-api', () => ({
  discoveryApi: {
    getJobs: vi.fn(),
    getSources: vi.fn(),
    saveJobToWishlist: vi.fn(),
    dismissJob: vi.fn(),
    ingestJobs: vi.fn(),
  },
}));

const mockJob: DiscoveredJob = {
  id: 'job-101',
  sourceId: 'src-1',
  sourceName: 'Tech RSS',
  externalId: 'ext-99',
  title: 'Principal Distributed Systems Engineer',
  companyName: 'Cloudflare',
  location: 'Remote (US)',
  url: 'https://cloudflare.com/careers/101',
  description: 'Design and build high-throughput edge storage services.',
  postedAt: new Date().toISOString(),
  tags: ['Go', 'Rust', 'Distributed Systems'],
  status: 'New',
  fetchedAt: new Date().toISOString(),
};

const renderWithProviders = (ui: React.ReactElement) => {
  const queryClient = new QueryClient({
    defaultOptions: {
      queries: { retry: false },
      mutations: { retry: false },
    },
  });
  return render(
    <QueryClientProvider client={queryClient}>
      <BrowserRouter>{ui}</BrowserRouter>
    </QueryClientProvider>
  );
};

describe('Job Discovery Features', () => {
  beforeEach(() => {
    vi.clearAllMocks();
  });

  describe('DiscoveryExtensionBanner', () => {
    it('renders architecture summary and toggles code spec', () => {
      render(<DiscoveryExtensionBanner />);

      expect(screen.getByText('Job Discovery Extension Architecture')).toBeInTheDocument();
      expect(screen.getByText('Pluggable Connector API')).toBeInTheDocument();
      expect(screen.queryByText(/IJobSourceConnector/i)).toBeInTheDocument();

      // Click View Spec
      const toggleBtn = screen.getByRole('button', { name: /view spec/i });
      fireEvent.click(toggleBtn);

      expect(screen.getByText(/Implement connector in Pipeline.Infrastructure/i)).toBeInTheDocument();
      expect(screen.getByText(/docs\/discovery.md/i)).toBeInTheDocument();
    });
  });

  describe('DiscoveredJobCard', () => {
    it('renders job details and handles save and dismiss callbacks', () => {
      const handleSave = vi.fn();
      const handleDismiss = vi.fn();

      render(
        <DiscoveredJobCard
          job={mockJob}
          onSave={handleSave}
          onDismiss={handleDismiss}
        />
      );

      expect(screen.getByText('Principal Distributed Systems Engineer')).toBeInTheDocument();
      expect(screen.getByText('Cloudflare')).toBeInTheDocument();
      expect(screen.getByText('Remote (US)')).toBeInTheDocument();
      expect(screen.getByText('Tech RSS')).toBeInTheDocument();
      expect(screen.getByText('New')).toBeInTheDocument();
      expect(screen.getByText('Go')).toBeInTheDocument();
      expect(screen.getByText('Rust')).toBeInTheDocument();

      // Click Save to Wishlist
      const saveBtn = screen.getByRole('button', { name: /save to wishlist/i });
      fireEvent.click(saveBtn);
      expect(handleSave).toHaveBeenCalledWith('job-101');

      // Click Dismiss
      const dismissBtn = screen.getByRole('button', { name: /dismiss/i });
      fireEvent.click(dismissBtn);
      expect(handleDismiss).toHaveBeenCalledWith('job-101');
    });

    it('renders Saved badge and in-wishlist state when job is saved', () => {
      const savedJob: DiscoveredJob = { ...mockJob, status: 'Saved' };

      render(
        <DiscoveredJobCard
          job={savedJob}
          onSave={vi.fn()}
          onDismiss={vi.fn()}
        />
      );

      expect(screen.getByText('Saved')).toBeInTheDocument();
      expect(screen.getByText('In Wishlist')).toBeInTheDocument();
    });
  });

  describe('DiscoveryPage', () => {
    it('renders search input, status tabs, and discovered job cards', async () => {
      vi.mocked(discoveryApi.getJobs).mockResolvedValue({
        items: [mockJob],
        totalCount: 1,
        page: 1,
        pageSize: 18,
      });
      vi.mocked(discoveryApi.getSources).mockResolvedValue([
        {
          id: 'src-1',
          name: 'Tech RSS Feed',
          type: 'Rss',
          baseUrl: 'https://feed.rss',
          enabled: true,
          lastRunAt: null,
        },
      ]);

      renderWithProviders(<DiscoveryPage />);

      expect(screen.getByRole('heading', { level: 1, name: 'Job Discovery' })).toBeInTheDocument();
      expect(screen.getByPlaceholderText(/search by role title, company/i)).toBeInTheDocument();
      expect(screen.getByRole('button', { name: 'New' })).toBeInTheDocument();
      expect(screen.getByRole('button', { name: 'Saved' })).toBeInTheDocument();
      expect(screen.getByRole('button', { name: 'Dismissed' })).toBeInTheDocument();

      await waitFor(() => {
        expect(screen.getByText('Principal Distributed Systems Engineer')).toBeInTheDocument();
      });
    });
  });
});
