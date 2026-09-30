import React, { useState } from 'react';
import { BarChart3, Briefcase, FileText, Globe, Building } from 'lucide-react';
import { BreakdownMetric } from './types';

interface BreakdownTablesProps {
  bySource: BreakdownMetric[];
  byDocument: BreakdownMetric[];
  byWorkMode: BreakdownMetric[];
}

export const BreakdownTables: React.FC<BreakdownTablesProps> = ({
  bySource,
  byDocument,
  byWorkMode,
}) => {
  const [activeTab, setActiveTab] = useState<'source' | 'document' | 'workMode'>('source');

  const getActiveData = (): { label: string; data: BreakdownMetric[] } => {
    switch (activeTab) {
      case 'source':
        return { label: 'Application Source', data: bySource };
      case 'document':
        return { label: 'Resume / CV Version', data: byDocument };
      case 'workMode':
      default:
        return { label: 'Work Mode', data: byWorkMode };
    }
  };

  const current = getActiveData();

  return (
    <div className="bg-card border border-border rounded-xl p-6 shadow-xs space-y-6">
      <div className="flex flex-col sm:flex-row sm:items-center justify-between gap-4 border-b border-border pb-4">
        <div className="flex items-center gap-2">
          <div className="p-2 rounded-lg bg-indigo-500/10 text-indigo-500">
            <BarChart3 className="h-5 w-5" />
          </div>
          <div>
            <h3 className="text-sm font-bold text-foreground">Pipeline Dimension Breakdown</h3>
            <p className="text-xs text-muted-foreground">
              Compare response and interview yields across channels and materials
            </p>
          </div>
        </div>

        {/* Dimension Tabs */}
        <div className="flex items-center gap-1.5 p-1 bg-muted/50 rounded-lg text-xs font-semibold self-start sm:self-auto">
          <button
            onClick={() => setActiveTab('source')}
            className={`px-3 py-1.5 rounded-md transition-colors flex items-center gap-1.5 ${
              activeTab === 'source'
                ? 'bg-card text-foreground shadow-2xs'
                : 'text-muted-foreground hover:text-foreground'
            }`}
          >
            <Globe className="h-3.5 w-3.5" />
            <span>Source</span>
          </button>

          <button
            onClick={() => setActiveTab('document')}
            className={`px-3 py-1.5 rounded-md transition-colors flex items-center gap-1.5 ${
              activeTab === 'document'
                ? 'bg-card text-foreground shadow-2xs'
                : 'text-muted-foreground hover:text-foreground'
            }`}
          >
            <FileText className="h-3.5 w-3.5" />
            <span>CV Version</span>
          </button>

          <button
            onClick={() => setActiveTab('workMode')}
            className={`px-3 py-1.5 rounded-md transition-colors flex items-center gap-1.5 ${
              activeTab === 'workMode'
                ? 'bg-card text-foreground shadow-2xs'
                : 'text-muted-foreground hover:text-foreground'
            }`}
          >
            <Briefcase className="h-3.5 w-3.5" />
            <span>Work Mode</span>
          </button>
        </div>
      </div>

      {current.data.length === 0 ? (
        <div className="py-10 text-center text-xs text-muted-foreground">
          No breakdown metrics available for this category yet.
        </div>
      ) : (
        <div className="overflow-x-auto">
          <table className="w-full text-left text-xs border-collapse">
            <thead>
              <tr className="border-b border-border text-muted-foreground font-semibold">
                <th className="py-2.5 px-3">{current.label}</th>
                <th className="py-2.5 px-3 text-right">Applications</th>
                <th className="py-2.5 px-3 text-right">Responses</th>
                <th className="py-2.5 px-3 text-right">Response Rate</th>
                <th className="py-2.5 px-3 text-right">Interviews</th>
                <th className="py-2.5 px-3 text-right">Interview Rate</th>
                <th className="py-2.5 px-3 text-right">Offers</th>
              </tr>
            </thead>
            <tbody className="divide-y divide-border/60">
              {current.data.map((row) => (
                <tr key={row.groupKey} className="hover:bg-muted/20 transition-colors">
                  <td className="py-3 px-3 font-semibold text-foreground">
                    {row.label}
                  </td>
                  <td className="py-3 px-3 text-right font-mono font-medium">
                    {row.totalApplications}
                  </td>
                  <td className="py-3 px-3 text-right font-mono text-muted-foreground">
                    {row.responses}
                  </td>
                  <td className="py-3 px-3 text-right font-mono">
                    <span className={`px-2 py-0.5 rounded-md font-semibold ${
                      row.responseRate >= 30
                        ? 'bg-emerald-500/10 text-emerald-500'
                        : row.responseRate >= 15
                        ? 'bg-amber-500/10 text-amber-500'
                        : 'bg-muted text-muted-foreground'
                    }`}>
                      {row.responseRate}%
                    </span>
                  </td>
                  <td className="py-3 px-3 text-right font-mono text-muted-foreground">
                    {row.interviews}
                  </td>
                  <td className="py-3 px-3 text-right font-mono font-semibold text-foreground">
                    {row.interviewRate}%
                  </td>
                  <td className="py-3 px-3 text-right font-mono font-bold text-amber-500">
                    {row.offers}
                  </td>
                </tr>
              ))}
            </tbody>
          </table>
        </div>
      )}
    </div>
  );
};
