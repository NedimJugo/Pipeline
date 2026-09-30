import React from 'react';
import { useAuth } from '@/features/auth/AuthContext';
import { useDashboard } from './useDashboard';
import {
  Briefcase,
  CheckCircle2,
  Calendar,
  Sparkles,
  ArrowUpRight,
  TrendingUp,
  MessageSquare,
  FileText,
  Mail,
  ListTodo,
} from 'lucide-react';
import { Link } from 'react-router-dom';
import { DoTodayList } from './DoTodayList';
import { UpcomingInterviewsWidget } from './UpcomingInterviewsWidget';
import { StaleApplicationsWidget } from './StaleApplicationsWidget';
import { QuickAddMenu } from './QuickAddMenu';

export const DashboardPage: React.FC = () => {
  const { user } = useAuth();
  const { data: dashboard, isLoading } = useDashboard();

  return (
    <div className="max-w-6xl mx-auto space-y-8 pb-12">
      {/* Header Banner */}
      <div className="flex flex-col sm:flex-row sm:items-center justify-between gap-4 pb-6 border-b border-border">
        <div>
          <h1 className="text-2xl font-bold tracking-tight">
            {dashboard?.greeting || `Welcome, ${user?.displayName || 'Job Seeker'}`}
          </h1>
          <div className="flex items-center gap-2 mt-1 flex-wrap">
            <span className="text-xs text-muted-foreground">Search status:</span>
            <span className="inline-flex items-center px-2 py-0.5 rounded-full text-xs font-semibold bg-emerald-500/10 text-emerald-500 border border-emerald-500/20">
              {dashboard?.searchStatus || 'Active'}
            </span>
            {(dashboard?.targetRole || user?.targetRole) && (
              <span className="text-xs text-muted-foreground">
                · Target: <strong className="text-foreground">{dashboard?.targetRole || user?.targetRole}</strong>
              </span>
            )}
          </div>
        </div>

        <div className="flex items-center gap-3">
          <QuickAddMenu />
        </div>
      </div>

      {/* Metric Cards */}
      <div className="grid grid-cols-1 sm:grid-cols-2 lg:grid-cols-4 gap-4">
        <Link
          to="/applications"
          className="p-5 rounded-xl border border-border bg-card shadow-2xs hover:border-border/80 transition-all group"
        >
          <div className="flex items-center justify-between text-muted-foreground mb-3">
            <span className="text-xs font-semibold uppercase tracking-wider group-hover:text-primary transition-colors">
              Active Applications
            </span>
            <Briefcase className="h-4 w-4 text-indigo-500" />
          </div>
          <div className="text-2xl font-bold">{dashboard?.activeApplicationsCount ?? 0}</div>
          <div className="text-xs text-muted-foreground mt-1">
            {dashboard?.activeApplicationsCount ? 'Applications in flight' : 'Ready to add your first application'}
          </div>
        </Link>

        <Link
          to="/tasks"
          className="p-5 rounded-xl border border-border bg-card shadow-2xs hover:border-border/80 transition-all group"
        >
          <div className="flex items-center justify-between text-muted-foreground mb-3">
            <span className="text-xs font-semibold uppercase tracking-wider group-hover:text-primary transition-colors">
              Do Today
            </span>
            <CheckCircle2 className="h-4 w-4 text-emerald-500" />
          </div>
          <div className="text-2xl font-bold">{dashboard?.doToday?.length ?? 0}</div>
          <div className="text-xs text-muted-foreground mt-1">
            {dashboard?.doToday?.length
              ? 'Urgent follow-ups & next actions'
              : 'All daily follow-ups complete'}
          </div>
        </Link>

        <Link
          to="/interviews"
          className="p-5 rounded-xl border border-border bg-card shadow-2xs hover:border-border/80 transition-all group"
        >
          <div className="flex items-center justify-between text-muted-foreground mb-3">
            <span className="text-xs font-semibold uppercase tracking-wider group-hover:text-primary transition-colors">
              Upcoming Interviews
            </span>
            <Calendar className="h-4 w-4 text-amber-500" />
          </div>
          <div className="text-2xl font-bold">{dashboard?.upcomingInterviews?.length ?? 0}</div>
          <div className="text-xs text-muted-foreground mt-1">Next 7 days schedule</div>
        </Link>

        <div className="p-5 rounded-xl border border-border bg-card shadow-2xs">
          <div className="flex items-center justify-between text-muted-foreground mb-3">
            <span className="text-xs font-semibold uppercase tracking-wider">Active Offers</span>
            <Sparkles className="h-4 w-4 text-violet-500" />
          </div>
          <div className="text-2xl font-bold">{dashboard?.activeOffersCount ?? 0}</div>
          <div className="text-xs text-muted-foreground mt-1">
            {dashboard?.activeOffersCount ? 'Offers available to compare' : 'Compare offers when received'}
          </div>
        </div>
      </div>

      {/* This Week Activity Strip */}
      <div className="bg-card border border-border rounded-xl p-4 shadow-2xs">
        <div className="flex flex-col sm:flex-row sm:items-center justify-between gap-3 pb-3 border-b border-border/60">
          <div className="flex items-center gap-2">
            <TrendingUp className="h-4 w-4 text-primary" />
            <h3 className="text-xs font-bold uppercase tracking-wider text-muted-foreground">
              This Week's Activity
            </h3>
          </div>
          <span className="text-[11px] text-muted-foreground">Updated in real-time</span>
        </div>

        <div className="grid grid-cols-2 sm:grid-cols-4 gap-4 pt-3 text-center">
          <div>
            <span className="text-xl font-bold text-foreground">
              {dashboard?.thisWeekStats?.applicationsSent ?? 0}
            </span>
            <span className="block text-[11px] text-muted-foreground font-medium mt-0.5">
              Applications Sent
            </span>
          </div>
          <div>
            <span className="text-xl font-bold text-foreground">
              {dashboard?.thisWeekStats?.responses ?? 0}
            </span>
            <span className="block text-[11px] text-muted-foreground font-medium mt-0.5">
              Replies Received
            </span>
          </div>
          <div>
            <span className="text-xl font-bold text-foreground">
              {dashboard?.thisWeekStats?.interviews ?? 0}
            </span>
            <span className="block text-[11px] text-muted-foreground font-medium mt-0.5">
              Interviews Completed
            </span>
          </div>
          <div>
            <span className="text-xl font-bold text-foreground">
              {dashboard?.thisWeekStats?.offers ?? 0}
            </span>
            <span className="block text-[11px] text-muted-foreground font-medium mt-0.5">
              Offers Received
            </span>
          </div>
        </div>
      </div>

      {/* Main Sections: Left (Do Today + Stale) & Right (Interviews + Shortcuts) */}
      <div className="grid grid-cols-1 lg:grid-cols-3 gap-6">
        {/* Left Column (2 Cols) */}
        <div className="lg:col-span-2 space-y-6">
          {/* Do Today Section */}
          <div className="border border-border rounded-xl bg-card p-6 shadow-2xs space-y-4">
            <div className="flex items-center justify-between pb-3 border-b border-border">
              <div className="flex items-center gap-2">
                <CheckCircle2 className="h-5 w-5 text-emerald-500" />
                <div>
                  <h2 className="text-base font-bold tracking-tight">Do Today</h2>
                  <p className="text-xs text-muted-foreground">
                    Next-action items generated from rules and your scheduled deadlines
                  </p>
                </div>
              </div>

              <Link
                to="/tasks"
                className="inline-flex items-center gap-1 text-xs font-semibold text-primary hover:underline"
              >
                <span>View all tasks</span>
                <ArrowUpRight className="h-3.5 w-3.5" />
              </Link>
            </div>

            <DoTodayList tasks={dashboard?.doToday ?? []} />
          </div>

          {/* Stale Applications Section (if any) */}
          {dashboard?.staleApplications && dashboard.staleApplications.length > 0 && (
            <StaleApplicationsWidget applications={dashboard.staleApplications} />
          )}
        </div>

        {/* Right Column (1 Col) */}
        <div className="space-y-6">
          {/* Upcoming Interviews */}
          <div className="border border-border rounded-xl bg-card p-5 shadow-2xs space-y-4">
            <div className="flex items-center justify-between pb-3 border-b border-border">
              <div className="flex items-center gap-2">
                <Calendar className="h-4 w-4 text-amber-500" />
                <h3 className="font-bold text-sm">Upcoming Interviews</h3>
              </div>
              <Link
                to="/interviews"
                className="text-xs font-semibold text-primary hover:underline"
              >
                Schedule
              </Link>
            </div>

            <UpcomingInterviewsWidget interviews={dashboard?.upcomingInterviews ?? []} />
          </div>

          {/* Quick Shortcuts */}
          <div className="border border-border rounded-xl bg-card p-5 shadow-2xs space-y-3">
            <h3 className="font-bold text-xs uppercase tracking-wider text-muted-foreground pb-2 border-b border-border">
              Command Shortcuts
            </h3>

            <div className="space-y-1.5">
              <Link
                to="/applications"
                className="flex items-center justify-between p-2.5 rounded-lg border border-border hover:bg-muted text-xs font-medium transition-colors"
              >
                <div className="flex items-center gap-2">
                  <Briefcase className="h-3.5 w-3.5 text-indigo-500" />
                  <span>Kanban & Table View</span>
                </div>
                <ArrowUpRight className="h-3.5 w-3.5 text-muted-foreground" />
              </Link>

              <Link
                to="/tasks"
                className="flex items-center justify-between p-2.5 rounded-lg border border-border hover:bg-muted text-xs font-medium transition-colors"
              >
                <div className="flex items-center gap-2">
                  <ListTodo className="h-3.5 w-3.5 text-emerald-500" />
                  <span>Tasks & Reminders</span>
                </div>
                <ArrowUpRight className="h-3.5 w-3.5 text-muted-foreground" />
              </Link>

              <Link
                to="/templates"
                className="flex items-center justify-between p-2.5 rounded-lg border border-border hover:bg-muted text-xs font-medium transition-colors"
              >
                <div className="flex items-center gap-2">
                  <Mail className="h-3.5 w-3.5 text-blue-500" />
                  <span>Email Templates</span>
                </div>
                <ArrowUpRight className="h-3.5 w-3.5 text-muted-foreground" />
              </Link>

              <Link
                to="/documents"
                className="flex items-center justify-between p-2.5 rounded-lg border border-border hover:bg-muted text-xs font-medium transition-colors"
              >
                <div className="flex items-center gap-2">
                  <FileText className="h-3.5 w-3.5 text-amber-500" />
                  <span>CV Versions & Stats</span>
                </div>
                <ArrowUpRight className="h-3.5 w-3.5 text-muted-foreground" />
              </Link>
            </div>
          </div>
        </div>
      </div>
    </div>
  );
};
