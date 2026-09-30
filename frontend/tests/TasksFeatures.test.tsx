import { describe, it, expect, vi } from 'vitest';
import { render, screen, fireEvent } from '@testing-library/react';
import React from 'react';
import { BrowserRouter } from 'react-router-dom';
import { QueryClient, QueryClientProvider } from '@tanstack/react-query';
import { TaskCard } from '@/features/tasks/TaskCard';
import { DoTodayList } from '@/features/dashboard/DoTodayList';
import { UpcomingInterviewsWidget } from '@/features/dashboard/UpcomingInterviewsWidget';
import { StaleApplicationsWidget } from '@/features/dashboard/StaleApplicationsWidget';
import { EmailTemplatePickerModal } from '@/features/templates/EmailTemplatePickerModal';
import { TaskItem } from '@/features/tasks/types';
import { UpcomingInterview, StaleApplication } from '@/features/dashboard/types';

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

describe('TaskCard', () => {
  const mockTask: TaskItem = {
    id: 'task-1',
    title: 'Follow up with recruiter regarding next steps',
    notes: 'Send polite email inquiring on engineering interview outcome',
    dueAt: new Date().toISOString(),
    completedAt: null,
    source: 'Auto',
    autoRuleKey: 'post_interview_follow_up:int-1',
    applicationId: 'app-1',
    applicationTitle: 'Principal Cloud Engineer',
    companyName: 'Starlight Tech',
    contactId: 'con-1',
    contactName: 'Alice Recruiter',
    interviewId: null,
    interviewTitle: null,
    createdAt: new Date().toISOString(),
  };

  it('renders task title, notes, auto badge, and linked application & contact', () => {
    renderWithProviders(<TaskCard task={mockTask} />);

    expect(screen.getByText('Follow up with recruiter regarding next steps')).toBeDefined();
    expect(screen.getByText('Send polite email inquiring on engineering interview outcome')).toBeDefined();
    expect(screen.getByText('Auto')).toBeDefined();
    expect(screen.getByText('Principal Cloud Engineer @ Starlight Tech')).toBeDefined();
    expect(screen.getByText('Alice Recruiter')).toBeDefined();
    expect(screen.getByText('Due Today')).toBeDefined();
  });
});

describe('DoTodayList', () => {
  it('renders empty state when no tasks are due today', () => {
    renderWithProviders(<DoTodayList tasks={[]} />);
    expect(screen.getByText('All caught up for today!')).toBeDefined();
  });

  it('renders list of tasks with Open and Snooze buttons', () => {
    const tasks: TaskItem[] = [
      {
        id: 'task-2',
        title: 'Review system design guide',
        dueAt: new Date().toISOString(),
        completedAt: null,
        source: 'Manual',
        applicationId: 'app-2',
        companyName: 'CloudCorp',
        createdAt: new Date().toISOString(),
      },
    ];

    renderWithProviders(<DoTodayList tasks={tasks} />);
    expect(screen.getByText('Review system design guide')).toBeDefined();
    expect(screen.getByText('Open')).toBeDefined();
    expect(screen.getByText('Snooze')).toBeDefined();
  });
});

describe('UpcomingInterviewsWidget', () => {
  it('renders interview details, countdown, and prep progress', () => {
    const interviews: UpcomingInterview[] = [
      {
        id: 'int-1',
        applicationId: 'app-1',
        roleTitle: 'Lead Software Architect',
        companyName: 'Horizon Labs',
        type: 'Technical',
        format: 'Video',
        scheduledAt: new Date(Date.now() + 86400000).toISOString(),
        countdownText: 'Tomorrow at 14:00',
        prepChecklistTotal: 4,
        prepChecklistCompleted: 3,
        prepProgressPercent: 75,
      },
    ];

    renderWithProviders(<UpcomingInterviewsWidget interviews={interviews} />);
    expect(screen.getByText('Horizon Labs')).toBeDefined();
    expect(screen.getByText('Lead Software Architect')).toBeDefined();
    expect(screen.getByText('Tomorrow at 14:00')).toBeDefined();
    expect(screen.getByText('3/4 completed (75%)')).toBeDefined();
  });
});

describe('StaleApplicationsWidget', () => {
  it('renders stale applications with Follow Up and Mark Ghosted actions', () => {
    const staleApps: StaleApplication[] = [
      {
        id: 'app-stale-1',
        companyName: 'OldCorp',
        roleTitle: 'DevOps Lead',
        status: 'Applied',
        daysSinceUpdate: 21,
        staleAfterDays: 14,
      },
    ];

    renderWithProviders(<StaleApplicationsWidget applications={staleApps} />);
    expect(screen.getByText('Stale Applications (1)')).toBeDefined();
    expect(screen.getByText('DevOps Lead')).toBeDefined();
    expect(screen.getByText('OldCorp')).toBeDefined();
    expect(screen.getByText('21d inactive')).toBeDefined();
    expect(screen.getByText('Follow Up')).toBeDefined();
    expect(screen.getByText('Mark Ghosted')).toBeDefined();
  });
});
