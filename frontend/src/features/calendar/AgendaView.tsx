import React from 'react';
import { useNavigate } from 'react-router-dom';
import {
  Calendar,
  Clock,
  MapPin,
  CheckCircle2,
  AlertCircle,
  ExternalLink,
  ChevronRight,
  Briefcase,
} from 'lucide-react';
import { CalendarEvent } from './types';

interface AgendaViewProps {
  events: CalendarEvent[];
}

export const AgendaView: React.FC<AgendaViewProps> = ({ events }) => {
  const navigate = useNavigate();

  // Group events by date string
  const sorted = [...events].sort(
    (a, b) => new Date(a.startAt).getTime() - new Date(b.startAt).getTime()
  );

  const groups = sorted.reduce<Record<string, CalendarEvent[]>>((acc, ev) => {
    const dateKey = new Date(ev.startAt).toLocaleDateString([], {
      weekday: 'long',
      month: 'short',
      day: 'numeric',
      year: 'numeric',
    });
    if (!acc[dateKey]) acc[dateKey] = [];
    acc[dateKey].push(ev);
    return acc;
  }, {});

  const getTypeBadge = (type: CalendarEvent['type']) => {
    switch (type) {
      case 'Interview':
        return (
          <span className="px-2.5 py-0.5 rounded-full text-xs font-semibold bg-indigo-500/10 text-indigo-400 border border-indigo-500/20">
            Interview
          </span>
        );
      case 'Task':
        return (
          <span className="px-2.5 py-0.5 rounded-full text-xs font-semibold bg-blue-500/10 text-blue-400 border border-blue-500/20">
            Task Due
          </span>
        );
      case 'OfferDeadline':
        return (
          <span className="px-2.5 py-0.5 rounded-full text-xs font-semibold bg-amber-500/10 text-amber-500 border border-amber-500/20">
            Offer Deadline
          </span>
        );
      case 'FollowUp':
        return (
          <span className="px-2.5 py-0.5 rounded-full text-xs font-semibold bg-emerald-500/10 text-emerald-500 border border-emerald-500/20">
            Follow-up
          </span>
        );
    }
  };

  return (
    <div className="space-y-6">
      {Object.keys(groups).length === 0 ? (
        <div className="bg-card border border-dashed border-border rounded-xl py-16 text-center space-y-2">
          <Calendar className="h-8 w-8 text-muted-foreground mx-auto" />
          <h4 className="font-bold text-sm text-foreground">No upcoming agenda events</h4>
          <p className="text-xs text-muted-foreground max-w-sm mx-auto">
            Schedule an interview, set a task due date, or log an offer deadline to populate your agenda.
          </p>
        </div>
      ) : (
        Object.entries(groups).map(([dateLabel, dayEvents]) => (
          <div key={dateLabel} className="space-y-3">
            <div className="flex items-center gap-2">
              <span className="text-xs font-bold uppercase tracking-wider text-muted-foreground">
                {dateLabel}
              </span>
              <div className="h-px bg-border flex-1" />
              <span className="text-[11px] text-muted-foreground font-mono">
                {dayEvents.length} event{dayEvents.length === 1 ? '' : 's'}
              </span>
            </div>

            <div className="space-y-2">
              {dayEvents.map((ev) => (
                <div
                  key={ev.id}
                  onClick={() => ev.url && navigate(ev.url)}
                  className="bg-card border border-border rounded-xl p-4 shadow-2xs hover:border-primary/40 transition-all flex flex-col sm:flex-row sm:items-center justify-between gap-3 cursor-pointer group"
                >
                  <div className="space-y-1.5 flex-1">
                    <div className="flex flex-wrap items-center gap-2">
                      {getTypeBadge(ev.type)}

                      <span className="text-xs font-mono text-muted-foreground">
                        {ev.isAllDay
                          ? 'All Day'
                          : new Date(ev.startAt).toLocaleTimeString([], {
                              hour: '2-digit',
                              minute: '2-digit',
                            })}
                      </span>

                      {ev.companyName && (
                        <span className="text-xs text-muted-foreground flex items-center gap-1 font-medium">
                          <span>•</span>
                          <Briefcase className="h-3 w-3" />
                          <span>{ev.companyName}</span>
                        </span>
                      )}
                    </div>

                    <h4 className="font-bold text-sm text-foreground group-hover:text-primary transition-colors">
                      {ev.title}
                    </h4>

                    {ev.description && (
                      <p className="text-xs text-muted-foreground line-clamp-1 italic">
                        "{ev.description}"
                      </p>
                    )}

                    {ev.location && (
                      <p className="text-xs text-muted-foreground flex items-center gap-1">
                        <MapPin className="h-3 w-3 shrink-0" />
                        <span className="truncate">{ev.location}</span>
                      </p>
                    )}
                  </div>

                  <div className="flex items-center gap-2 self-end sm:self-center shrink-0">
                    <button
                      type="button"
                      className="inline-flex items-center gap-1 px-3 py-1.5 text-xs font-semibold rounded-lg bg-muted text-foreground hover:bg-muted/80 transition-colors"
                    >
                      <span>Open Details</span>
                      <ChevronRight className="h-3.5 w-3.5" />
                    </button>
                  </div>
                </div>
              ))}
            </div>
          </div>
        ))
      )}
    </div>
  );
};
