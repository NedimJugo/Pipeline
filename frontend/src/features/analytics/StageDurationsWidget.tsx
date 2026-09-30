import React from 'react';
import { Clock, Timer, ArrowRight, Gauge } from 'lucide-react';
import { StageDuration } from './types';

interface StageDurationsWidgetProps {
  durations: StageDuration[];
}

export const StageDurationsWidget: React.FC<StageDurationsWidgetProps> = ({ durations }) => {
  const activeDurations = durations.filter((d) => d.stage !== 'Wishlist');

  return (
    <div className="bg-card border border-border rounded-xl p-6 shadow-xs space-y-5">
      <div className="flex items-center gap-2 border-b border-border pb-4">
        <div className="p-2 rounded-lg bg-emerald-500/10 text-emerald-500">
          <Clock className="h-5 w-5" />
        </div>
        <div>
          <h3 className="text-sm font-bold text-foreground">Stage Velocity & Durations</h3>
          <p className="text-xs text-muted-foreground">
            Average and median days spent in each pipeline stage before moving
          </p>
        </div>
      </div>

      <div className="grid grid-cols-1 sm:grid-cols-2 lg:grid-cols-4 gap-4">
        {activeDurations.map((item) => (
          <div
            key={item.stage}
            className="p-4 rounded-xl bg-muted/20 border border-border/60 space-y-2 hover:border-primary/40 transition-colors"
          >
            <div className="flex items-center justify-between text-xs">
              <span className="font-bold text-foreground">{item.stage}</span>
              <span className="text-[11px] text-muted-foreground">
                {item.sampleCount} transitions
              </span>
            </div>

            <div className="flex items-baseline justify-between pt-1">
              <div>
                <span className="text-[11px] text-muted-foreground block">Average</span>
                <span className="text-lg font-extrabold text-foreground font-mono">
                  {item.averageDays} <span className="text-xs font-normal text-muted-foreground">days</span>
                </span>
              </div>

              <div className="text-right">
                <span className="text-[11px] text-muted-foreground block">Median</span>
                <span className="text-base font-bold text-foreground/90 font-mono">
                  {item.medianDays} <span className="text-xs font-normal text-muted-foreground">days</span>
                </span>
              </div>
            </div>

            <div className="w-full bg-muted/60 h-1.5 rounded-full overflow-hidden">
              <div
                className={`h-full rounded-full ${
                  item.averageDays <= 7
                    ? 'bg-emerald-500'
                    : item.averageDays <= 14
                    ? 'bg-amber-500'
                    : 'bg-indigo-500'
                }`}
                style={{ width: `${Math.min(100, Math.max(10, item.averageDays * 5))}%` }}
              />
            </div>
          </div>
        ))}
      </div>
    </div>
  );
};
