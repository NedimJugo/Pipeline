import React, { useState } from 'react';
import {
  Calendar as CalendarIcon,
  ChevronLeft,
  ChevronRight,
  Filter,
  Rss,
  CheckCircle2,
  Clock,
  Briefcase,
  Layers,
} from 'lucide-react';
import { useCalendarEvents } from './useCalendar';
import { CalendarEventType } from './types';
import { MonthView } from './MonthView';
import { WeekView } from './WeekView';
import { AgendaView } from './AgendaView';
import { CalendarFeedModal } from './CalendarFeedModal';

export const CalendarPage: React.FC = () => {
  const [currentDate, setCurrentDate] = useState<Date>(new Date());
  const [activeView, setActiveView] = useState<'month' | 'week' | 'agenda'>('month');
  const [isFeedModalOpen, setIsFeedModalOpen] = useState(false);

  // Filters for event types
  const [selectedTypes, setSelectedTypes] = useState<Record<CalendarEventType, boolean>>({
    Interview: true,
    Task: true,
    OfferDeadline: true,
    FollowUp: true,
  });

  const { data: rawEvents = [], isLoading } = useCalendarEvents();
  const allEvents = Array.isArray(rawEvents) ? rawEvents : [];

  // Filter events by selected category
  const filteredEvents = allEvents.filter((ev) => selectedTypes[ev.type]);

  const toggleType = (type: CalendarEventType) => {
    setSelectedTypes((prev) => ({ ...prev, [type]: !prev[type] }));
  };

  const handlePrev = () => {
    const next = new Date(currentDate);
    if (activeView === 'month') {
      next.setMonth(next.getMonth() - 1);
    } else if (activeView === 'week') {
      next.setDate(next.getDate() - 7);
    } else {
      next.setMonth(next.getMonth() - 1);
    }
    setCurrentDate(next);
  };

  const handleNext = () => {
    const next = new Date(currentDate);
    if (activeView === 'month') {
      next.setMonth(next.getMonth() + 1);
    } else if (activeView === 'week') {
      next.setDate(next.getDate() + 7);
    } else {
      next.setMonth(next.getMonth() + 1);
    }
    setCurrentDate(next);
  };

  const handleToday = () => {
    setCurrentDate(new Date());
  };

  const monthYearLabel = currentDate.toLocaleDateString([], {
    month: 'long',
    year: 'numeric',
  });

  return (
    <div className="max-w-7xl mx-auto space-y-6 pb-12">
      {/* Top Header Bar */}
      <div className="flex flex-col sm:flex-row sm:items-center justify-between gap-4 pb-4 border-b border-border">
        <div>
          <h1 className="text-2xl font-bold tracking-tight">Recruitment & Pipeline Calendar</h1>
          <p className="text-sm text-muted-foreground mt-1">
            Track interview times, task deliverables, offer decisions, and contact follow-ups
          </p>
        </div>

        <button
          onClick={() => setIsFeedModalOpen(true)}
          className="inline-flex items-center gap-2 px-3.5 py-2 bg-secondary text-secondary-foreground hover:bg-secondary/80 text-xs font-bold rounded-lg transition-colors shadow-2xs self-start sm:self-auto"
        >
          <Rss className="h-4 w-4 text-primary" />
          <span>Subscribe (.ics)</span>
        </button>
      </div>

      {/* Controls & Filter Toolbar */}
      <div className="flex flex-col md:flex-row md:items-center justify-between gap-4 bg-card border border-border rounded-xl p-3 shadow-xs">
        {/* Date Navigation */}
        <div className="flex items-center gap-2">
          <div className="flex items-center rounded-lg border border-border overflow-hidden">
            <button
              onClick={handlePrev}
              className="p-1.5 hover:bg-muted text-muted-foreground hover:text-foreground transition-colors"
              title="Previous"
            >
              <ChevronLeft className="h-4 w-4" />
            </button>
            <button
              onClick={handleToday}
              className="px-3 py-1.5 text-xs font-semibold hover:bg-muted transition-colors border-x border-border"
            >
              Today
            </button>
            <button
              onClick={handleNext}
              className="p-1.5 hover:bg-muted text-muted-foreground hover:text-foreground transition-colors"
              title="Next"
            >
              <ChevronRight className="h-4 w-4" />
            </button>
          </div>

          <h2 className="text-base font-bold text-foreground pl-2">
            {monthYearLabel}
          </h2>
        </div>

        {/* View Switcher & Event Category Filter Pills */}
        <div className="flex flex-wrap items-center gap-3">
          {/* Category Filter Pills */}
          <div className="flex items-center gap-1.5 text-xs font-medium">
            <button
              onClick={() => toggleType('Interview')}
              className={`px-2.5 py-1 rounded-md transition-colors flex items-center gap-1.5 border text-xs ${
                selectedTypes.Interview
                  ? 'bg-indigo-500/10 text-indigo-400 border-indigo-500/30'
                  : 'bg-muted/30 text-muted-foreground border-transparent opacity-60'
              }`}
            >
              <span className="h-2 w-2 rounded-full bg-indigo-500" />
              <span>Interviews</span>
            </button>

            <button
              onClick={() => toggleType('Task')}
              className={`px-2.5 py-1 rounded-md transition-colors flex items-center gap-1.5 border text-xs ${
                selectedTypes.Task
                  ? 'bg-blue-500/10 text-blue-400 border-blue-500/30'
                  : 'bg-muted/30 text-muted-foreground border-transparent opacity-60'
              }`}
            >
              <span className="h-2 w-2 rounded-full bg-blue-500" />
              <span>Tasks</span>
            </button>

            <button
              onClick={() => toggleType('OfferDeadline')}
              className={`px-2.5 py-1 rounded-md transition-colors flex items-center gap-1.5 border text-xs ${
                selectedTypes.OfferDeadline
                  ? 'bg-amber-500/10 text-amber-500 border-amber-500/30'
                  : 'bg-muted/30 text-muted-foreground border-transparent opacity-60'
              }`}
            >
              <span className="h-2 w-2 rounded-full bg-amber-500" />
              <span>Offer Deadlines</span>
            </button>

            <button
              onClick={() => toggleType('FollowUp')}
              className={`px-2.5 py-1 rounded-md transition-colors flex items-center gap-1.5 border text-xs ${
                selectedTypes.FollowUp
                  ? 'bg-emerald-500/10 text-emerald-500 border-emerald-500/30'
                  : 'bg-muted/30 text-muted-foreground border-transparent opacity-60'
              }`}
            >
              <span className="h-2 w-2 rounded-full bg-emerald-500" />
              <span>Follow-ups</span>
            </button>
          </div>

          {/* View Toggles (Month / Week / Agenda) */}
          <div className="flex items-center p-1 bg-muted/60 rounded-lg text-xs font-semibold ml-auto md:ml-0">
            <button
              onClick={() => setActiveView('month')}
              className={`px-3 py-1 rounded-md transition-colors ${
                activeView === 'month'
                  ? 'bg-card text-foreground shadow-2xs font-bold'
                  : 'text-muted-foreground hover:text-foreground'
              }`}
            >
              Month
            </button>
            <button
              onClick={() => setActiveView('week')}
              className={`px-3 py-1 rounded-md transition-colors ${
                activeView === 'week'
                  ? 'bg-card text-foreground shadow-2xs font-bold'
                  : 'text-muted-foreground hover:text-foreground'
              }`}
            >
              Week
            </button>
            <button
              onClick={() => setActiveView('agenda')}
              className={`px-3 py-1 rounded-md transition-colors ${
                activeView === 'agenda'
                  ? 'bg-card text-foreground shadow-2xs font-bold'
                  : 'text-muted-foreground hover:text-foreground'
              }`}
            >
              Agenda
            </button>
          </div>
        </div>
      </div>

      {/* Main View Area */}
      {isLoading ? (
        <div className="h-[480px] bg-card/40 border border-border/50 rounded-xl animate-pulse" />
      ) : activeView === 'month' ? (
        <MonthView currentDate={currentDate} events={filteredEvents} />
      ) : activeView === 'week' ? (
        <WeekView currentDate={currentDate} events={filteredEvents} />
      ) : (
        <AgendaView events={filteredEvents} />
      )}

      {/* Calendar Feed Subscription Modal */}
      <CalendarFeedModal
        isOpen={isFeedModalOpen}
        onClose={() => setIsFeedModalOpen(false)}
      />
    </div>
  );
};
