import React from 'react';
import { XCircle, AlertTriangle, ShieldAlert } from 'lucide-react';
import { RejectionStage } from './types';

interface RejectionBreakdownWidgetProps {
  rejections: RejectionStage[];
}

export const RejectionBreakdownWidget: React.FC<RejectionBreakdownWidgetProps> = ({ rejections }) => {
  const totalRejections = rejections.reduce((sum, r) => sum + r.count, 0);

  return (
    <div className="bg-card border border-border rounded-xl p-6 shadow-xs space-y-5">
      <div className="flex items-center gap-2 border-b border-border pb-4">
        <div className="p-2 rounded-lg bg-destructive/10 text-destructive">
          <XCircle className="h-5 w-5" />
        </div>
        <div>
          <h3 className="text-sm font-bold text-foreground">Rejection & Drop-off Diagnostics</h3>
          <p className="text-xs text-muted-foreground">
            Distribution of stages where non-advancing candidacies concluded
          </p>
        </div>
      </div>

      {rejections.length === 0 ? (
        <div className="py-8 text-center text-xs text-muted-foreground">
          No rejected or ghosted applications recorded in this timeframe.
        </div>
      ) : (
        <div className="space-y-3.5">
          {rejections.map((item) => (
            <div key={item.stage} className="space-y-1.5">
              <div className="flex items-center justify-between text-xs">
                <span className="font-semibold text-foreground">{item.stage}</span>
                <span className="font-mono text-muted-foreground">
                  <strong className="text-foreground">{item.count}</strong> ({item.percentage}%)
                </span>
              </div>

              <div className="w-full bg-muted/40 h-2 rounded-full overflow-hidden">
                <div
                  className="bg-destructive/80 h-full rounded-full transition-all duration-500"
                  style={{ width: `${item.percentage}%` }}
                />
              </div>
            </div>
          ))}
        </div>
      )}
    </div>
  );
};
