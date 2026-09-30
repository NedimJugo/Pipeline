import React from 'react';
import { Calendar, Clock, Video, Phone, MapPin, ArrowRight, CheckCircle2 } from 'lucide-react';
import { Link } from 'react-router-dom';
import { UpcomingInterview } from './types';

interface UpcomingInterviewsWidgetProps {
  interviews: UpcomingInterview[];
}

export const UpcomingInterviewsWidget: React.FC<UpcomingInterviewsWidgetProps> = ({
  interviews,
}) => {
  if (interviews.length === 0) {
    return (
      <div className="border border-dashed border-border rounded-xl p-8 text-center bg-card/20">
        <Calendar className="h-8 w-8 text-muted-foreground mx-auto mb-2 opacity-50" />
        <h3 className="font-semibold text-sm">No interviews scheduled</h3>
        <p className="text-xs text-muted-foreground mt-1 max-w-sm mx-auto">
          Your schedule for the next 7 days is clear. Add an interview date from any active application.
        </p>
      </div>
    );
  }

  return (
    <div className="space-y-3">
      {interviews.map((interview) => (
        <div
          key={interview.id}
          className="p-4 rounded-xl border border-border bg-card shadow-2xs hover:border-border/80 transition-all space-y-3"
        >
          <div className="flex items-start justify-between gap-3">
            <div className="min-w-0 space-y-0.5">
              <span className="text-[11px] font-semibold text-primary uppercase tracking-wider block">
                {interview.companyName}
              </span>
              <h4 className="text-sm font-bold tracking-tight text-foreground truncate">
                {interview.roleTitle}
              </h4>
              <div className="flex items-center gap-2 pt-0.5 text-xs text-muted-foreground">
                <span className="inline-flex items-center gap-1">
                  {interview.format === 'Video' ? (
                    <Video className="h-3 w-3 text-indigo-500" />
                  ) : interview.format === 'Phone' ? (
                    <Phone className="h-3 w-3 text-emerald-500" />
                  ) : (
                    <MapPin className="h-3 w-3 text-amber-500" />
                  )}
                  <span>{interview.type}</span>
                </span>
              </div>
            </div>

            <span className="inline-flex items-center gap-1 px-2.5 py-1 rounded-full text-xs font-bold bg-amber-500/10 text-amber-600 dark:text-amber-400 border border-amber-500/20 shrink-0">
              <Clock className="h-3 w-3" />
              <span>{interview.countdownText}</span>
            </span>
          </div>

          {/* Prep checklist progress bar */}
          <div className="pt-2 border-t border-border/60 space-y-1.5">
            <div className="flex items-center justify-between text-[11px]">
              <span className="text-muted-foreground font-medium">Prep Checklist</span>
              <span className="font-semibold text-foreground">
                {interview.prepChecklistCompleted}/{interview.prepChecklistTotal} completed ({interview.prepProgressPercent}%)
              </span>
            </div>
            <div className="h-1.5 w-full bg-muted rounded-full overflow-hidden">
              <div
                className={`h-full transition-all duration-300 rounded-full ${
                  interview.prepProgressPercent === 100
                    ? 'bg-emerald-500'
                    : interview.prepProgressPercent > 50
                    ? 'bg-primary'
                    : 'bg-amber-500'
                }`}
                style={{ width: `${Math.max(5, interview.prepProgressPercent)}%` }}
              />
            </div>
          </div>

          <div className="flex items-center justify-end pt-1">
            <Link
              to={`/interviews/${interview.id}`}
              className="inline-flex items-center gap-1 text-xs font-semibold text-primary hover:underline"
            >
              <span>Review Prep & Strategy</span>
              <ArrowRight className="h-3 w-3" />
            </Link>
          </div>
        </div>
      ))}
    </div>
  );
};
