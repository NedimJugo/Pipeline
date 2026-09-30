import React, { useState } from 'react';
import { X, Calendar, Copy, Check, RefreshCw, ExternalLink, ShieldAlert } from 'lucide-react';
import { useCalendarFeedUrl, useRotateCalendarToken } from './useCalendar';

interface CalendarFeedModalProps {
  isOpen: boolean;
  onClose: () => void;
}

export const CalendarFeedModal: React.FC<CalendarFeedModalProps> = ({ isOpen, onClose }) => {
  const [copied, setCopied] = useState(false);
  const { data: feedData, isLoading } = useCalendarFeedUrl();
  const rotateMutation = useRotateCalendarToken();

  if (!isOpen) return null;

  const handleCopy = async () => {
    if (!feedData?.feedUrl) return;
    await navigator.clipboard.writeText(feedData.feedUrl);
    setCopied(true);
    setTimeout(() => setCopied(false), 2000);
  };

  const handleRotate = async () => {
    if (confirm('Are you sure you want to rotate your calendar feed key? Existing calendar subscriptions in Google/Apple/Outlook will stop updating until you update them with the new link.')) {
      await rotateMutation.mutateAsync();
    }
  };

  return (
    <div
      role="dialog"
      aria-modal="true"
      aria-labelledby="calendar-feed-modal-title"
      className="fixed inset-0 z-50 flex items-center justify-center p-4 bg-black/60 backdrop-blur-sm animate-in fade-in duration-200"
      onClick={onClose}
    >
      <div
        className="relative w-full max-w-lg bg-card border border-border rounded-xl shadow-2xl overflow-hidden flex flex-col max-h-[90vh]"
        onClick={(e) => e.stopPropagation()}
      >
        <div className="flex items-center justify-between px-6 py-4 border-b border-border bg-muted/30">
          <div className="flex items-center gap-2.5">
            <div className="p-2 rounded-lg bg-primary/10 text-primary">
              <Calendar className="h-5 w-5" />
            </div>
            <div>
              <h2 id="calendar-feed-modal-title" className="text-base font-bold tracking-tight">
                Subscribe to Calendar Feed
              </h2>
              <p className="text-xs text-muted-foreground">
                Sync interviews, deadlines, and tasks automatically with your external calendar
              </p>
            </div>
          </div>

          <button
            onClick={onClose}
            aria-label="Close modal"
            className="p-2 text-muted-foreground hover:text-foreground rounded-lg hover:bg-muted/80 transition-colors"
          >
            <X className="h-5 w-5" />
          </button>
        </div>

        <div className="p-6 overflow-y-auto space-y-5">
          <p className="text-xs text-muted-foreground leading-relaxed">
            Copy the secret calendar link below and paste it into your external calendar app (Apple Calendar, Google Calendar, Outlook, etc.) as an <strong>iCalendar Subscription URL</strong>. Events will stay in sync automatically.
          </p>

          <div className="space-y-2">
            <label className="text-xs font-semibold text-muted-foreground">Private iCalendar (.ics) Feed URL</label>
            <div className="flex items-center gap-2">
              <input
                type="text"
                readOnly
                value={isLoading ? 'Loading feed URL...' : feedData?.feedUrl || ''}
                className="flex-1 px-3 py-2 text-xs bg-muted/30 border border-border rounded-lg font-mono select-all focus:outline-none focus:ring-2 focus:ring-primary/20"
              />
              <button
                type="button"
                onClick={handleCopy}
                disabled={isLoading || !feedData?.feedUrl}
                className="inline-flex items-center gap-1.5 px-3.5 py-2 text-xs font-bold rounded-lg bg-primary text-primary-foreground hover:bg-primary/90 transition-colors shadow-2xs shrink-0"
              >
                {copied ? <Check className="h-4 w-4" /> : <Copy className="h-4 w-4" />}
                <span>{copied ? 'Copied' : 'Copy URL'}</span>
              </button>
            </div>
          </div>

          <div className="flex flex-wrap gap-2 pt-1">
            {feedData?.webcalUrl && (
              <a
                href={feedData.webcalUrl}
                className="inline-flex items-center gap-1.5 px-3 py-1.5 text-xs font-semibold rounded-lg bg-muted text-foreground hover:bg-muted/80 transition-colors"
              >
                <ExternalLink className="h-3.5 w-3.5" />
                <span>One-Click Subscribe (Webcal)</span>
              </a>
            )}

            <button
              type="button"
              onClick={handleRotate}
              disabled={rotateMutation.isPending}
              className="inline-flex items-center gap-1.5 px-3 py-1.5 text-xs font-medium rounded-lg text-muted-foreground hover:text-foreground hover:bg-muted/80 transition-colors ml-auto"
              title="Generate a new private subscription token if leaked"
            >
              <RefreshCw className={`h-3.5 w-3.5 ${rotateMutation.isPending ? 'animate-spin' : ''}`} />
              <span>Rotate Secret Key</span>
            </button>
          </div>

          <div className="p-3.5 rounded-lg bg-muted/40 border border-border text-xs text-muted-foreground space-y-1">
            <span className="font-semibold text-foreground block">How to add to Google Calendar:</span>
            <span>Settings → Add calendar → <strong>From URL</strong> → Paste the copied URL and click "Add calendar".</span>
          </div>
        </div>

        <div className="flex items-center justify-end px-6 py-3 border-t border-border bg-muted/20">
          <button
            type="button"
            onClick={onClose}
            className="px-4 py-1.5 text-xs font-semibold border border-border rounded-lg hover:bg-muted transition-colors"
          >
            Done
          </button>
        </div>
      </div>
    </div>
  );
};
