import React from 'react';
import { TrendingUp, Calendar, ArrowUpRight } from 'lucide-react';
import { WeeklyVelocity } from './types';

interface WeeklyVelocityChartProps {
  velocity: WeeklyVelocity[];
}

export const WeeklyVelocityChart: React.FC<WeeklyVelocityChartProps> = ({ velocity }) => {
  const maxVal = Math.max(
    ...velocity.map((v) => Math.max(v.applicationsCount, v.interviewsCount)),
    1
  );

  return (
    <div className="bg-card border border-border rounded-xl p-6 shadow-xs space-y-6">
      <div className="flex flex-col sm:flex-row sm:items-center justify-between gap-4 border-b border-border pb-4">
        <div className="flex items-center gap-2">
          <div className="p-2 rounded-lg bg-blue-500/10 text-blue-500">
            <TrendingUp className="h-5 w-5" />
          </div>
          <div>
            <h3 className="text-sm font-bold text-foreground">Weekly Momentum & Velocity</h3>
            <p className="text-xs text-muted-foreground">
              Applications submitted and interviews scheduled per calendar week
            </p>
          </div>
        </div>

        {/* Legend */}
        <div className="flex items-center gap-4 text-xs font-medium">
          <div className="flex items-center gap-1.5">
            <span className="h-3 w-3 rounded-xs bg-indigo-600 inline-block" />
            <span className="text-muted-foreground">Applications</span>
          </div>
          <div className="flex items-center gap-1.5">
            <span className="h-3 w-3 rounded-xs bg-emerald-500 inline-block" />
            <span className="text-muted-foreground">Interviews</span>
          </div>
        </div>
      </div>

      {velocity.length === 0 ? (
        <div className="py-12 text-center text-xs text-muted-foreground">
          No weekly trend data available yet.
        </div>
      ) : (
        <div className="grid grid-cols-4 sm:grid-cols-8 gap-3 items-end pt-4 pb-2">
          {velocity.map((w) => {
            const appHeightPct = Math.round((w.applicationsCount / maxVal) * 100);
            const intHeightPct = Math.round((w.interviewsCount / maxVal) * 100);

            return (
              <div key={w.weekLabel} className="flex flex-col items-center gap-2 group">
                <div className="h-36 w-full flex items-end justify-center gap-1.5 px-1">
                  {/* Applications bar */}
                  <div
                    className="w-full max-w-[14px] bg-indigo-600 rounded-t-md transition-all duration-300 relative group-hover:bg-indigo-500"
                    style={{ height: `${Math.max(4, appHeightPct)}%` }}
                    title={`${w.weekLabel}: ${w.applicationsCount} Applications`}
                  >
                    <span className="opacity-0 group-hover:opacity-100 transition-opacity absolute -top-5 left-1/2 -translate-x-1/2 text-[10px] font-bold text-indigo-400">
                      {w.applicationsCount}
                    </span>
                  </div>

                  {/* Interviews bar */}
                  <div
                    className="w-full max-w-[14px] bg-emerald-500 rounded-t-md transition-all duration-300 relative group-hover:bg-emerald-400"
                    style={{ height: `${Math.max(4, intHeightPct)}%` }}
                    title={`${w.weekLabel}: ${w.interviewsCount} Interviews`}
                  >
                    <span className="opacity-0 group-hover:opacity-100 transition-opacity absolute -top-5 left-1/2 -translate-x-1/2 text-[10px] font-bold text-emerald-400">
                      {w.interviewsCount}
                    </span>
                  </div>
                </div>

                <span className="text-[11px] font-medium text-muted-foreground group-hover:text-foreground transition-colors truncate">
                  {w.weekLabel}
                </span>
              </div>
            );
          })}
        </div>
      )}
    </div>
  );
};
