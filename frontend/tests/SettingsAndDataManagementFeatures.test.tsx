import { render, screen, fireEvent, waitFor } from '@testing-library/react';
import { describe, it, expect, vi, beforeEach } from 'vitest';
import React from 'react';
import { BrowserRouter } from 'react-router-dom';
import { QueryClient, QueryClientProvider } from '@tanstack/react-query';
import { ProfileSettingsTab } from '@/features/settings/ProfileSettingsTab';
import { PipelineSettingsTab } from '@/features/settings/PipelineSettingsTab';
import { DataManagementTab } from '@/features/settings/DataManagementTab';
import { AppearanceTab } from '@/features/settings/AppearanceTab';
import { CommandPalette } from '@/components/CommandPalette';
import { OfflineBanner } from '@/components/OfflineBanner';
import { UserSettingsProfile } from '@/features/settings/types';
import { settingsApi } from '@/features/settings/settings-api';

vi.mock('@/features/settings/settings-api', () => ({
  settingsApi: {
    getProfile: vi.fn(),
    updateProfile: vi.fn().mockResolvedValue({}),
    updatePreferences: vi.fn().mockResolvedValue({}),
    exportApplicationsCsv: vi.fn().mockResolvedValue(undefined),
    exportGdprData: vi.fn().mockResolvedValue(undefined),
    importApplicationsCsv: vi.fn().mockResolvedValue({
      totalProcessed: 5,
      createdCount: 4,
      updatedCount: 1,
      failedCount: 0,
      errors: [],
    }),
    deleteAccount: vi.fn().mockResolvedValue(undefined),
    seedDemoData: vi.fn().mockResolvedValue({
      message: 'Demo applications seeded successfully',
      seededCount: 10,
    }),
  },
}));

