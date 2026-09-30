import { render, screen, fireEvent } from '@testing-library/react';
import { describe, it, expect, vi } from 'vitest';
import React from 'react';
import { BrowserRouter } from 'react-router-dom';
import { FunnelChart } from '@/features/analytics/FunnelChart';
import { BreakdownTables } from '@/features/analytics/BreakdownTables';
import { InsightCardsGrid } from '@/features/analytics/InsightCardsGrid';
import { MonthView } from '@/features/calendar/MonthView';
import { AgendaView } from '@/features/calendar/AgendaView';
import { CalendarEvent } from '@/features/calendar/types';

describe('Analytics Features', () => {
  it('FunnelChart renders pipeline stages and throughput percentage', () => {
    const mockStages = [
      { stage: 'Applied', count: 10, conversionFromPrevious: 100, conversionFromApplied: 100 },
      { stage: 'Screening', count: 8, conversionFromPrevious: 80, conversionFromApplied: 80 },
      { stage: 'Interview', count: 5, conversionFromPrevious: 62.5, conversionFromApplied: 50 },
      { stage: 'Offer', count: 2, conversionFromPrevious: 40, conversionFromApplied: 20 },
      { stage: 'Accepted', count: 1, conversionFromPrevious: 50, conversionFromApplied: 10 },
    ];

    render(<FunnelChart stages={mockStages} />);

    expect(screen.getByText('Pipeline Conversion Funnel')).toBeInTheDocument();
    expect(screen.getByText('10%')).toBeInTheDocument();
    expect(screen.getAllByText('Screening').length).toBeGreaterThan(0);
    expect(screen.getByText('80% of total')).toBeInTheDocument();
  });

  it('BreakdownTables toggles tabs and displays conversion metrics', () => {
    const mockSource = [
      {
        groupKey: 'LinkedIn',
        label: 'LinkedIn',
        totalApplications: 5,
        responses: 3,
        responseRate: 60,
        interviews: 2,
        interviewRate: 40,
        offers: 1,
        offerRate: 20,
      },
    ];

    const mockDocument = [
      {
        groupKey: 'v1',
        label: 'Senior CV (v1)',
        totalApplications: 4,
        responses: 2,
        responseRate: 50,
        interviews: 1,
        interviewRate: 25,
        offers: 0,
        offerRate: 0,
      },
    ];

    render(
      <BreakdownTables
        bySource={mockSource}
        byDocument={mockDocument}
        byWorkMode={[]}
      />
    );

    // Initial Source view
    expect(screen.getByText('Application Source')).toBeInTheDocument();
    expect(screen.getByText('LinkedIn')).toBeInTheDocument();
    expect(screen.getByText('60%')).toBeInTheDocument();

    // Switch to CV Version
    fireEvent.click(screen.getByText('CV Version'));
    expect(screen.getByText('Senior CV (v1)')).toBeInTheDocument();
    expect(screen.getByText('50%')).toBeInTheDocument();
  });

  it('InsightCardsGrid renders actionable observations with badges', () => {
    const mockInsights = [
      {
        id: 'referral_advantage',
        title: 'Referrals Yield Higher Interview Rates',
        description: 'Referrals convert 3.1x more than cold job boards.',
        type: 'positive' as const,
        metric: '3.1x Higher',
        minSampleSizeMet: true,
      },
    ];

    render(<InsightCardsGrid insights={mockInsights} />);

    expect(screen.getByText('Referrals Yield Higher Interview Rates')).toBeInTheDocument();
    expect(screen.getByText('3.1x Higher')).toBeInTheDocument();
  });
});

describe('Calendar Features', () => {
  const mockEvents: CalendarEvent[] = [
    {
      id: 'inv_1',
      title: 'Technical Screen @ Stripe',
      type: 'Interview',
      startAt: new Date().toISOString(),
      isAllDay: false,
      location: 'Zoom',
      companyName: 'Stripe',
      url: '/interviews/1',
    },
    {
      id: 'task_1',
      title: 'Review System Design doc',
      type: 'Task',
      startAt: new Date().toISOString(),
      isAllDay: true,
      url: '/tasks',
    },
  ];

  it('MonthView renders event badges on day cell', () => {
    render(
      <BrowserRouter>
        <MonthView currentDate={new Date()} events={mockEvents} />
      </BrowserRouter>
    );

    expect(screen.getByText(/Technical Screen @ Stripe/i)).toBeInTheDocument();
    expect(screen.getByText(/Review System Design doc/i)).toBeInTheDocument();
  });

  it('AgendaView groups events and displays location and link', () => {
    render(
      <BrowserRouter>
        <AgendaView events={mockEvents} />
      </BrowserRouter>
    );

    expect(screen.getByText('Technical Screen @ Stripe')).toBeInTheDocument();
    expect(screen.getByText('Zoom')).toBeInTheDocument();
    expect(screen.getByText('Review System Design doc')).toBeInTheDocument();
  });
});
