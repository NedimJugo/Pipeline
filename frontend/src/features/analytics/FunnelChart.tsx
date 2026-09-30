import React from 'react';
import { Filter, ArrowDownRight, CheckCircle2, TrendingUp, Layers } from 'lucide-react';
import { FunnelStage } from './types';

interface FunnelChartProps {
  stages: FunnelStage[];
}

export const FunnelChart: React.FC<FunnelChartProps> = ({ stages }) => {
  const maxCount = Math.max(...stages.map((s) => s.count), 1);

  const getStageColor = (index: number) => {
    switch (index) {
      case 0:
        return 'from-blue-600 to-indigo-600 text-blue-500';
      case 1:
        return 'from-indigo-600 to-violet-600 text-indigo-500';
      case 2:
        return 'from-violet-600 to-purple-600 text-purple-500';
      case 3:
        return 'from-amber-500 to-amber-600 text-amber-500';
      case 4:
      default:
        return 'from-emerald-500 to-teal-500 text-emerald-500';
    }
  };

  const totalApplied = stages.find((s) => s.stage === 'Applied')?.count || 0;

  return (
    <div className="bg-card border border-border rounded-xl p-6 shadow-xs space-y-6">
      <div className="flex flex-col sm:flex-row sm:items-center justify-between gap-2 border-b border-border pb-4">
        <div className="flex items-center gap-2">
          <div className="p-2 rounded-lg bg-primary/10 text-primary">
            <Layers className="h-5 w-5" />
          </div>
          <div>
            <h3 className="text-sm font-bold text-foreground">Pipeline Conversion Funnel</h3>
            <p className="text-xs text-muted-foreground">
              Candidate progression from submission to final accepted offer
            </p>
          </div>
        </div>

        <div className="flex items-center gap-4 text-xs font-semibold text-muted-foreground">
          <span>Overall Throughput: <strong className="text-foreground">{stages[stages.length - 1]?.conversionFromApplied ?? 0}%</strong></span>
        </div>
      </div>

      {totalApplied === 0 ? (
        <div className="py-12 text-center text-xs text-muted-foreground border border-dashed border-border rounded-xl">
          <p className="font-semibold text-foreground">No applications in this timeframe</p>
          <p className="mt-1">Submit applications or expand your date range to populate the funnel.</p>
        </div>
      ) : (
        <div className="space-y-4">
          {stages.map((stage, idx) => {
            const widthPct = Math.max(14, Math.round((stage.count / maxCount) * 100));
            const prevCount = idx > 0 ? stages[idx - 1].count : stage.count;
            const dropPct = prevCount > 0 ? Math.round(((prevCount - stage.count) / prevCount) * 100) : 0;

            return (
              <div key={stage.stage} className="space-y-1.5 group">
                <div className="flex items-center justify-between text-xs">
                  <div className="flex items-center gap-2">
                    <span className="font-bold text-foreground text-sm">{stage.stage}</span>
                    <span className="px-2 py-0.5 rounded-full text-[11px] font-semibold bg-muted text-muted-foreground">
                      {stage.count} {stage.count === 1 ? 'app' : 'apps'}
                    </span>
                  </div>

                  <div className="flex items-center gap-3 font-mono text-xs">
                    {idx > 0 && dropPct > 0 && (
                      <span className="text-muted-foreground/75 flex items-center text-[11px]">
                        <ArrowDownRight className="h-3 w-3 text-destructive mr-0.5" />
                        -{dropPct}% drop
                      </span>
                    )}
                    <span className="font-semibold text-foreground">
                      {stage.conversionFromApplied}% of total
                    </span>
                  </div>
                </div>

                {/* Funnel Bar */}
                <div className="h-8 w-full bg-muted/30 rounded-lg overflow-hidden flex items-center p-1 relative border border-border/40">
                  <div
                    className={`h-full rounded-md bg-gradient-to-r ${getStageColor(idx)} opacity-90 transition-all duration-500 flex items-center justify-between px-3 text-white text-xs font-bold shadow-2xs`}
                    style={{ width: `${widthPct}%` }}
                  >
                    <span className="truncate">{stage.stage}</span>
                    <span className="shrink-0">{stage.count}</span>
                  </div>
                </div>
              </div>
            );
          })}
        </div>
      )}
    </div>
  );
};