const mockProfile: UserSettingsProfile = {
  id: 'user-1',
  email: 'tester@pipeline.io',
  displayName: 'Nedim Developer',
  targetRole: 'Senior Full Stack Engineer',
  seniority: 'Senior',
  location: 'Remote',
  salaryExpectationMin: 90000,
  salaryExpectationMax: 120000,
  currency: 'USD',
  searchStatus: 'Active',
  timezone: 'UTC',
  staleAfterDays: 14,
  onboardingCompleted: true,
  notificationPrefs: '{}',
  createdAt: '2026-09-01T00:00:00Z',
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

describe('Settings and Data Management Features', () => {
  beforeEach(() => {
    vi.clearAllMocks();
  });

  describe('ProfileSettingsTab', () => {
    it('renders profile fields and handles profile update submission', async () => {
      renderWithProviders(<ProfileSettingsTab profile={mockProfile} />);

      expect(screen.getByDisplayValue('Nedim Developer')).toBeInTheDocument();
      expect(screen.getByDisplayValue('Senior Full Stack Engineer')).toBeInTheDocument();
      expect(screen.getByDisplayValue('90000')).toBeInTheDocument();
      expect(screen.getByDisplayValue('120000')).toBeInTheDocument();

      const roleInput = screen.getByDisplayValue('Senior Full Stack Engineer');
      fireEvent.change(roleInput, { target: { value: 'Staff Engineer' } });

      const saveBtn = screen.getByRole('button', { name: /save profile changes/i });
      fireEvent.click(saveBtn);

      await waitFor(() => {
        expect(settingsApi.updateProfile).toHaveBeenCalledWith(
          expect.objectContaining({
            displayName: 'Nedim Developer',
            targetRole: 'Staff Engineer',
            seniority: 'Senior',
            salaryExpectationMin: 90000,
            salaryExpectationMax: 120000,
          })
        );
      });
    });
  });

  describe('PipelineSettingsTab', () => {
    it('renders stale days threshold and handles preference update', async () => {
      renderWithProviders(<PipelineSettingsTab profile={mockProfile} />);

      expect(screen.getByText('Stale Inactivity Threshold')).toBeInTheDocument();
      expect(screen.getByText(/14\s*Days/i)).toBeInTheDocument();

      // Click on 21 Days option
      const day21Button = screen.getByText(/21\s*Days/);
      fireEvent.click(day21Button);

      const saveBtn = screen.getByRole('button', { name: /save pipeline rules/i });
      fireEvent.click(saveBtn);

      await waitFor(() => {
        expect(settingsApi.updatePreferences).toHaveBeenCalledWith({
          staleAfterDays: 21,
        });
      });
    });
  });

  describe('DataManagementTab', () => {
    it('renders export, import, seed demo, and account purge buttons', () => {
      renderWithProviders(<DataManagementTab />);

      expect(screen.getByRole('button', { name: /download applications \(\.csv\)/i })).toBeInTheDocument();
      expect(screen.getByRole('button', { name: /download complete archive \(\.json\)/i })).toBeInTheDocument();
      expect(screen.getByRole('button', { name: /import from csv/i })).toBeInTheDocument();
      expect(screen.getByRole('button', { name: /seed demo data/i })).toBeInTheDocument();
      expect(screen.getByRole('button', { name: /delete account/i })).toBeInTheDocument();

      // Clicking Download CSV
      fireEvent.click(screen.getByRole('button', { name: /download applications \(\.csv\)/i }));
      expect(settingsApi.exportApplicationsCsv).toHaveBeenCalled();

      // Clicking Download JSON
      fireEvent.click(screen.getByRole('button', { name: /download complete archive \(\.json\)/i }));
      expect(settingsApi.exportGdprData).toHaveBeenCalled();
    });

    it('opens and closes CSV import modal', () => {
      renderWithProviders(<DataManagementTab />);

      expect(screen.queryByText('Import Applications from CSV')).not.toBeInTheDocument();

      // Open modal
      fireEvent.click(screen.getByRole('button', { name: /import from csv/i }));
      expect(screen.getByText('Import Applications from CSV')).toBeInTheDocument();
      expect(screen.getByText('Click to select CSV file')).toBeInTheDocument();

      // Close modal
      fireEvent.click(screen.getByRole('button', { name: /close/i }));
      expect(screen.queryByText('Import Applications from CSV')).not.toBeInTheDocument();
    });
  });

  describe('AppearanceTab', () => {
    it('renders color theme switcher and PWA status cards', () => {
      renderWithProviders(<AppearanceTab />);

      expect(screen.getByText('Theme & Appearance')).toBeInTheDocument();
      expect(screen.getByText(/Progressive Web App/i)).toBeInTheDocument();
      expect(screen.getByText(/install pipeline to your desktop or mobile/i)).toBeInTheDocument();
    });
  });

  describe('OfflineBanner', () => {
    it('does not render banner when online', () => {
      vi.spyOn(navigator, 'onLine', 'get').mockReturnValue(true);

      const { container } = renderWithProviders(<OfflineBanner />);
      expect(container.firstChild).toBeNull();
    });

    it('renders offline warning when offline event fires', () => {
      vi.spyOn(navigator, 'onLine', 'get').mockReturnValue(false);

      renderWithProviders(<OfflineBanner />);
      expect(screen.getByText(/working offline/i)).toBeInTheDocument();
    });
  });

  describe('CommandPalette', () => {
    it('renders search input and quick navigation actions when open', () => {
      renderWithProviders(<CommandPalette />);

      // Open command palette via Ctrl+K
      fireEvent.keyDown(window, { key: 'k', ctrlKey: true });

      expect(screen.getByPlaceholderText(/search applications, companies, or quick actions/i)).toBeInTheDocument();
      expect(screen.getByText('Applications Pipeline')).toBeInTheDocument();
      expect(screen.getByText('Recruitment Calendar')).toBeInTheDocument();
      expect(screen.getByText('Analytics & Insights')).toBeInTheDocument();
    });

    it('filters quick navigation actions on typing', () => {
      renderWithProviders(<CommandPalette />);

      // Open command palette via Ctrl+K
      fireEvent.keyDown(window, { key: 'k', ctrlKey: true });

      const input = screen.getByPlaceholderText(/search applications, companies, or quick actions/i);
      fireEvent.change(input, { target: { value: 'Analytics' } });

      expect(screen.getByText('Analytics & Insights')).toBeInTheDocument();
      expect(screen.queryByText('Networking Contacts')).not.toBeInTheDocument();
    });
  });
});
