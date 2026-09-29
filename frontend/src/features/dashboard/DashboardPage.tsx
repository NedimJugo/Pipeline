import React from 'react';
import { useAuth } from '@/features/auth/AuthContext';
import {
  Briefcase,
  CheckCircle2,
  Calendar,
  Sparkles,
  ArrowUpRight,
  Plus,
} from 'lucide-react';
import { Link } from 'react-router-dom';

export const DashboardPage: React.FC = () => {
  const { user } = useAuth();

  const greeting = () => {
    const hour = new Date().getHours();
    if (hour < 12) return 'Good morning';
    if (hour < 18) return 'Good afternoon';
    return 'Good evening';
  };

  return (
    <div className="max-w-6xl mx-auto space-y-8">
      {/* Header Banner */}
      <div className="flex flex-col sm:flex-row sm:items-center justify-between gap-4 pb-6 border-b border-border">
        <div>
          <h1 className="text-2xl font-bold tracking-tight">
            {greeting()}, {user?.displayName || 'Job Seeker'}
          </h1>
          <p className="text-sm text-muted-foreground mt-1">
            Search status: <span className="inline-flex items-center px-2 py-0.5 rounded-full text-xs font-semibold bg-emerald-500/10 text-emerald-500 border border-emerald-500/20">Active</span>
            {user?.targetRole && <span className="ml-2 text-muted-foreground">· Target: {user.targetRole}</span>}
          </p>
        </div>

        <div className="flex items-center gap-3">
          <Link
            to="/applications"
            className="inline-flex items-center gap-2 px-4 py-2 bg-primary text-primary-foreground text-sm font-semibold rounded-lg hover:bg-primary/90 shadow-sm transition-colors"
          >
            <Plus className="h-4 w-4" />
            <span>New Application</span>
          </Link>
        </div>
      </div>

      {/* Metric Cards */}
      <div className="grid grid-cols-1 sm:grid-cols-2 lg:grid-cols-4 gap-4">
        <div className="p-5 rounded-xl border border-border bg-card/60 shadow-sm">
          <div className="flex items-center justify-between text-muted-foreground mb-3">
            <span className="text-xs font-semibold uppercase tracking-wider">Active Applications</span>
            <Briefcase className="h-4 w-4 text-indigo-500" />
          </div>
          <div className="text-2xl font-bold">0</div>
          <div className="text-xs text-muted-foreground mt-1">Ready to add your first application</div>
        </div>

        <div className="p-5 rounded-xl border border-border bg-card/60 shadow-sm">
          <div className="flex items-center justify-between text-muted-foreground mb-3">
            <span className="text-xs font-semibold uppercase tracking-wider">Do Today</span>
            <CheckCircle2 className="h-4 w-4 text-emerald-500" />
          </div>
          <div className="text-2xl font-bold">0</div>
          <div className="text-xs text-muted-foreground mt-1">All follow-ups and tasks complete</div>
        </div>

        <div className="p-5 rounded-xl border border-border bg-card/60 shadow-sm">
          <div className="flex items-center justify-between text-muted-foreground mb-3">
            <span className="text-xs font-semibold uppercase tracking-wider">Upcoming Interviews</span>
            <Calendar className="h-4 w-4 text-amber-500" />
          </div>
          <div className="text-2xl font-bold">0</div>
          <div className="text-xs text-muted-foreground mt-1">Next 7 days schedule clear</div>
        </div>

        <div className="p-5 rounded-xl border border-border bg-card/60 shadow-sm">
          <div className="flex items-center justify-between text-muted-foreground mb-3">
            <span className="text-xs font-semibold uppercase tracking-wider">Active Offers</span>
            <Sparkles className="h-4 w-4 text-violet-500" />
          </div>
          <div className="text-2xl font-bold">0</div>
          <div className="text-xs text-muted-foreground mt-1">Compare offers when received</div>
        </div>
      </div>

      {/* Main Sections: Do Today & Next Action Engine */}
      <div className="grid grid-cols-1 lg:grid-cols-3 gap-6">
        <div className="lg:col-span-2 border border-border rounded-xl bg-card/60 p-6 shadow-sm">
          <div className="flex items-center justify-between mb-4">
            <h2 className="text-base font-bold tracking-tight">Today's Next Actions</h2>
            <span className="text-xs text-muted-foreground font-medium">Auto-generated rules</span>
          </div>

          <div className="border border-dashed border-border rounded-lg p-8 text-center">
            <CheckCircle2 className="h-8 w-8 text-muted-foreground mx-auto mb-2 opacity-50" />
            <h3 className="font-semibold text-sm">No tasks due today</h3>
            <p className="text-xs text-muted-foreground mt-1 max-w-sm mx-auto">
              As you add applications, contacts, and interview dates, the Next-Action Engine will automatically remind you when to follow up.
            </p>
          </div>
        </div>

        <div className="border border-border rounded-xl bg-card/60 p-6 shadow-sm">
          <div className="flex items-center justify-between mb-4">
            <h2 className="text-base font-bold tracking-tight">Quick Shortcuts</h2>
          </div>

          <div className="space-y-2">
            <Link
              to="/applications"
              className="flex items-center justify-between p-3 rounded-lg border border-border hover:bg-muted text-sm font-medium transition-colors"
            >
              <span>View Kanban Board</span>
              <ArrowUpRight className="h-4 w-4 text-muted-foreground" />
            </Link>
            <Link
              to="/contacts"
              className="flex items-center justify-between p-3 rounded-lg border border-border hover:bg-muted text-sm font-medium transition-colors"
            >
              <span>Recruiter & Contact Directory</span>
              <ArrowUpRight className="h-4 w-4 text-muted-foreground" />
            </Link>
            <Link
              to="/documents"
              className="flex items-center justify-between p-3 rounded-lg border border-border hover:bg-muted text-sm font-medium transition-colors"
            >
              <span>Manage CV Versions</span>
              <ArrowUpRight className="h-4 w-4 text-muted-foreground" />
            </Link>
            <Link
              to="/discovery"
              className="flex items-center justify-between p-3 rounded-lg border border-border hover:bg-muted text-sm font-medium transition-colors"
            >
              <span>Job Discovery (Coming Soon)</span>
              <ArrowUpRight className="h-4 w-4 text-muted-foreground" />
            </Link>
          </div>
        </div>
      </div>
    </div>
  );
};
