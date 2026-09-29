import React from 'react';
import { Interaction } from '../types';
import { useDeleteInteraction } from '../useContacts';
import {
  Mail,
  Linkedin,
  Phone,
  Video,
  Users,
  MessageSquare,
  ArrowUpRight,
  ArrowDownLeft,
  Calendar,
  Trash2,
  Briefcase,
  AlertCircle,
} from 'lucide-react';
import { clsx } from 'clsx';

interface ContactTimelineProps {
  interactions: Interaction[];
  onNewInteractionClick?: () => void;
}

export const ContactTimeline: React.FC<ContactTimelineProps> = ({
  interactions,
  onNewInteractionClick,
}) => {
  const deleteMutation = useDeleteInteraction();

  const getChannelIcon = (channel: string) => {
    switch (channel) {
      case 'Email':
        return <Mail className="h-4 w-4 text-blue-500" />;
      case 'LinkedIn':
        return <Linkedin className="h-4 w-4 text-sky-600" />;
      case 'Phone':
        return <Phone className="h-4 w-4 text-emerald-500" />;
      case 'Video':
        return <Video className="h-4 w-4 text-purple-500" />;
      case 'InPerson':
        return <Users className="h-4 w-4 text-amber-500" />;
      default:
        return <MessageSquare className="h-4 w-4 text-muted-foreground" />;
    }
  };

  const formatDate = (dateStr: string) => {
    try {
      const date = new Date(dateStr);
      return date.toLocaleDateString('en-US', {
        month: 'short',
        day: 'numeric',
        year: 'numeric',
        hour: '2-digit',
        minute: '2-digit',
      });
    } catch {
      return dateStr;
    }
  };

  if (interactions.length === 0) {
    return (
      <div className="flex flex-col items-center justify-center p-12 text-center border border-dashed border-border rounded-xl bg-card">
        <MessageSquare className="h-10 w-10 text-muted-foreground/50 mb-3" />
        <h3 className="font-semibold text-sm text-foreground">No Interactions Logged</h3>
        <p className="text-xs text-muted-foreground mt-1 max-w-sm">
          Log emails, LinkedIn messages, phone screens, or meetings to keep your communication timeline and relationship warmth up to date.
        </p>
        {onNewInteractionClick && (
          <button
            type="button"
            onClick={onNewInteractionClick}
            className="mt-4 px-4 py-1.5 text-xs font-semibold bg-primary text-primary-foreground rounded-lg hover:opacity-90 shadow-sm"
          >
            Log First Interaction
          </button>
        )}
      </div>
    );
  }

  return (
    <div className="relative pl-6 space-y-6 before:absolute before:bottom-0 before:top-2 before:left-[15px] before:w-[2px] before:bg-border">
      {interactions.map((item) => (
        <div key={item.id} className="relative group">
          {/* Channel icon badge */}
          <div className="absolute -left-[30px] top-1 flex h-7 w-7 items-center justify-center rounded-full bg-card border border-border shadow-xs group-hover:scale-110 transition-transform">
            {getChannelIcon(item.channel)}
          </div>

          {/* Interaction card */}
          <div className="bg-card border border-border rounded-xl p-4 shadow-2xs hover:border-primary/30 transition-colors space-y-3">
            <div className="flex items-start justify-between gap-2 flex-wrap">
              <div className="flex items-center gap-2 flex-wrap">
                <span className="px-2 py-0.5 rounded-md text-[10px] font-bold uppercase tracking-wider bg-muted text-foreground border border-border">
                  {item.channel}
                </span>

                <span
                  className={clsx(
                    'inline-flex items-center gap-1 px-2 py-0.5 rounded-md text-[10px] font-semibold border',
                    item.direction === 'Outbound'
                      ? 'bg-primary/10 text-primary border-primary/20'
                      : 'bg-emerald-500/10 text-emerald-600 dark:text-emerald-400 border-emerald-500/20'
                  )}
                >
                  {item.direction === 'Outbound' ? (
                    <ArrowUpRight className="h-3 w-3" />
                  ) : (
                    <ArrowDownLeft className="h-3 w-3" />
                  )}
                  <span>{item.direction}</span>
                </span>

                {item.applicationRole && (
                  <span className="inline-flex items-center gap-1 px-2 py-0.5 rounded-md text-[11px] font-medium bg-muted/60 text-muted-foreground border border-border">
                    <Briefcase className="h-3 w-3 text-muted-foreground" />
                    <span>
                      {item.companyName ? `${item.companyName} • ` : ''}
                      {item.applicationRole}
                    </span>
                  </span>
                )}
              </div>

              <div className="flex items-center gap-2">
                <time className="text-[11px] text-muted-foreground font-mono">
                  {formatDate(item.occurredAt)}
                </time>
                <button
                  type="button"
                  onClick={() => deleteMutation.mutate(item.id)}
                  disabled={deleteMutation.isPending}
                  className="opacity-0 group-hover:opacity-100 p-1 text-muted-foreground hover:text-destructive hover:bg-destructive/10 rounded transition-all"
                  title="Delete interaction"
                >
                  <Trash2 className="h-3.5 w-3.5" />
                </button>
              </div>
            </div>

            {/* Summary */}
            <p className="text-xs text-foreground/90 leading-relaxed whitespace-pre-wrap">
              {item.summary}
            </p>

            {/* Sent Content block if present */}
            {item.sentContent && (
              <div className="bg-muted/40 border-l-2 border-primary/60 p-2.5 rounded-r-lg text-xs text-muted-foreground font-mono leading-relaxed whitespace-pre-wrap">
                {item.sentContent}
              </div>
            )}

            {/* Follow-up required badge */}
            {item.followUpRequired && item.followUpDueAt && (
              <div className="inline-flex items-center gap-1.5 px-2.5 py-1 rounded-md bg-amber-500/10 border border-amber-500/20 text-amber-600 dark:text-amber-400 text-xs font-semibold">
                <Calendar className="h-3.5 w-3.5" />
                <span>Follow-up due: {new Date(item.followUpDueAt).toLocaleDateString([], { month: 'short', day: 'numeric', year: 'numeric' })}</span>
              </div>
            )}
          </div>
        </div>
      ))}
    </div>
  );
};
