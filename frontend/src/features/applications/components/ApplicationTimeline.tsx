import React from 'react';
import { useApplicationTimeline } from '../useApplications';
import {
  Calendar,
  GitCommit,
  MessageSquare,
  CheckCircle2,
  Clock,
  Sparkles,
} from 'lucide-react';
import { clsx } from 'clsx';

interface ApplicationTimelineProps {
  applicationId: string;
}

export const ApplicationTimeline: React.FC<ApplicationTimelineProps> = ({ applicationId }) => {
  const { data: timeline, isLoading, error } = useApplicationTimeline(applicationId);

  if (isLoading) {
    return (
      <div className="space-y-4 p-4">
        {[1, 2, 3].map((i) => (
          <div key={i} className="flex gap-4 animate-pulse">
            <div className="w-8 h-8 rounded-full bg-muted shrink-0" />
            <div className="space-y-2 flex-1">
              <div className="h-4 bg-muted rounded w-1/3" />
              <div className="h-3 bg-muted rounded w-2/3" />
            </div>
          </div>
        ))}
      </div>
    );
  }

  if (error) {
    return (
      <div className="p-6 text-center text-xs text-destructive border border-destructive/20 rounded-xl bg-destructive/5">
        Failed to load application timeline.
      </div>
    );
  }

  if (!timeline || timeline.length === 0) {
    return (
      <div className="flex flex-col items-center justify-center p-12 text-center border border-dashed border-border rounded-xl bg-card">
        <Clock className="h-10 w-10 text-muted-foreground/50 mb-3" />
        <h3 className="font-semibold text-sm text-foreground">No Timeline Activity Yet</h3>
        <p className="text-xs text-muted-foreground mt-1 max-w-sm">
          Stage changes, interview schedules, contacts, and notes will automatically populate this chronological audit trail.
        </p>
      </div>
    );
  }

  const getEventIcon = (type: string) => {
    switch (type) {
      case 'StatusChange':
        return <GitCommit className="h-4 w-4 text-primary" />;
      case 'Interview':
        return <Calendar className="h-4 w-4 text-amber-500" />;
      case 'Interaction':
        return <MessageSquare className="h-4 w-4 text-blue-500" />;
      case 'Task':
        return <CheckCircle2 className="h-4 w-4 text-emerald-500" />;
      default:
        return <Sparkles className="h-4 w-4 text-muted-foreground" />;
    }
  };

  const getBadgeStyle = (type: string) => {
    switch (type) {
      case 'StatusChange':
        return 'bg-primary/10 text-primary border-primary/20';
      case 'Interview':
        return 'bg-amber-500/10 text-amber-600 dark:text-amber-400 border-amber-500/20';
      case 'Interaction':
        return 'bg-blue-500/10 text-blue-600 dark:text-blue-400 border-blue-500/20';
      case 'Task':
        return 'bg-emerald-500/10 text-emerald-600 dark:text-emerald-400 border-emerald-500/20';
      default:
        return 'bg-muted text-muted-foreground border-border';
    }
  };

  const formatDate = (dateStr: string) => {
    try {
      const date = new Date(dateStr);
      return date.toLocaleDateString('en-US', {
        month: 'short',
        day: 'numeric',
        year: 'numeric',
        hour: '2-digit',
        minute: '2-digit',
      });
    } catch {
      return dateStr;
    }
  };

  return (
    <div className="relative pl-6 space-y-6 before:absolute before:bottom-0 before:top-2 before:left-[15px] before:w-[2px] before:bg-border">
      {timeline.map((item) => (
        <div key={item.id} className="relative group">
          {/* Timeline node icon */}
          <div className="absolute -left-[30px] top-0.5 flex h-7 w-7 items-center justify-center rounded-full bg-card border border-border shadow-xs group-hover:scale-110 transition-transform">
            {getEventIcon(item.type)}
          </div>

          {/* Event Content Card */}
          <div className="bg-card border border-border rounded-xl p-4 shadow-xs hover:border-primary/30 transition-colors">
            <div className="flex items-start justify-between gap-2 flex-wrap mb-1.5">
              <div className="flex items-center gap-2">
                <span
                  className={clsx(
                    'px-2 py-0.5 text-[10px] font-semibold uppercase tracking-wider rounded-md border',
                    getBadgeStyle(item.type)
                  )}
                >
                  {item.type}
                </span>
                <h4 className="text-xs font-semibold text-foreground">{item.title}</h4>
              </div>
              <time className="text-[11px] text-muted-foreground font-mono">
                {formatDate(item.timestamp)}
              </time>
            </div>

            {item.description && (
              <p className="text-xs text-muted-foreground mt-1 whitespace-pre-line leading-relaxed">
                {item.description}
              </p>
            )}

            {item.metadata && Object.keys(item.metadata).length > 0 && (
              <div className="mt-3 pt-2 border-t border-border/60 flex flex-wrap gap-2 text-[11px] text-muted-foreground">
                {Object.entries(item.metadata).map(([key, val]) => (
                  <span
                    key={key}
                    className="inline-flex items-center gap-1 bg-muted/60 px-2 py-0.5 rounded text-xs"
                  >
                    <span className="font-semibold text-foreground/70">{key}:</span>
                    <span>{val || '—'}</span>
                  </span>
                ))}
              </div>
            )}
          </div>
        </div>
      ))}
    </div>
  );
};
