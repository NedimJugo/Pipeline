import React, { useState, useMemo } from 'react';
import { useInterviews } from './useInterviews';
import { InterviewCard } from './components/InterviewCard';
import { ScheduleInterviewModal } from './components/ScheduleInterviewModal';
import {
  Calendar,
  CheckCircle2,
  Clock,
  Plus,
  Video,
  AlertCircle,
} from 'lucide-react';
import { clsx } from 'clsx';

type FilterTab = 'upcoming' | 'past' | 'all';

export const InterviewsPage: React.FC = () => {
  const [activeTab, setActiveTab] = useState<FilterTab>('upcoming');
  const [isScheduleModalOpen, setIsScheduleModalOpen] = useState(false);

  const { data: interviews, isLoading, error } = useInterviews();

  // Metrics
  const metrics = useMemo(() => {
    if (!interviews) return { upcoming: 0, completed: 0, total: 0 };
    const now = new Date();
    const upcoming = interviews.filter(
      (i) => i.status === 'Scheduled' && new Date(i.scheduledAt) >= now
    ).length;
    const completed = interviews.filter((i) => i.status === 'Completed').length;
    return { upcoming, completed, total: interviews.length };
  }, [interviews]);

  // Filtered interviews list
  const filteredInterviews = useMemo(() => {
    if (!interviews) return [];
    const now = new Date();
    switch (activeTab) {
      case 'upcoming':
        return interviews.filter(
          (i) => i.status === 'Scheduled' && new Date(i.scheduledAt) >= now
        );
      case 'past':
        return interviews.filter(
          (i) => i.status === 'Completed' || new Date(i.scheduledAt) < now
        );
      case 'all':
      default:
        return interviews;
    }
  }, [interviews, activeTab]);

  return (
    <div className="space-y-6 max-w-7xl mx-auto pb-12">
      {/* Header */}
      <div className="flex flex-col sm:flex-row sm:items-center justify-between gap-4">
        <div>
          <h1 className="text-xl font-bold tracking-tight text-foreground">
            Interviews & Prep Center
          </h1>
          <p className="text-xs text-muted-foreground mt-0.5">
            Prepare tailored checklists, log questions, debrief, and export calendar invites.
          </p>
        </div>

        <button
          type="button"
          onClick={() => setIsScheduleModalOpen(true)}
          className="inline-flex items-center gap-1.5 px-3.5 py-2 text-xs font-semibold bg-primary text-primary-foreground rounded-lg hover:opacity-90 shadow-sm transition-opacity self-start sm:self-auto"
        >
          <Plus className="h-4 w-4" />
          <span>Schedule Interview</span>
        </button>
      </div>

      {/* Metrics Row */}
      <div className="grid grid-cols-1 sm:grid-cols-3 gap-4">
        <div className="bg-card border border-border rounded-xl p-4 shadow-2xs">
          <div className="flex items-center justify-between">
            <span className="text-xs font-semibold text-muted-foreground">Upcoming Rounds</span>
            <Clock className="h-4 w-4 text-primary" />
          </div>
          <p className="text-2xl font-bold text-foreground mt-2">{metrics.upcoming}</p>
        </div>

        <div className="bg-card border border-border rounded-xl p-4 shadow-2xs">
          <div className="flex items-center justify-between">
            <span className="text-xs font-semibold text-muted-foreground">Completed Rounds</span>
            <CheckCircle2 className="h-4 w-4 text-emerald-500" />
          </div>
          <p className="text-2xl font-bold text-emerald-600 dark:text-emerald-400 mt-2">
            {metrics.completed}
          </p>
        </div>

        <div className="bg-card border border-border rounded-xl p-4 shadow-2xs">
          <div className="flex items-center justify-between">
            <span className="text-xs font-semibold text-muted-foreground">Total In Pipeline</span>
            <Calendar className="h-4 w-4 text-muted-foreground" />
          </div>
          <p className="text-2xl font-bold text-foreground mt-2">{metrics.total}</p>
        </div>
      </div>

      {/* Tabs */}
      <div className="flex items-center justify-between border-b border-border">
        <div className="flex items-center gap-6">
          {[
            { key: 'upcoming', label: `Upcoming (${metrics.upcoming})` },
            { key: 'past', label: `Past & Debriefed (${metrics.completed})` },
            { key: 'all', label: `All (${metrics.total})` },
          ].map((tab) => (
            <button
              key={tab.key}
              type="button"
              onClick={() => setActiveTab(tab.key as FilterTab)}
              className={clsx(
                'pb-3 text-xs font-semibold transition-colors border-b-2 relative -mb-[1px]',
                activeTab === tab.key
                  ? 'border-primary text-primary'
                  : 'border-transparent text-muted-foreground hover:text-foreground'
              )}
            >
              {tab.label}
            </button>
          ))}
        </div>
      </div>

      {/* Content */}
      {isLoading ? (
        <div className="grid grid-cols-1 md:grid-cols-2 lg:grid-cols-3 gap-4">
          {[1, 2, 3].map((n) => (
            <div key={n} className="h-48 bg-card border border-border rounded-xl animate-pulse" />
          ))}
        </div>
      ) : error ? (
        <div className="p-8 text-center bg-card border border-border rounded-xl space-y-2">
          <AlertCircle className="h-8 w-8 text-destructive mx-auto" />
          <h3 className="text-sm font-bold text-foreground">Failed to load interviews</h3>
          <p className="text-xs text-muted-foreground">
            There was an error loading your interview schedule.
          </p>
        </div>
      ) : filteredInterviews.length === 0 ? (
        <div className="bg-card border border-dashed border-border rounded-xl p-12 text-center space-y-3">
          <div className="w-12 h-12 bg-muted/50 rounded-xl flex items-center justify-center mx-auto text-muted-foreground">
            <Calendar className="h-6 w-6" />
          </div>
          <div className="max-w-sm mx-auto space-y-1">
            <h3 className="text-sm font-bold text-foreground">
              {activeTab === 'upcoming'
                ? 'No upcoming interviews scheduled'
                : 'No interviews in this view'}
            </h3>
            <p className="text-xs text-muted-foreground">
              Schedule your next screening or technical loop to unlock curated prep checklists and calendar exports.
            </p>
          </div>
          <button
            type="button"
            onClick={() => setIsScheduleModalOpen(true)}
            className="inline-flex items-center gap-1.5 px-4 py-2 text-xs font-semibold bg-primary text-primary-foreground rounded-lg hover:opacity-90 shadow-sm transition-opacity"
          >
            <Plus className="h-4 w-4" />
            <span>Schedule New Interview</span>
          </button>
        </div>
      ) : (
        <div className="grid grid-cols-1 md:grid-cols-2 lg:grid-cols-3 gap-4">
          {filteredInterviews.map((interview) => (
            <InterviewCard key={interview.id} interview={interview} />
          ))}
        </div>
      )}

      {/* Modal */}
      {isScheduleModalOpen && (
        <ScheduleInterviewModal
          isOpen={isScheduleModalOpen}
          onClose={() => setIsScheduleModalOpen(false)}
        />
      )}
    </div>
  );
};
