import React from 'react';
import { Sparkles, CheckCircle2, AlertTriangle, Info, ArrowUpRight } from 'lucide-react';
import { InsightCard } from './types';

interface InsightCardsGridProps {
  insights: InsightCard[];
}

export const InsightCardsGrid: React.FC<InsightCardsGridProps> = ({ insights }) => {
  const getInsightStyle = (type: InsightCard['type']) => {
    switch (type) {
      case 'positive':
        return {
          icon: <CheckCircle2 className="h-5 w-5 text-emerald-500 shrink-0" />,
          badgeClass: 'bg-emerald-500/10 text-emerald-500 border-emerald-500/20',
          borderClass: 'border-emerald-500/30 bg-emerald-500/5',
        };
      case 'warning':
        return {
          icon: <AlertTriangle className="h-5 w-5 text-amber-500 shrink-0" />,
          badgeClass: 'bg-amber-500/10 text-amber-500 border-amber-500/20',
          borderClass: 'border-amber-500/30 bg-amber-500/5',
        };
      case 'info':
      default:
        return {
          icon: <Info className="h-5 w-5 text-indigo-500 shrink-0" />,
          badgeClass: 'bg-indigo-500/10 text-indigo-500 border-indigo-500/20',
          borderClass: 'border-indigo-500/30 bg-indigo-500/5',
        };
    }
  };

  return (
    <div className="space-y-4">
      <div className="flex items-center gap-2">
        <div className="p-2 rounded-lg bg-primary/10 text-primary">
          <Sparkles className="h-4 w-4" />
        </div>
        <div>
          <h3 className="text-sm font-bold text-foreground">Actionable Pipeline Insights</h3>
          <p className="text-xs text-muted-foreground">
            Automated empirical observations and statistical pattern detections
          </p>
        </div>
      </div>

      <div className="grid grid-cols-1 md:grid-cols-2 gap-4">
        {insights.map((item) => {
          const style = getInsightStyle(item.type);

          return (
            <div
              key={item.id}
              className={`p-4 rounded-xl border ${style.borderClass} shadow-2xs space-y-2.5 transition-all hover:scale-[1.01]`}
            >
              <div className="flex items-start justify-between gap-3">
                <div className="flex items-center gap-2.5">
                  {style.icon}
                  <h4 className="font-bold text-sm tracking-tight text-foreground">
                    {item.title}
                  </h4>
                </div>

                {item.metric && (
                  <span className={`px-2.5 py-0.5 rounded-full text-xs font-bold border shrink-0 ${style.badgeClass}`}>
                    {item.metric}
                  </span>
                )}
              </div>

              <p className="text-xs text-muted-foreground leading-relaxed pl-7">
                {item.description}
              </p>
            </div>
          );
        })}
      </div>
    </div>
  );
};
