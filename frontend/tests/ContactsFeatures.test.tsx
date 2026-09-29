import { describe, it, expect, vi } from 'vitest';
import { render, screen, fireEvent, waitFor } from '@testing-library/react';
import React from 'react';
import { BrowserRouter } from 'react-router-dom';
import { QueryClient, QueryClientProvider } from '@tanstack/react-query';
import { WarmthBadge } from '@/features/contacts/components/WarmthBadge';
import { ContactCard } from '@/features/contacts/components/ContactCard';
import { ContactTimeline } from '@/features/contacts/components/ContactTimeline';
import { ContactListItem, Interaction } from '@/features/contacts/types';

const queryClient = new QueryClient({
  defaultOptions: {
    queries: {
      retry: false,
    },
  },
});

const renderWithProviders = (ui: React.ReactElement) => {
  return render(
    <QueryClientProvider client={queryClient}>
      <BrowserRouter>{ui}</BrowserRouter>
    </QueryClientProvider>
  );
};

describe('WarmthBadge', () => {
  it('renders correct labels and classes for Hot warmth', () => {
    render(<WarmthBadge warmth="Hot" />);
    expect(screen.getByText('Hot')).toBeInTheDocument();
  });

  it('renders correct labels for Warm, Cooling, and Cold', () => {
    const { rerender } = render(<WarmthBadge warmth="Warm" />);
    expect(screen.getByText('Warm')).toBeInTheDocument();

    rerender(<WarmthBadge warmth="Cooling" />);
    expect(screen.getByText('Cooling')).toBeInTheDocument();

    rerender(<WarmthBadge warmth="Cold" />);
    expect(screen.getByText('Cold')).toBeInTheDocument();
  });
});

describe('ContactCard', () => {
  const mockContact: ContactListItem = {
    id: 'test-contact-1',
    fullName: 'Jane Recruiter',
    role: 'Senior Technical Sourcer',
    companyName: 'Acme Corp',
    companyId: 'company-1',
    email: 'jane@acme.com',
    phone: '+1 555-0199',
    linkedInUrl: 'https://linkedin.com/in/janerecruiter',
    type: 'Recruiter',
    warmth: 'Hot',
    daysSinceLastContact: 3,
    lastContactedAt: new Date().toISOString(),
    nextFollowUpAt: new Date(Date.now() + 86400000).toISOString(),
    linkedApplicationsCount: 2,
    createdAt: new Date().toISOString(),
  };

  it('renders contact identity, warmth badge, and details', () => {
    renderWithProviders(
      <ContactCard
        contact={mockContact}
        onLogInteraction={vi.fn()}
      />
    );

    expect(screen.getByText('Jane Recruiter')).toBeInTheDocument();
    expect(screen.getByText(/Senior Technical Sourcer/i)).toBeInTheDocument();
    expect(screen.getByText(/Acme Corp/i)).toBeInTheDocument();
    expect(screen.getByText('Hot')).toBeInTheDocument();
    expect(screen.getByTitle(/Hot \(3d ago\)/i)).toBeInTheDocument();
    expect(screen.getByText('2 Apps')).toBeInTheDocument();
  });

  it('invokes callback when Log Interaction is clicked', () => {
    const onLog = vi.fn();
    renderWithProviders(
      <ContactCard
        contact={mockContact}
        onLogInteraction={onLog}
      />
    );

    const logButton = screen.getByRole('button', { name: /Log Interaction/i });
    fireEvent.click(logButton);
    expect(onLog).toHaveBeenCalledWith(mockContact);
  });
});

describe('ContactTimeline', () => {
  const mockInteractions: Interaction[] = [
    {
      id: 'int-1',
      contactId: 'c-1',
      contactName: 'Jane Recruiter',
      channel: 'LinkedIn',
      direction: 'Inbound',
      occurredAt: new Date().toISOString(),
      summary: 'Jane reached out with open Senior Staff position',
      sentContent: 'Hey there! Are you open to discussing opportunities?',
      followUpRequired: true,
      followUpDueAt: new Date(Date.now() + 86400000).toISOString(),
      createdAt: new Date().toISOString(),
    },
    {
      id: 'int-2',
      contactId: 'c-1',
      contactName: 'Jane Recruiter',
      channel: 'Email',
      direction: 'Outbound',
      occurredAt: new Date(Date.now() - 3600000).toISOString(),
      summary: 'Sent updated CV and portfolio link',
      followUpRequired: false,
      createdAt: new Date().toISOString(),
    },
  ];

  it('renders interaction cards with channel badges and summary', () => {
    renderWithProviders(
      <ContactTimeline interactions={mockInteractions} />
    );

    expect(
      screen.getByText('Jane reached out with open Senior Staff position')
    ).toBeInTheDocument();
    expect(
      screen.getByText('Sent updated CV and portfolio link')
    ).toBeInTheDocument();
    expect(screen.getByText('Inbound')).toBeInTheDocument();
    expect(screen.getByText('Outbound')).toBeInTheDocument();
    expect(screen.getByText('LinkedIn')).toBeInTheDocument();
    expect(screen.getByText('Email')).toBeInTheDocument();
    expect(screen.getByText(/Follow-up Due/i)).toBeInTheDocument();
  });

  it('displays empty state when no interactions exist', () => {
    renderWithProviders(<ContactTimeline interactions={[]} />);
    expect(screen.getByText('No Interactions Logged')).toBeInTheDocument();
  });
});
