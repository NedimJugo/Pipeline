import { describe, it, expect, vi } from 'vitest';
import { render, screen, fireEvent, waitFor } from '@testing-library/react';
import React from 'react';
import { QueryClient, QueryClientProvider } from '@tanstack/react-query';
import { JobDescriptionHighlighter } from '@/features/applications/components/JobDescriptionHighlighter';
import { StatusStepper } from '@/features/applications/components/StatusStepper';
import { TerminalStatusModal } from '@/features/applications/components/TerminalStatusModal';
import { CreateApplicationModal } from '@/features/applications/components/CreateApplicationModal';

describe('JobDescriptionHighlighter', () => {
  it('renders empty state when no job description is provided', () => {
    render(<JobDescriptionHighlighter jobDescription="" />);
    expect(screen.getByText('No Job Description Provided')).toBeInTheDocument();
  });

  it('detects and highlights skills in job description text', () => {
    const jd = 'We are looking for a Senior Engineer with deep React, TypeScript, and Docker experience.';
    render(<JobDescriptionHighlighter jobDescription={jd} />);

    // Check detected skills header & badges
    expect(screen.getByText(/Detected Skills & Technologies/i)).toBeInTheDocument();
    expect(screen.getByRole('button', { name: 'React' })).toBeInTheDocument();
    expect(screen.getByRole('button', { name: 'TypeScript' })).toBeInTheDocument();
    expect(screen.getByRole('button', { name: 'Docker' })).toBeInTheDocument();
  });
});

describe('StatusStepper', () => {
  it('renders all pipeline stages and triggers status select', () => {
    const onSelect = vi.fn();
    render(<StatusStepper currentStatus="Interview" onStatusSelect={onSelect} />);

    expect(screen.getByRole('button', { name: /Interview/i })).toBeInTheDocument();
    const offerButton = screen.getByRole('button', { name: /Offer/i });
    fireEvent.click(offerButton);
    expect(onSelect).toHaveBeenCalledWith('Offer');
  });

  it('displays terminal status badge when in terminal state', () => {
    render(<StatusStepper currentStatus="Rejected" onStatusSelect={vi.fn()} />);
    expect(screen.getByText('Terminal status: Rejected')).toBeInTheDocument();
  });
});

describe('TerminalStatusModal', () => {
  it('captures closure details and submits payload', async () => {
    const onSubmit = vi.fn().mockResolvedValue(undefined);
    const onClose = vi.fn();

    render(
      <TerminalStatusModal
        isOpen={true}
        onClose={onClose}
        targetStatus="Rejected"
        onSubmit={onSubmit}
      />
    );

    expect(screen.getByText('Mark Application as Closed')).toBeInTheDocument();

    const lessonInput = screen.getByPlaceholderText(/What could be improved next time/i);
    fireEvent.change(lessonInput, { target: { value: 'Need more practice on graph algorithms' } });

    const submitButton = screen.getByRole('button', { name: /Confirm Status Change/i });
    fireEvent.click(submitButton);

    await waitFor(() => {
      expect(onSubmit).toHaveBeenCalledWith(
        expect.objectContaining({
          status: 'Rejected',
          lessonsLearned: 'Need more practice on graph algorithms',
        })
      );
      expect(onClose).toHaveBeenCalled();
    });
  });
});

describe('CreateApplicationModal Date Validation', () => {
  it('renders application date input with max set to today and allows past dates', () => {
    const queryClient = new QueryClient({
      defaultOptions: { queries: { retry: false } },
    });

    render(
      <QueryClientProvider client={queryClient}>
        <CreateApplicationModal isOpen={true} onClose={vi.fn()} />
      </QueryClientProvider>
    );

    const todayStr = new Date().toISOString().split('T')[0];
    const dateInput = screen.getByLabelText(/Application Date/i) as HTMLInputElement;
    expect(dateInput).toBeInTheDocument();
    expect(dateInput.value).toBe(todayStr);
    expect(dateInput.max).toBe(todayStr);

    // Can change to a previous past date
    fireEvent.change(dateInput, { target: { value: '2026-01-15' } });
    expect(dateInput.value).toBe('2026-01-15');
  });
});
