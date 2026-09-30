import React from 'react';
import { useNavigate } from 'react-router-dom';
import { CalendarEvent, CalendarEventType } from './types';

interface MonthViewProps {
  currentDate: Date;
  events: CalendarEvent[];
}

export const MonthView: React.FC<MonthViewProps> = ({ currentDate, events }) => {
  const navigate = useNavigate();

  const year = currentDate.getFullYear();
  const month = currentDate.getMonth();

  // First day of month and days count
  const firstDayOfMonth = new Date(year, month, 1);
  const startingDayIndex = firstDayOfMonth.getDay(); // 0 is Sunday
  const daysInMonth = new Date(year, month + 1, 0).getDate();

  const daysInPrevMonth = new Date(year, month, 0).getDate();

  // Build grid of 35 or 42 cells
  const calendarCells: { date: Date; isCurrentMonth: boolean }[] = [];

  // Previous month padding
  for (let i = startingDayIndex - 1; i >= 0; i--) {
    calendarCells.push({
      date: new Date(year, month - 1, daysInPrevMonth - i),
      isCurrentMonth: false,
    });
  }

  // Current month days
  for (let day = 1; day <= daysInMonth; day++) {
    calendarCells.push({
      date: new Date(year, month, day),
      isCurrentMonth: true,
    });
  }

  // Next month padding to complete row
  const remainingCells = 7 - (calendarCells.length % 7);
  if (remainingCells < 7) {
    for (let day = 1; day <= remainingCells; day++) {
      calendarCells.push({
        date: new Date(year, month + 1, day),
        isCurrentMonth: false,
      });
    }
  }

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

  const getEventBadge = (ev: CalendarEvent) => {
    let colorClass = 'bg-muted text-muted-foreground border-border';
    switch (ev.type) {
      case 'Interview':
        colorClass = 'bg-indigo-500/15 text-indigo-400 border-indigo-500/30 hover:bg-indigo-500/25';
        break;
      case 'Task':
        colorClass = 'bg-blue-500/15 text-blue-400 border-blue-500/30 hover:bg-blue-500/25';
        break;
      case 'OfferDeadline':
        colorClass = 'bg-amber-500/15 text-amber-400 border-amber-500/30 hover:bg-amber-500/25';
        break;
      case 'FollowUp':
        colorClass = 'bg-emerald-500/15 text-emerald-400 border-emerald-500/30 hover:bg-emerald-500/25';
        break;
    }

    const timeStr = ev.isAllDay
      ? ''
      : new Date(ev.startAt).toLocaleTimeString([], { hour: '2-digit', minute: '2-digit' }) + ' ';

    return (
      <button
        key={ev.id}
        onClick={(e) => {
          e.stopPropagation();
          if (ev.url) navigate(ev.url);
        }}
        className={`w-full text-left truncate px-1.5 py-0.5 rounded text-[11px] font-semibold border transition-all ${colorClass}`}
        title={`${ev.title}${ev.location ? ` (${ev.location})` : ''}`}
      >
        <span className="opacity-75">{timeStr}</span>
        <span>{ev.title}</span>
      </button>
    );
  };

  const weekDays = ['Sun', 'Mon', 'Tue', 'Wed', 'Thu', 'Fri', 'Sat'];

  return (
    <div className="bg-card border border-border rounded-xl shadow-xs overflow-hidden">
      {/* Week Header */}
      <div className="grid grid-cols-7 border-b border-border bg-muted/30 text-center py-2 text-xs font-bold text-muted-foreground">
        {weekDays.map((d) => (
          <div key={d}>{d}</div>
        ))}
      </div>

      {/* Month Days Grid */}
      <div className="grid grid-cols-7 divide-x divide-y divide-border border-b border-border">
        {calendarCells.map((cell, idx) => {
          const dayEvents = getEventsForDay(cell.date);
          const activeToday = isToday(cell.date);

          return (
            <div
              key={idx}
              className={`min-h-[110px] p-1.5 sm:p-2 flex flex-col justify-between transition-colors ${
                cell.isCurrentMonth ? 'bg-card' : 'bg-muted/10 opacity-50'
              } hover:bg-muted/20`}
            >
              <div className="flex items-center justify-between">
                <span
                  className={`text-xs font-bold h-6 w-6 rounded-full flex items-center justify-center ${
                    activeToday
                      ? 'bg-primary text-primary-foreground shadow-2xs'
                      : 'text-foreground/80'
                  }`}
                >
                  {cell.date.getDate()}
                </span>

                {dayEvents.length > 0 && (
                  <span className="text-[10px] text-muted-foreground font-mono">
                    {dayEvents.length}
                  </span>
                )}
              </div>

              {/* Day Events Stack */}
              <div className="mt-1 space-y-1 flex-1 overflow-hidden">
                {dayEvents.slice(0, 3).map(getEventBadge)}

                {dayEvents.length > 3 && (
                  <span className="block text-[10px] text-muted-foreground pl-1 font-medium">
                    +{dayEvents.length - 3} more
                  </span>
                )}
              </div>
            </div>
          );
        })}
      </div>
    </div>
  );
};
