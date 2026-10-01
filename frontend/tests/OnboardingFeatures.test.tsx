import { describe, it, expect, vi } from 'vitest';
import { render, screen, fireEvent, waitFor } from '@testing-library/react';
import React from 'react';
import { QueryClient, QueryClientProvider } from '@tanstack/react-query';
import { AppTourModal } from '@/features/onboarding/AppTourModal';
import { OnboardingWizardModal } from '@/features/onboarding/OnboardingWizardModal';

const createWrapper = () => {
  const queryClient = new QueryClient({
    defaultOptions: { queries: { retry: false }, mutations: { retry: false } },
  });
  return ({ children }: { children: React.ReactNode }) => (
    <QueryClientProvider client={queryClient}>{children}</QueryClientProvider>
  );
};

describe('AppTourModal', () => {
  it('renders tour slides and allows stepping forward and backward', () => {
    const onClose = vi.fn();
    render(<AppTourModal isOpen={true} onClose={onClose} />);

    // Slide 1: Today Dashboard
    expect(screen.getByText(/Your Daily Command Center/i)).toBeInTheDocument();
    expect(screen.getAllByText(/Do Today/i).length).toBeGreaterThan(0);

    // Click Next -> Slide 2
    const nextBtn = screen.getByRole('button', { name: /Next/i });
    fireEvent.click(nextBtn);
    expect(screen.getByText(/Visual Drag-and-Drop Pipeline/i)).toBeInTheDocument();

    // Click Previous -> Slide 1
    const prevBtn = screen.getByRole('button', { name: /Previous/i });
    fireEvent.click(prevBtn);
    expect(screen.getByText(/Your Daily Command Center/i)).toBeInTheDocument();
  });
});

describe('OnboardingWizardModal', () => {
  it('navigates through Step 1 (Preferences), Step 2 (Tour), and completes on Step 3 (Skip / Finish)', async () => {
    const onComplete = vi.fn();
    render(
      <OnboardingWizardModal isOpen={true} onComplete={onComplete} />,
      { wrapper: createWrapper() }
    );

    // Step 1: Preferences
    expect(screen.getByText(/Set Up Your Job Search Profile/i)).toBeInTheDocument();
    const roleInput = screen.getByPlaceholderText(/e\.g\. Senior Software Engineer/i);
    fireEvent.change(roleInput, { target: { value: 'Staff Platform Engineer' } });

    // Click Continue to Tour
    const toTourBtn = screen.getByRole('button', { name: /Continue to Tour/i });
    fireEvent.click(toTourBtn);

    // Step 2: Tour slides
    await waitFor(() => {
      expect(screen.getByText(/Welcome to Pipeline Tour/i)).toBeInTheDocument();
    });
    const toQuickStartBtn = screen.getByRole('button', { name: /Continue to Quick Start/i });
    fireEvent.click(toQuickStartBtn);

    // Step 3: Fast Start / First Application
    expect(screen.getByText(/Log Your First Opportunity/i)).toBeInTheDocument();
    const skipBtn = screen.getByRole('button', { name: /Skip to Dashboard/i });
    fireEvent.click(skipBtn);

    await waitFor(() => {
      expect(onComplete).toHaveBeenCalled();
    });
  });
});
