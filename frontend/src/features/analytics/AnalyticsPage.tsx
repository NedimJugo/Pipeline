import React, { useState } from 'react';
import {
  TrendingUp,
  Clock,
  Layers,
  Sparkles,
  Calendar,
  CheckCircle2,
  FileText,
  Briefcase,
  AlertCircle,
  BarChart3,
  RefreshCw,
} from 'lucide-react';
import { useAnalyticsOverview } from './useAnalytics';
import { FunnelChart } from './FunnelChart';
import { BreakdownTables } from './BreakdownTables';
import { StageDurationsWidget } from './StageDurationsWidget';
import { WeeklyVelocityChart } from './WeeklyVelocityChart';
import { RejectionBreakdownWidget } from './RejectionBreakdownWidget';
import { InsightCardsGrid } from './InsightCardsGrid';

export const AnalyticsPage: React.FC = () => {
  const [selectedRange, setSelectedRange] = useState<number | null>(90);

  const { data: analytics, isLoading, refetch, isFetching } = useAnalyticsOverview(selectedRange);

  return (
    <div className="max-w-7xl mx-auto space-y-8 pb-12">
      {/* Page Header */}
      <div className="flex flex-col sm:flex-row sm:items-center justify-between gap-4 pb-6 border-b border-border">
        <div>
          <h1 className="text-2xl font-bold tracking-tight">Search Analytics & Performance</h1>
          <p className="text-sm text-muted-foreground mt-1">
            Track pipeline throughput, response rates by channel, stage velocity, and algorithmic insights
          </p>
        </div>

        {/* Date Range Selector */}
        <div className="flex items-center gap-2 self-start sm:self-auto">
          <div className="flex items-center p-1 bg-muted/60 rounded-lg text-xs font-semibold">
            <button
              onClick={() => setSelectedRange(30)}
              className={`px-3 py-1.5 rounded-md transition-colors ${
                selectedRange === 30
                  ? 'bg-card text-foreground shadow-2xs font-bold'
                  : 'text-muted-foreground hover:text-foreground'
              }`}
            >
              30 Days
            </button>
            <button
              onClick={() => setSelectedRange(90)}
              className={`px-3 py-1.5 rounded-md transition-colors ${
                selectedRange === 90
                  ? 'bg-card text-foreground shadow-2xs font-bold'
                  : 'text-muted-foreground hover:text-foreground'
              }`}
            >
              90 Days
            </button>
            <button
              onClick={() => setSelectedRange(365)}
              className={`px-3 py-1.5 rounded-md transition-colors ${
                selectedRange === 365
                  ? 'bg-card text-foreground shadow-2xs font-bold'
                  : 'text-muted-foreground hover:text-foreground'
              }`}
            >
              12 Months
            </button>
            <button
              onClick={() => setSelectedRange(null)}
              className={`px-3 py-1.5 rounded-md transition-colors ${
                selectedRange === null
                  ? 'bg-card text-foreground shadow-2xs font-bold'
                  : 'text-muted-foreground hover:text-foreground'
              }`}
            >
              All Time
            </button>
          </div>

          <button
            onClick={() => refetch()}
            disabled={isFetching}
            className="p-2 border border-border rounded-lg text-muted-foreground hover:text-foreground hover:bg-muted/60 transition-colors"
            title="Refresh metrics"
          >
            <RefreshCw className={`h-4 w-4 ${isFetching ? 'animate-spin' : ''}`} />
          </button>
        </div>
      </div>

      {isLoading ? (
        <div className="space-y-6">
          <div className="grid grid-cols-2 lg:grid-cols-4 gap-4">
            {[1, 2, 3, 4].map((i) => (
              <div key={i} className="h-24 bg-card/40 border border-border/50 rounded-xl animate-pulse" />
            ))}
          </div>
          <div className="h-72 bg-card/40 border border-border/50 rounded-xl animate-pulse" />
          <div className="h-72 bg-card/40 border border-border/50 rounded-xl animate-pulse" />
        </div>
      ) : !analytics ? (
        <div className="py-20 text-center text-xs text-muted-foreground border border-dashed border-border rounded-xl space-y-2">
          <AlertCircle className="h-8 w-8 text-muted-foreground mx-auto" />
          <p className="font-bold text-foreground text-sm">Unable to load analytics</p>
          <p>Please refresh the page or check your connection.</p>
        </div>
      ) : (
        <div className="space-y-8">
          {/* Top KPI Cards */}
          <div className="grid grid-cols-2 lg:grid-cols-4 gap-4">
            <div className="p-5 rounded-xl bg-card border border-border shadow-2xs space-y-1">
              <span className="text-xs font-semibold text-muted-foreground block">Total Applications</span>
              <span className="text-2xl font-black text-foreground font-mono">
                {analytics.totalApplications}
              </span>
              <span className="text-[11px] text-muted-foreground block">
                {analytics.activeApplications} active search{analytics.activeApplications === 1 ? '' : 'es'}
              </span>
            </div>

            <div className="p-5 rounded-xl bg-card border border-border shadow-2xs space-y-1">
              <span className="text-xs font-semibold text-muted-foreground block">Response Rate</span>
              <span className="text-2xl font-black text-foreground font-mono">
                {analytics.overallResponseRate}%
              </span>
              <span className="text-[11px] text-emerald-500 font-semibold block">
                Advanced past applied
              </span>
            </div>

            <div className="p-5 rounded-xl bg-card border border-border shadow-2xs space-y-1">
              <span className="text-xs font-semibold text-muted-foreground block">Interview Rate</span>
              <span className="text-2xl font-black text-foreground font-mono">
                {analytics.overallInterviewRate}%
              </span>
              <span className="text-[11px] text-indigo-500 font-semibold block">
                Reached technical / phone screens
              </span>
            </div>

            <div className="p-5 rounded-xl bg-card border border-border shadow-2xs space-y-1">
              <span className="text-xs font-semibold text-muted-foreground block">Active Inquiries</span>
              <span className="text-2xl font-black text-foreground font-mono">
                {analytics.activeApplications}
              </span>
              <span className="text-[11px] text-muted-foreground block">
                Excludes rejected & ghosted
              </span>
            </div>
          </div>

          {/* Actionable Insights */}
          <InsightCardsGrid insights={analytics.insights} />

          {/* Pipeline Funnel */}
          <FunnelChart stages={analytics.funnel} />

          {/* Dimensional Breakdown Tables */}
          <BreakdownTables
            bySource={analytics.bySource}
            byDocument={analytics.byDocument}
            byWorkMode={analytics.byWorkMode}
          />

          {/* Stage Durations Benchmark */}
          <StageDurationsWidget durations={analytics.stageDurations} />

          {/* Weekly Velocity and Rejection Breakdown */}
          <div className="grid grid-cols-1 lg:grid-cols-2 gap-6">
            <WeeklyVelocityChart velocity={analytics.weeklyVelocity} />
            <RejectionBreakdownWidget rejections={analytics.rejectionStages} />
          </div>
        </div>
      )}
    </div>
  );
};
