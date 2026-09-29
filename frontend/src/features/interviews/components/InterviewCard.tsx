import React from 'react';
import { Link } from 'react-router-dom';
import { InterviewListItem } from '../types';
import { interviewsApi } from '../interviews-api';
import {
  Calendar,
  Clock,
  Video,
  Phone,
  Building,
  ExternalLink,
  Download,
  Users,
  HelpCircle,
  Star,
  CheckCircle2,
  XCircle,
  AlertCircle,
} from 'lucide-react';
import { clsx } from 'clsx';

interface InterviewCardProps {
  interview: InterviewListItem;
}

export const InterviewCard: React.FC<InterviewCardProps> = ({ interview }) => {
  const scheduledDate = new Date(interview.scheduledAt);
  const now = new Date();
  const diffDays = Math.ceil((scheduledDate.getTime() - now.getTime()) / (1000 * 60 * 60 * 24));

  const formatCountdown = () => {
    if (interview.status === 'Completed') return 'Completed';
    if (interview.status === 'Cancelled') return 'Cancelled';
    if (interview.status === 'NoShow') return 'Missed';

    if (diffDays === 0) return 'Today';
    if (diffDays === 1) return 'Tomorrow';
    if (diffDays > 1 && diffDays <= 7) return `In ${diffDays} days`;
    if (diffDays > 7) return scheduledDate.toLocaleDateString(undefined, { month: 'short', day: 'numeric' });
    if (diffDays === -1) return 'Yesterday';
    return `${Math.abs(diffDays)}d ago`;
  };

  const getFormatIcon = () => {
    switch (interview.format) {
      case 'Phone':
        return Phone;
      case 'Onsite':
        return Building;
      case 'Video':
      default:
        return Video;
    }
  };

  const getTypeStyle = () => {
    switch (interview.type) {
      case 'Technical':
        return 'bg-blue-500/10 text-blue-600 dark:text-blue-400 border-blue-500/20';
      case 'Culture':
        return 'bg-emerald-500/10 text-emerald-600 dark:text-emerald-400 border-emerald-500/20';
      case 'Manager':
        return 'bg-purple-500/10 text-purple-600 dark:text-purple-400 border-purple-500/20';
      case 'Final':
        return 'bg-amber-500/10 text-amber-600 dark:text-amber-400 border-amber-500/20';
      case 'HR':
        return 'bg-sky-500/10 text-sky-600 dark:text-sky-400 border-sky-500/20';
      default:
        return 'bg-muted text-muted-foreground border-border';
    }
  };

  const FormatIcon = getFormatIcon();

  const handleDownloadIcs = async (e: React.MouseEvent) => {
    e.preventDefault();
    e.stopPropagation();
    await interviewsApi.downloadIcs(interview.id);
  };

  return (
    <div className="bg-card border border-border rounded-xl p-5 shadow-2xs hover:border-primary/40 transition-all flex flex-col justify-between space-y-4 group">
      {/* Header: Date countdown & badges */}
      <div className="flex items-start justify-between gap-3">
        <div className="flex items-center gap-2 flex-wrap">
          <span
            className={clsx(
              'px-2.5 py-0.5 rounded-full text-[11px] font-bold border uppercase tracking-wider',
              getTypeStyle()
            )}
          >
            {interview.type}
          </span>

          <span className="inline-flex items-center gap-1 px-2 py-0.5 rounded-md text-[11px] font-medium bg-muted text-muted-foreground border border-border">
            <FormatIcon className="h-3 w-3" />
            <span>{interview.format}</span>
          </span>

          <span className="inline-flex items-center gap-1 text-[11px] font-mono text-muted-foreground">
            <Clock className="h-3 w-3" />
            {interview.durationMinutes}m
          </span>
        </div>

        <span
          className={clsx(
            'px-2 py-0.5 rounded-full text-[11px] font-bold font-mono shrink-0',
            interview.status === 'Completed'
              ? 'bg-emerald-500/10 text-emerald-600 dark:text-emerald-400'
              : interview.status === 'Cancelled'
              ? 'bg-destructive/10 text-destructive'
              : diffDays === 0
              ? 'bg-rose-500/10 text-rose-600 dark:text-rose-400 animate-pulse'
              : 'bg-primary/10 text-primary'
          )}
        >
          {formatCountdown()}
        </span>
      </div>

      {/* Main Info */}
      <div>
        <Link
          to={`/interviews/${interview.id}`}
          className="font-bold text-base text-foreground hover:text-primary transition-colors block group-hover:text-primary"
        >
          {interview.roleTitle}
        </Link>
        <p className="text-xs text-muted-foreground font-semibold mt-0.5">
          {interview.companyName}
        </p>

        <div className="flex items-center gap-4 text-xs text-muted-foreground mt-3">
          <span className="flex items-center gap-1 font-mono">
            <Calendar className="h-3.5 w-3.5 text-muted-foreground" />
            {scheduledDate.toLocaleDateString(undefined, {
              weekday: 'short',
              month: 'short',
              day: 'numeric',
            })}{' '}
            at{' '}
            {scheduledDate.toLocaleTimeString(undefined, {
              hour: '2-digit',
              minute: '2-digit',
            })}
          </span>

          {interview.interviewersCount > 0 && (
            <span className="flex items-center gap-1">
              <Users className="h-3.5 w-3.5" />
              <span>{interview.interviewersCount} Interviewer{interview.interviewersCount > 1 ? 's' : ''}</span>
            </span>
          )}

          {interview.questionsCount > 0 && (
            <span className="flex items-center gap-1">
              <HelpCircle className="h-3.5 w-3.5" />
              <span>{interview.questionsCount} Qs</span>
            </span>
          )}

          {interview.selfRating && (
            <span className="flex items-center gap-1 font-semibold text-amber-500">
              <Star className="h-3.5 w-3.5 fill-amber-400 text-amber-400" />
              <span>{interview.selfRating}/5</span>
            </span>
          )}
        </div>
      </div>

      {/* Bottom actions */}
      <div className="flex items-center justify-between pt-3 border-t border-border/60">
        <div className="flex items-center gap-2">
          {interview.meetingLink && (
            <a
              href={interview.meetingLink}
              target="_blank"
              rel="noreferrer"
              className="inline-flex items-center gap-1 px-2.5 py-1 text-xs font-semibold bg-primary/10 text-primary hover:bg-primary/20 rounded-md transition-colors"
              title="Open Meeting Room"
            >
              <Video className="h-3 w-3" />
              <span>Join</span>
            </a>
          )}

          <button
            type="button"
            onClick={handleDownloadIcs}
            className="inline-flex items-center gap-1 px-2.5 py-1 text-xs font-medium text-muted-foreground hover:text-foreground hover:bg-muted rounded-md transition-colors"
            title="Download .ics Calendar Invite"
          >
            <Download className="h-3 w-3" />
            <span>.ics</span>
          </button>
        </div>

        <Link
          to={`/interviews/${interview.id}`}
          className="inline-flex items-center gap-1 text-xs font-semibold text-primary hover:underline"
        >
          <span>Prep & Debrief</span>
          <ExternalLink className="h-3 w-3" />
        </Link>
      </div>
    </div>
  );
};
