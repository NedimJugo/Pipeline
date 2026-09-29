import React, { useState } from 'react';
import { X, TrendingUp, BarChart2, Award, CheckCircle2, FileText, Send } from 'lucide-react';
import { DocumentVersionStats } from '../types';
import { useDocumentStatsSummary } from '../useDocuments';
import clsx from 'clsx';

interface DocumentVersionCompareModalProps {
  isOpen: boolean;
  onClose: () => void;
  initialType?: 'CV' | 'CoverLetter';
}

export const DocumentVersionCompareModal: React.FC<DocumentVersionCompareModalProps> = ({
  isOpen,
  onClose,
  initialType = 'CV',
}) => {
  const [selectedType, setSelectedType] = useState<'CV' | 'CoverLetter'>(initialType);
  const { data: statsSummary, isLoading } = useDocumentStatsSummary();

  React.useEffect(() => {
    const handleKeyDown = (e: KeyboardEvent) => {
      if (e.key === 'Escape') onClose();
    };
    if (isOpen) {
      window.addEventListener('keydown', handleKeyDown);
      return () => window.removeEventListener('keydown', handleKeyDown);
    }
  }, [isOpen, onClose]);

  if (!isOpen) return null;

  const versions: DocumentVersionStats[] =
    selectedType === 'CV'
      ? statsSummary?.cvVersionStats ?? []
      : statsSummary?.coverLetterStats ?? [];

  const topPerformer = [...versions].sort((a, b) => b.interviewRate - a.interviewRate)[0];

  return (
    <div className="fixed inset-0 z-50 flex items-center justify-center p-4 bg-background/80 backdrop-blur-sm animate-in fade-in duration-200">
      <div className="border border-border bg-card max-w-5xl w-full max-h-[90vh] rounded-2xl shadow-2xl flex flex-col overflow-hidden">
        {/* Header */}
        <div className="flex items-center justify-between px-6 py-4 border-b border-border bg-muted/30">
          <div className="flex items-center gap-3">
            <div className="p-2 rounded-lg bg-indigo-500/10 text-indigo-600 dark:text-indigo-400">
              <BarChart2 className="h-5 w-5" />
            </div>
            <div>
              <h2 className="text-base font-bold text-foreground">Compare Version Performance</h2>
              <p className="text-xs text-muted-foreground">
                Side-by-side funnel conversion analytics across resume versions
              </p>
            </div>
          </div>

          <div className="flex items-center gap-3">
            {/* Type selector toggle */}
            <div className="flex items-center p-0.5 bg-muted rounded-lg border border-border">
              <button
                type="button"
                onClick={() => setSelectedType('CV')}
                className={clsx(
                  'px-3 py-1 text-xs font-semibold rounded-md transition-colors',
                  selectedType === 'CV'
                    ? 'bg-card text-foreground shadow-2xs'
                    : 'text-muted-foreground hover:text-foreground'
                )}
              >
                CVs / Resumes
              </button>
              <button
                type="button"
                onClick={() => setSelectedType('CoverLetter')}
                className={clsx(
                  'px-3 py-1 text-xs font-semibold rounded-md transition-colors',
                  selectedType === 'CoverLetter'
                    ? 'bg-card text-foreground shadow-2xs'
                    : 'text-muted-foreground hover:text-foreground'
                )}
              >
                Cover Letters
              </button>
            </div>

            <button
              type="button"
              aria-label="Close modal"
              onClick={onClose}
              className="p-1.5 text-muted-foreground hover:text-foreground rounded-lg hover:bg-muted transition-colors"
            >
              <X className="h-5 w-5" />
            </button>
          </div>
        </div>

        {/* Body */}
        <div className="p-6 overflow-y-auto space-y-6 flex-1">
          {isLoading ? (
            <div className="h-64 flex items-center justify-center">
              <p className="text-xs text-muted-foreground animate-pulse">Calculating metrics...</p>
            </div>
          ) : versions.length === 0 ? (
            <div className="py-16 text-center space-y-3">
              <FileText className="h-10 w-10 text-muted-foreground/40 mx-auto" />
              <h3 className="text-sm font-semibold text-foreground">No Version Statistics Available</h3>
              <p className="text-xs text-muted-foreground max-w-sm mx-auto">
                Upload documents and link them to your job applications to see automatic conversion tracking here.
              </p>
            </div>
          ) : (
            <>
              {/* Highlight callout banner if top performer exists */}
              {topPerformer && topPerformer.sentCount > 0 && (
                <div className="p-4 rounded-xl bg-indigo-500/10 border border-indigo-500/20 flex items-center justify-between gap-4">
                  <div className="flex items-center gap-3">
                    <div className="p-2 rounded-lg bg-indigo-500 text-white shrink-0">
                      <Award className="h-5 w-5" />
                    </div>
                    <div>
                      <span className="text-[11px] font-bold uppercase tracking-wider text-indigo-600 dark:text-indigo-400">
                        Top Converting Version
                      </span>
                      <p className="text-sm font-bold text-foreground">
                        {topPerformer.documentTitle} ({topPerformer.versionLabel})
                      </p>
                    </div>
                  </div>
                  <div className="text-right">
                    <span className="text-xs text-muted-foreground block">Interview Conversion</span>
                    <span className="text-lg font-black text-indigo-600 dark:text-indigo-400">
                      {topPerformer.interviewRate}%
                    </span>
                  </div>
                </div>
              )}

              {/* Side-by-side cards grid */}
              <div className="grid grid-cols-1 md:grid-cols-2 lg:grid-cols-3 gap-4">
                {versions.map((v) => {
                  const isWinner = topPerformer?.versionId === v.versionId && v.sentCount > 0;
                  return (
                    <div
                      key={v.versionId}
                      className={clsx(
                        'bg-card border rounded-xl p-5 space-y-4 shadow-2xs relative flex flex-col justify-between',
                        isWinner ? 'border-indigo-500/50 ring-1 ring-indigo-500/30' : 'border-border'
                      )}
                    >
                      {isWinner && (
                        <span className="absolute -top-2.5 right-4 px-2 py-0.5 rounded-full bg-indigo-600 text-white font-bold text-[10px] uppercase tracking-wider shadow-sm flex items-center gap-1">
                          <Award className="h-3 w-3" />
                          Winner
                        </span>
                      )}

                      <div className="space-y-1">
                        <div className="flex items-center gap-2">
                          <span className="px-2 py-0.5 rounded-md bg-primary/10 text-primary font-mono font-bold text-xs">
                            {v.versionLabel}
                          </span>
                          <h4 className="font-semibold text-sm text-foreground truncate">
                            {v.documentTitle}
                          </h4>
                        </div>
                      </div>

                      {/* Primary Metrics */}
                      <div className="grid grid-cols-2 gap-2 pt-2 border-t border-border text-center">
                        <div className="bg-muted/40 p-2.5 rounded-lg border border-border">
                          <span className="text-[10px] font-semibold uppercase tracking-wider text-muted-foreground block">
                            Applications
                          </span>
                          <span className="text-base font-bold text-foreground mt-0.5 block flex items-center justify-center gap-1">
                            <Send className="h-3.5 w-3.5 text-muted-foreground" />
                            {v.sentCount}
                          </span>
                        </div>
                        <div className="bg-muted/40 p-2.5 rounded-lg border border-border">
                          <span className="text-[10px] font-semibold uppercase tracking-wider text-muted-foreground block">
                            Interviews
                          </span>
                          <span className="text-base font-bold text-foreground mt-0.5 block flex items-center justify-center gap-1">
                            <CheckCircle2 className="h-3.5 w-3.5 text-emerald-500" />
                            {v.interviewCount}
                          </span>
                        </div>
                      </div>

                      {/* Conversion Bars */}
                      <div className="space-y-3 pt-2">
                        {/* Response Rate */}
                        <div>
                          <div className="flex items-center justify-between text-xs mb-1">
                            <span className="text-muted-foreground flex items-center gap-1">
                              <TrendingUp className="h-3 w-3" /> Response Rate
                            </span>
                            <span className="font-bold text-foreground">{v.responseRate}%</span>
                          </div>
                          <div className="w-full h-2 rounded-full bg-muted overflow-hidden">
                            <div
                              className="h-full bg-blue-500 rounded-full transition-all duration-500"
                              style={{ width: `${Math.min(100, v.responseRate)}%` }}
                            />
                          </div>
                        </div>

                        {/* Interview Rate */}
                        <div>
                          <div className="flex items-center justify-between text-xs mb-1">
                            <span className="text-muted-foreground flex items-center gap-1">
                              <Award className="h-3 w-3" /> Interview Rate
                            </span>
                            <span className="font-bold text-emerald-600 dark:text-emerald-400">
                              {v.interviewRate}%
                            </span>
                          </div>
                          <div className="w-full h-2 rounded-full bg-muted overflow-hidden">
                            <div
                              className="h-full bg-emerald-500 rounded-full transition-all duration-500"
                              style={{ width: `${Math.min(100, v.interviewRate)}%` }}
                            />
                          </div>
                        </div>

                        {/* Offer Rate */}
                        <div>
                          <div className="flex items-center justify-between text-xs mb-1">
                            <span className="text-muted-foreground flex items-center gap-1">
                              <Award className="h-3 w-3 text-amber-500" /> Offer Rate
                            </span>
                            <span className="font-bold text-amber-600 dark:text-amber-400">
                              {v.offerRate}% ({v.offerCount})
                            </span>
                          </div>
                          <div className="w-full h-2 rounded-full bg-muted overflow-hidden">
                            <div
                              className="h-full bg-amber-500 rounded-full transition-all duration-500"
                              style={{ width: `${Math.min(100, v.offerRate)}%` }}
                            />
                          </div>
                        </div>
                      </div>
                    </div>
                  );
                })}
              </div>
            </>
          )}
        </div>
      </div>
    </div>
  );
};
