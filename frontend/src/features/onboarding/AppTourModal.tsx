import React, { useState } from 'react';
import {
  X,
  LayoutDashboard,
  KanbanSquare,
  Users,
  Sparkles,
  ArrowRight,
  ArrowLeft,
  CheckCircle2,
  Calendar,
  Search,
} from 'lucide-react';

interface AppTourModalProps {
  isOpen: boolean;
  onClose: () => void;
}

interface TourSlide {
  title: string;
  badge: string;
  icon: React.ComponentType<{ className?: string }>;
  description: string;
  highlights: string[];
  color: string;
}

const TOUR_SLIDES: TourSlide[] = [
  {
    title: 'Your Daily Command Center',
    badge: 'Today Dashboard',
    icon: LayoutDashboard,
    description:
      'Start each morning here. Pipeline automatically prioritizes your "Do Today" queue, flags applications that have gone stale, and surfaces upcoming interview preparation checklists.',
    highlights: [
      'Prioritized "Do Today" action items',
      'Stale application follow-up reminders',
      'Immediate prep checklists for upcoming rounds',
    ],
    color: 'text-indigo-500 bg-indigo-500/10 border-indigo-500/20',
  },
  {
    title: 'Visual Drag-and-Drop Pipeline',
    badge: 'Applications Pipeline',
    icon: KanbanSquare,
    description:
      'Organize all your opportunities seamlessly from Wishlist and Applied through Interviews and Offers. Seamlessly log historical application dates, notes, and salary ranges.',
    highlights: [
      'Kanban board and filterable table views',
      'Accurate historical date logging (backfill past applications)',
      'Multi-currency salary compensation tracking',
    ],
    color: 'text-emerald-500 bg-emerald-500/10 border-emerald-500/20',
  },
  {
    title: 'Networking & Document Management',
    badge: 'Contacts & Documents',
    icon: Users,
    description:
      'Keep track of recruiters, hiring managers, and referees. Store multiple resume and cover letter versions and view conversion analytics per document.',
    highlights: [
      'Warmth badges (Cold, Warm, Strong, Advocate)',
      'Referee consent enforcement and notification emails',
      'Resume versioning with interview conversion tracking',
    ],
    color: 'text-amber-500 bg-amber-500/10 border-amber-500/20',
  },
  {
    title: 'Speed, Automation & Calendar',
    badge: 'Discovery & Command Palette',
    icon: Sparkles,
    description:
      'Discover opportunities with 1-click wishlist conversion, sync all interviews into your calendar with a live .ics feed, and press Ctrl+K anywhere to navigate in seconds.',
    highlights: [
      'Global Command Palette (Ctrl+K or Cmd+K)',
      'Live subscribe-able .ics calendar feed',
      '100% private, self-hosted data management & backup',
    ],
    color: 'text-sky-500 bg-sky-500/10 border-sky-500/20',
  },
];

export const AppTourModal: React.FC<AppTourModalProps> = ({ isOpen, onClose }) => {
  const [currentSlideIndex, setCurrentSlideIndex] = useState(0);

  if (!isOpen) return null;

  const currentSlide = TOUR_SLIDES[currentSlideIndex];
  const Icon = currentSlide.icon;
  const isFirstSlide = currentSlideIndex === 0;
  const isLastSlide = currentSlideIndex === TOUR_SLIDES.length - 1;

  const handleNext = () => {
    if (isLastSlide) {
      onClose();
    } else {
      setCurrentSlideIndex((prev) => prev + 1);
    }
  };

  const handlePrev = () => {
    if (!isFirstSlide) {
      setCurrentSlideIndex((prev) => prev - 1);
    }
  };

  return (
    <div className="fixed inset-0 z-50 flex items-center justify-center p-4 bg-background/80 backdrop-blur-sm animate-in fade-in duration-200">
      <div className="border border-border bg-card max-w-xl w-full rounded-2xl shadow-2xl overflow-hidden flex flex-col">
        {/* Header */}
        <div className="px-6 py-4 border-b border-border flex items-center justify-between">
          <div className="flex items-center gap-2">
            <img src="/app_icon.png" alt="Pipeline" className="h-6 w-6 rounded-md object-contain" />
            <span className="font-bold text-sm tracking-tight">Welcome to Pipeline Tour</span>
          </div>
          <button
            type="button"
            onClick={onClose}
            className="p-1 rounded-lg text-muted-foreground hover:text-foreground hover:bg-muted transition-colors"
          >
            <X className="h-4 w-4" />
          </button>
        </div>

        {/* Slide Content */}
        <div className="p-6 space-y-6">
          <div className="flex items-start gap-4">
            <div className={`p-3.5 rounded-2xl border ${currentSlide.color} shrink-0`}>
              <Icon className="h-7 w-7" />
            </div>
            <div className="space-y-1">
              <span className="text-[11px] font-semibold uppercase tracking-wider text-muted-foreground">
                {currentSlide.badge}
              </span>
              <h3 className="text-xl font-bold tracking-tight">{currentSlide.title}</h3>
            </div>
          </div>

          <p className="text-sm text-muted-foreground leading-relaxed">
            {currentSlide.description}
          </p>

          <div className="space-y-2 bg-muted/30 border border-border/80 p-4 rounded-xl">
            <span className="text-xs font-semibold text-foreground block mb-2">Key Capabilities:</span>
            {currentSlide.highlights.map((h, i) => (
              <div key={i} className="flex items-center gap-2 text-xs text-foreground/90">
                <CheckCircle2 className="h-4 w-4 text-emerald-500 shrink-0" />
                <span>{h}</span>
              </div>
            ))}
          </div>

          {/* Stepper Dots & Navigation */}
          <div className="flex items-center justify-between pt-2 border-t border-border/60">
            <div className="flex items-center gap-1.5">
              {TOUR_SLIDES.map((_, idx) => (
                <button
                  key={idx}
                  type="button"
                  onClick={() => setCurrentSlideIndex(idx)}
                  className={`h-2 rounded-full transition-all ${
                    idx === currentSlideIndex
                      ? 'w-6 bg-primary'
                      : 'w-2 bg-border hover:bg-muted-foreground/40'
                  }`}
                  aria-label={`Go to slide ${idx + 1}`}
                />
              ))}
              <span className="text-[11px] text-muted-foreground ml-2 font-mono">
                {currentSlideIndex + 1} / {TOUR_SLIDES.length}
              </span>
            </div>

            <div className="flex items-center gap-2">
              {!isFirstSlide && (
                <button
                  type="button"
                  onClick={handlePrev}
                  className="px-3 py-1.5 rounded-lg border border-border text-xs font-semibold hover:bg-muted transition-colors flex items-center gap-1"
                >
                  <ArrowLeft className="h-3.5 w-3.5" />
                  <span>Previous</span>
                </button>
              )}
              <button
                type="button"
                onClick={handleNext}
                className="px-4 py-1.5 rounded-lg bg-primary text-primary-foreground text-xs font-bold hover:bg-primary/90 transition-colors shadow-xs flex items-center gap-1.5"
              >
                <span>{isLastSlide ? 'Finish Tour' : 'Next'}</span>
                <ArrowRight className="h-3.5 w-3.5" />
              </button>
            </div>
          </div>
        </div>
      </div>
    </div>
  );
};
