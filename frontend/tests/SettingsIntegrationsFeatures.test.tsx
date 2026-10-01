import { describe, it, expect, vi } from 'vitest';
import { render, screen, fireEvent, waitFor } from '@testing-library/react';
import React from 'react';
import { QueryClient, QueryClientProvider } from '@tanstack/react-query';
import { IntegrationsTab } from '@/features/settings/IntegrationsTab';
import * as useSettingsModule from '@/features/settings/useSettings';

const createWrapper = () => {
  const queryClient = new QueryClient({
    defaultOptions: { queries: { retry: false }, mutations: { retry: false } },
  });
  return ({ children }: { children: React.ReactNode }) => (
    <QueryClientProvider client={queryClient}>{children}</QueryClientProvider>
  );
};

describe('SettingsIntegrationsFeatures', () => {
  it('renders integration sections with fallback badges and handles save', async () => {
    const mockUpdateMutation = vi.fn().mockResolvedValue({});
    const mockTestEmailMutation = vi.fn().mockResolvedValue({ success: true, message: 'Sent!' });

    vi.spyOn(useSettingsModule, 'useIntegrations').mockReturnValue({
      data: {
        useCustomSmtp: false,
        smtpHost: 'mailpit.local',
        smtpPort: 1025,
        smtpUser: null,
        hasSmtpPassword: false,
        smtpFrom: 'no-reply@pipeline.local',
        isEnvFallbackSmtp: true,
        useCustomGoogle: false,
        googleClientId: 'google-client-id-env',
        hasGoogleClientSecret: false,
        storageProvider: 'Local',
      },
      isLoading: false,
    } as any);

    vi.spyOn(useSettingsModule, 'useUpdateIntegrations').mockReturnValue({
      mutateAsync: mockUpdateMutation,
      isPending: false,
    } as any);

    vi.spyOn(useSettingsModule, 'useTestEmail').mockReturnValue({
      mutateAsync: mockTestEmailMutation,
      isPending: false,
    } as any);

    render(<IntegrationsTab />, { wrapper: createWrapper() });

    // Assert headers and fallback badge
    expect(screen.getByText(/Email & Notifications \(SMTP\)/i)).toBeInTheDocument();
    expect(screen.getByText(/Active \(\.env fallback\)/i)).toBeInTheDocument();
    expect(screen.getByText(/Google Calendar & OAuth/i)).toBeInTheDocument();
    expect(screen.getByText(/Document Storage Provider/i)).toBeInTheDocument();

    // Toggle custom SMTP
    const customSmtpToggle = screen.getByLabelText(/Enable custom SMTP/i);
    fireEvent.click(customSmtpToggle);

    // Fill new host
    const hostInput = screen.getByPlaceholderText(/e\.g\. smtp\.gmail\.com/i);
    fireEvent.change(hostInput, { target: { value: 'smtp.sendgrid.net' } });

    // Save
    const saveBtn = screen.getByRole('button', { name: /Save Integrations/i });
    fireEvent.click(saveBtn);

    await waitFor(() => {
      expect(mockUpdateMutation).toHaveBeenCalledWith(
        expect.objectContaining({
          useCustomSmtp: true,
          smtpHost: 'smtp.sendgrid.net',
        })
      );
    });
  });
});
