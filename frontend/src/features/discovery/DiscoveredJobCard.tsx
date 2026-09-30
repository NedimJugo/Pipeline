import React from 'react';
import {
  Building2,
  MapPin,
  ExternalLink,
  BookmarkPlus,
  CheckCircle2,
  XCircle,
  Clock,
  Sparkles,
  Loader2,
} from 'lucide-react';
import { DiscoveredJob } from './types';

interface DiscoveredJobCardProps {
  job: DiscoveredJob;
  onSave: (id: string) => void;
  onDismiss: (id: string) => void;
  isSaving?: boolean;
  isDismissing?: boolean;
}

export const DiscoveredJobCard: React.FC<DiscoveredJobCardProps> = ({
  job,
  onSave,
  onDismiss,
  isSaving = false,
  isDismissing = false,
}) => {
  const isSaved = job.status === 'Saved';
  const isDismissed = job.status === 'Dismissed';

  const formatPostedAt = (dateStr: string | null) => {
    if (!dateStr) return 'Recently';
    const date = new Date(dateStr);
    const now = new Date();
    const diffHours = Math.round((now.getTime() - date.getTime()) / (1000 * 60 * 60));
    if (diffHours < 24) return `${diffHours}h ago`;
    const diffDays = Math.round(diffHours / 24);
    return `${diffDays}d ago`;
  };

  return (
    <div
      className={`bg-card border rounded-xl p-5 shadow-xs transition-all flex flex-col justify-between ${
        isSaved
          ? 'border-emerald-500/40 bg-emerald-500/5'
          : isDismissed
          ? 'border-border/60 opacity-60 bg-muted/10'
          : 'border-border hover:border-border/80 hover:shadow-sm'
      }`}
    >
      <div className="space-y-3">
        {/* Top Header: Company, Source & Status */}
        <div className="flex items-start justify-between gap-3">
          <div className="min-w-0">
            <div className="flex items-center gap-2 text-xs font-semibold text-muted-foreground truncate">
              <Building2 className="h-3.5 w-3.5 shrink-0" />
              <span className="truncate">{job.companyName}</span>
              <span className="text-border">•</span>
              <span className="text-[10px] px-2 py-0.5 rounded-full bg-muted font-medium shrink-0">
                {job.sourceName}
              </span>
            </div>
            <h3 className="text-base font-bold tracking-tight text-foreground mt-1 truncate" title={job.title}>
              {job.title}
            </h3>
          </div>

          <div className="shrink-0">
            {isSaved && (
              <span className="inline-flex items-center gap-1 px-2.5 py-1 rounded-full text-[11px] font-bold bg-emerald-500/10 text-emerald-600 dark:text-emerald-400 border border-emerald-500/20">
                <CheckCircle2 className="h-3 w-3" />
                <span>Saved</span>
              </span>
            )}
            {isDismissed && (
              <span className="inline-flex items-center gap-1 px-2 py-0.5 rounded text-[11px] font-medium bg-muted text-muted-foreground">
                <XCircle className="h-3 w-3" />
                <span>Dismissed</span>
              </span>
            )}
            {!isSaved && !isDismissed && (
              <span className="inline-flex items-center gap-1 px-2.5 py-1 rounded-full text-[11px] font-bold bg-primary/10 text-primary border border-primary/20">
                <Sparkles className="h-3 w-3" />
                <span>New</span>
              </span>
            )}
          </div>
        </div>

        {/* Location & Time */}
        <div className="flex flex-wrap items-center gap-x-4 gap-y-1 text-xs text-muted-foreground">
          {job.location && (
            <div className="flex items-center gap-1">
              <MapPin className="h-3 w-3 shrink-0" />
              <span>{job.location}</span>
            </div>
          )}
          <div className="flex items-center gap-1">
            <Clock className="h-3 w-3 shrink-0" />
            <span>{formatPostedAt(job.postedAt)}</span>
          </div>
        </div>

        {/* Description */}
        {job.description && (
          <p className="text-xs text-muted-foreground line-clamp-2 leading-relaxed">
            {job.description}
          </p>
        )}

        {/* Tags */}
        {job.tags && job.tags.length > 0 && (
          <div className="flex flex-wrap gap-1.5 pt-1">
            {job.tags.slice(0, 4).map((tag, idx) => (
              <span
                key={idx}
                className="text-[11px] px-2 py-0.5 rounded bg-muted/60 text-muted-foreground font-medium border border-border/50"
              >
                {tag}
              </span>
            ))}
            {job.tags.length > 4 && (
              <span className="text-[10px] px-1.5 py-0.5 rounded text-muted-foreground font-medium">
                +{job.tags.length - 4}
              </span>
            )}
          </div>
        )}
      </div>

      {/* Action Footer */}
      <div className="mt-5 pt-3 border-t border-border flex items-center justify-between gap-2">
        <a
          href={job.url}
          target="_blank"
          rel="noopener noreferrer"
          className="inline-flex items-center gap-1 text-xs font-semibold text-primary hover:underline"
        >
          <span>View Posting</span>
          <ExternalLink className="h-3 w-3" />
        </a>

        <div className="flex items-center gap-2">
          {!isDismissed && (
            <button
              type="button"
              onClick={() => onDismiss(job.id)}
              disabled={isDismissing}
              className="px-2.5 py-1.5 rounded-lg text-xs font-medium text-muted-foreground hover:text-foreground hover:bg-muted transition-colors disabled:opacity-50"
            >
              Dismiss
            </button>
          )}

          <button
            type="button"
            onClick={() => onSave(job.id)}
            disabled={isSaved || isSaving}
            className={`inline-flex items-center gap-1.5 px-3.5 py-1.5 rounded-lg text-xs font-bold transition-colors shadow-2xs disabled:opacity-60 ${
              isSaved
                ? 'bg-emerald-600 text-white'
                : 'bg-primary text-primary-foreground hover:bg-primary/90'
            }`}
          >
            {isSaving ? (
              <>
                <Loader2 className="h-3.5 w-3.5 animate-spin" />
                <span>Saving...</span>
              </>
            ) : isSaved ? (
              <>
                <CheckCircle2 className="h-3.5 w-3.5" />
                <span>In Wishlist</span>
              </>
            ) : (
              <>
                <BookmarkPlus className="h-3.5 w-3.5" />
                <span>Save to Wishlist</span>
              </>
            )}
          </button>
        </div>
      </div>
    </div>
  );
};
