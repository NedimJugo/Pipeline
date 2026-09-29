import { describe, it, expect, vi } from 'vitest';
import { render, screen, fireEvent, waitFor } from '@testing-library/react';
import React from 'react';
import { BrowserRouter } from 'react-router-dom';
import { QueryClient, QueryClientProvider } from '@tanstack/react-query';
import { PrepChecklistCard } from '@/features/interviews/components/PrepChecklistCard';
import { QuestionsLogCard } from '@/features/interviews/components/QuestionsLogCard';
import { DebriefCard } from '@/features/interviews/components/DebriefCard';
import { PrepChecklistItem, InterviewQuestion } from '@/features/interviews/types';

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

describe('PrepChecklistCard', () => {
  const mockChecklist: PrepChecklistItem[] = [
    { text: 'Review system design concepts', done: true },
    { text: 'Prepare 3 STAR stories', done: false },
    { text: 'Test mic and webcam setup', done: false },
  ];

  it('renders progress bar and checklist items', () => {
    renderWithProviders(
      <PrepChecklistCard interviewId="test-int-1" checklist={mockChecklist} />
    );

    expect(screen.getByText('Interview Preparation Checklist')).toBeInTheDocument();
    expect(screen.getByText('1 / 3')).toBeInTheDocument();
    expect(screen.getByText('(33%)')).toBeInTheDocument();
    expect(screen.getByText('Review system design concepts')).toBeInTheDocument();
    expect(screen.getByText('Prepare 3 STAR stories')).toBeInTheDocument();
    expect(screen.getByText('Test mic and webcam setup')).toBeInTheDocument();
  });
});

describe('QuestionsLogCard', () => {
  const mockQuestions: InterviewQuestion[] = [
    {
      id: 'q-1',
      question: 'Design a distributed rate limiter.',
      myAnswer: 'Used token bucket algorithm with Redis cluster.',
      category: 'Technical',
      difficulty: 4,
      wasPrepared: true,
    },
  ];

  it('renders question text, category badge, and difficulty stars', () => {
    renderWithProviders(
      <QuestionsLogCard interviewId="test-int-1" questions={mockQuestions} />
    );

    expect(screen.getByText('Questions Log & Talking Points')).toBeInTheDocument();
    expect(screen.getByText('Design a distributed rate limiter.')).toBeInTheDocument();
    expect(screen.getByText('Technical')).toBeInTheDocument();
    expect(screen.getByText('Prepared')).toBeInTheDocument();
    expect(screen.getByText(/Used token bucket algorithm/i)).toBeInTheDocument();
  });

  it('toggles add question form on button click', () => {
    renderWithProviders(
      <QuestionsLogCard interviewId="test-int-1" questions={[]} />
    );

    const addBtn = screen.getByRole('button', { name: /Add Question/i });
    fireEvent.click(addBtn);

    expect(screen.getByText('Log New Interview Question')).toBeInTheDocument();
    expect(screen.getByPlaceholderText(/Tell me about a time/i)).toBeInTheDocument();
  });
});

describe('DebriefCard', () => {
  it('renders self-rating selector and thank-you template copy button', async () => {
    renderWithProviders(
      <DebriefCard
        interviewId="test-int-1"
        roleTitle="Senior Backend Engineer"
        companyName="Stripe"
        initialSelfRating={4}
        initialWentWell="Great architecture discussion."
        initialToImprove="Need to brush up on Redis replication."
        initialThankYouSent={false}
        initialOutcomeNotes=""
      />
    );

    expect(screen.getByText('Post-Interview Debrief & Self-Assessment')).toBeInTheDocument();
    expect(screen.getByText(/Strong — Demonstrated expertise/i)).toBeInTheDocument();
    expect(screen.getByDisplayValue('Great architecture discussion.')).toBeInTheDocument();
    expect(screen.getByRole('button', { name: /Copy Thank-You Email Template/i })).toBeInTheDocument();
  });
});
