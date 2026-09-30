import React from 'react';
import { useNavigate } from 'react-router-dom';
import { CalendarEvent } from './types';

interface WeekViewProps {
  currentDate: Date;
  events: CalendarEvent[];
}

export const WeekView: React.FC<WeekViewProps> = ({ currentDate, events }) => {
  const navigate = useNavigate();

  // Find start of week (Sunday)
  const startOfWeek = new Date(currentDate);
  const day = startOfWeek.getDay();
  startOfWeek.setDate(startOfWeek.getDate() - day);
  startOfWeek.setHours(0, 0, 0, 0);

  const weekDays = Array.from({ length: 7 }, (_, i) => {
    const d = new Date(startOfWeek);
    d.setDate(d.getDate() + i);
    return d;
  });

  const today = new Date();
  const isToday = (d: Date) =>
    d.getDate() === today.getDate() &&
    d.getMonth() === today.getMonth() &&
    d.getFullYear() === today.getFullYear();

  const getEventsForDay = (d: Date) => {
    return events.filter((ev) => {
      const evDate = new Date(ev.startAt);
      return (
        evDate.getDate() === d.getDate() &&
        evDate.getMonth() === d.getMonth() &&
        evDate.getFullYear() === d.getFullYear()
      );
    });
  };

  return (
    <div className="bg-card border border-border rounded-xl shadow-xs overflow-hidden">
      <div className="grid grid-cols-7 divide-x divide-border">
        {weekDays.map((d) => {
          const dayEvents = getEventsForDay(d);
          const activeToday = isToday(d);

          return (
            <div key={d.toISOString()} className="min-h-[420px] flex flex-col">
              {/* Day Header */}
              <div
                className={`py-3 px-2 text-center border-b border-border ${
                  activeToday ? 'bg-primary/10' : 'bg-muted/20'
                }`}
              >
                <span className="block text-[11px] font-semibold text-muted-foreground uppercase tracking-wider">
                  {d.toLocaleDateString([], { weekday: 'short' })}
                </span>
                <span
                  className={`inline-flex items-center justify-center text-sm font-bold h-7 w-7 rounded-full mt-1 ${
                    activeToday
                      ? 'bg-primary text-primary-foreground shadow-2xs'
                      : 'text-foreground'
                  }`}
                >
                  {d.getDate()}
                </span>
              </div>

              {/* Day Events Container */}
              <div className="p-2 space-y-2 flex-1">
                {dayEvents.length === 0 ? (
                  <span className="block text-[11px] text-muted-foreground/40 text-center pt-6 italic">
                    No events
                  </span>
                ) : (
                  dayEvents.map((ev) => {
                    let borderClass = 'border-l-indigo-500 bg-indigo-500/5';
                    if (ev.type === 'Task') borderClass = 'border-l-blue-500 bg-blue-500/5';
                    if (ev.type === 'OfferDeadline') borderClass = 'border-l-amber-500 bg-amber-500/5';
                    if (ev.type === 'FollowUp') borderClass = 'border-l-emerald-500 bg-emerald-500/5';

                    return (
                      <div
                        key={ev.id}
                        onClick={() => ev.url && navigate(ev.url)}
                        className={`p-2 rounded-lg border border-border border-l-4 ${borderClass} hover:border-border hover:shadow-xs transition-all cursor-pointer text-xs space-y-1`}
                      >
                        <div className="flex items-center justify-between text-[11px] text-muted-foreground">
                          <span className="font-semibold text-foreground/80">{ev.type}</span>
                          {!ev.isAllDay && (
                            <span className="font-mono">
                              {new Date(ev.startAt).toLocaleTimeString([], { hour: '2-digit', minute: '2-digit' })}
                            </span>
                          )}
                        </div>

                        <p className="font-bold text-foreground text-xs line-clamp-2">
                          {ev.title}
                        </p>

                        {ev.location && (
                          <span className="text-[11px] text-muted-foreground/80 block truncate">
                            📍 {ev.location}
                          </span>
                        )}
                      </div>
                    );
                  })
                )}
              </div>
            </div>
          );
        })}
      </div>
    </div>
  );
};
