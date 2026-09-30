import React, { useState } from 'react';
import { useParams, Link } from 'react-router-dom';
import { useInterview, useDeleteInterview } from './useInterviews';
import { interviewsApi } from './interviews-api';
import { PrepChecklistCard } from './components/PrepChecklistCard';
import { QuestionsLogCard } from './components/QuestionsLogCard';
import { DebriefCard } from './components/DebriefCard';
import { WarmthBadge } from '@/features/contacts/components/WarmthBadge';
import {
  ArrowLeft,
  Calendar,
  Clock,
  Video,
  Phone,
  Building,
  Download,
  Users,
  CheckCircle2,
  AlertCircle,
  ExternalLink,
  Mail,
  Linkedin,
  Sparkles,
  Award,
  HelpCircle,
} from 'lucide-react';
import { clsx } from 'clsx';

type TabType = 'prep' | 'questions' | 'debrief';

export const InterviewDetailPage: React.FC = () => {
  const { id } = useParams<{ id: string }>();
  const { data: interview, isLoading, error } = useInterview(id);
  const [activeTab, setActiveTab] = useState<TabType>('prep');

  if (isLoading) {
    return (
      <div className="p-8 max-w-5xl mx-auto space-y-6 animate-pulse">
        <div className="h-6 w-32 bg-muted rounded" />
        <div className="h-28 bg-card border border-border rounded-xl" />
        <div className="h-64 bg-card border border-border rounded-xl" />
      </div>
    );
  }

  if (error || !interview) {
    return (
      <div className="p-12 max-w-lg mx-auto text-center space-y-4">
        <AlertCircle className="h-12 w-12 text-destructive mx-auto" />
        <h2 className="text-lg font-bold">Interview Not Found</h2>
        <p className="text-xs text-muted-foreground">
          The requested interview could not be found or you do not have permission to view it.
        </p>
        <Link
          to="/applications"
          className="inline-flex items-center gap-2 px-4 py-2 text-xs font-semibold bg-primary text-primary-foreground rounded-lg"
        >
          <ArrowLeft className="h-4 w-4" />
          Back to Applications
        </Link>
      </div>
    );
  }

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

  const handleDownloadIcs = async () => {
    await interviewsApi.downloadIcs(interview.id);
  };

  return (
    <div className="space-y-6 max-w-6xl mx-auto pb-12">
      {/* Top back navigation */}
      <div className="flex items-center justify-between">
        <Link
          to={`/applications/${interview.applicationId}`}
          className="inline-flex items-center gap-1.5 text-xs font-medium text-muted-foreground hover:text-foreground transition-colors group"
        >
          <ArrowLeft className="h-4 w-4 group-hover:-translate-x-0.5 transition-transform" />
          <span>Back to Application ({interview.roleTitle} at {interview.companyName})</span>
        </Link>
      </div>

      {/* Hero Header Card */}
      <div className="bg-card border border-border rounded-xl p-6 shadow-xs space-y-5">
        <div className="flex flex-col md:flex-row md:items-center justify-between gap-4">
          <div className="space-y-1.5">
            <div className="flex items-center gap-2.5 flex-wrap">
              <span className="px-2.5 py-0.5 rounded-full text-xs font-bold uppercase tracking-wider bg-primary/10 text-primary border border-primary/20">
                {interview.type} Round
              </span>

              <span className="inline-flex items-center gap-1 px-2.5 py-0.5 rounded-full text-xs font-medium bg-muted text-muted-foreground border border-border">
                {interview.format === 'Phone' ? (
                  <Phone className="h-3 w-3" />
                ) : interview.format === 'Onsite' ? (
                  <Building className="h-3 w-3" />
                ) : (
                  <Video className="h-3 w-3" />
                )}
                <span>{interview.format}</span>
              </span>

              <span
                className={clsx(
                  'px-2.5 py-0.5 rounded-full text-xs font-bold font-mono',
                  interview.status === 'Completed'
                    ? 'bg-emerald-500/10 text-emerald-600 dark:text-emerald-400'
                    : 'bg-primary/10 text-primary'
                )}
              >
                {formatCountdown()}
              </span>
            </div>

            <h1 className="text-xl font-bold tracking-tight text-foreground">
              {interview.roleTitle}
            </h1>
            <p className="text-sm font-semibold text-muted-foreground">
              {interview.companyName}
            </p>
          </div>

          {/* Action buttons: Join Meeting & Add to Calendar */}
          <div className="flex items-center gap-2 flex-wrap shrink-0">
            {interview.meetingLink && (
              <a
                href={interview.meetingLink}
                target="_blank"
                rel="noreferrer"
                className="inline-flex items-center gap-1.5 px-3.5 py-2 text-xs font-semibold bg-primary text-primary-foreground rounded-lg hover:opacity-90 transition-opacity shadow-sm"
              >
                <Video className="h-4 w-4" />
                <span>Join Video Meeting</span>
              </a>
            )}

            <button
              type="button"
              onClick={handleDownloadIcs}
              className="inline-flex items-center gap-1.5 px-3.5 py-2 text-xs font-semibold bg-card border border-border hover:bg-muted text-foreground rounded-lg transition-colors shadow-2xs"
            >
              <Download className="h-4 w-4 text-muted-foreground" />
              <span>Add to Calendar (.ics)</span>
            </button>
          </div>
        </div>

        {/* Date, Time, Duration & Location banner */}
        <div className="grid grid-cols-1 sm:grid-cols-3 gap-4 pt-4 border-t border-border text-xs">
          <div>
            <span className="text-muted-foreground block text-[11px]">Scheduled Date & Time</span>
            <span className="font-semibold text-foreground flex items-center gap-1.5 mt-0.5 font-mono">
              <Calendar className="h-3.5 w-3.5 text-muted-foreground" />
              {scheduledDate.toLocaleDateString(undefined, {
                weekday: 'short',
                month: 'short',
                day: 'numeric',
                year: 'numeric',
              })}{' '}
              at{' '}
              {scheduledDate.toLocaleTimeString(undefined, {
                hour: '2-digit',
                minute: '2-digit',
              })}
            </span>
          </div>

          <div>
            <span className="text-muted-foreground block text-[11px]">Duration</span>
            <span className="font-semibold text-foreground flex items-center gap-1.5 mt-0.5 font-mono">
              <Clock className="h-3.5 w-3.5 text-muted-foreground" />
              {interview.durationMinutes} minutes
            </span>
          </div>

          <div>
            <span className="text-muted-foreground block text-[11px]">Meeting Room / Location</span>
            <span className="font-semibold text-foreground truncate block mt-0.5">
              {interview.meetingLink ? (
                <a
                  href={interview.meetingLink}
                  target="_blank"
                  rel="noreferrer"
                  className="text-primary hover:underline truncate"
                >
                  {interview.meetingLink}
                </a>
              ) : (
                interview.location || 'Video Call'
              )}
            </span>
          </div>
        </div>

        {/* Interviewers Panel (if any linked) */}
        {interview.interviewers.length > 0 && (
          <div className="pt-4 border-t border-border">
            <span className="text-[11px] font-bold uppercase tracking-wider text-muted-foreground block mb-2">
              Interviewers / Panel ({interview.interviewers.length})
            </span>
            <div className="grid grid-cols-1 sm:grid-cols-2 md:grid-cols-3 gap-3">
              {interview.interviewers.map((interviewer) => (
                <div
                  key={interviewer.contactId}
                  className="bg-muted/30 border border-border rounded-lg p-2.5 flex items-center justify-between gap-2"
                >
                  <div className="min-w-0">
                    <Link
                      to={`/contacts/${interviewer.contactId}`}
                      className="font-semibold text-xs text-foreground hover:text-primary transition-colors truncate block"
                    >
                      {interviewer.fullName}
                    </Link>
                    <span className="text-[11px] text-muted-foreground truncate block">
                      {interviewer.role || 'Interviewer'}
                    </span>
                  </div>

                  <div className="flex items-center gap-1.5 shrink-0">
                    <WarmthBadge warmth={interviewer.warmth} size="sm" />
                    {interviewer.linkedInUrl && (
                      <a
                        href={interviewer.linkedInUrl}
                        target="_blank"
                        rel="noreferrer"
                        className="text-muted-foreground hover:text-blue-500 transition-colors"
                      >
                        <Linkedin className="h-3.5 w-3.5" />
                      </a>
                    )}
                  </div>
                </div>
              ))}
            </div>
          </div>
        )}
      </div>

      {/* Tab Navigation */}
      <div className="border-b border-border flex items-center gap-6">
        {[
          { key: 'prep', label: 'Prep Checklist & Strategy' },
          { key: 'questions', label: `Questions Log (${interview.questions.length})` },
          { key: 'debrief', label: 'Post-Interview Debrief' },
        ].map((tab) => (
          <button
            key={tab.key}
            type="button"
            onClick={() => setActiveTab(tab.key as TabType)}
            className={clsx(
              'pb-3 text-xs font-semibold transition-colors border-b-2 relative -mb-[1px]',
              activeTab === tab.key
                ? 'border-primary text-primary'
                : 'border-transparent text-muted-foreground hover:text-foreground'
            )}
          >
            {tab.label}
          </button>
        ))}
      </div>

      {/* Tab Content */}
      <div className="space-y-6">
        {/* TAB 1: PREP CHECKLIST */}
        {activeTab === 'prep' && (
          <div className="space-y-6">
            <PrepChecklistCard
              interviewId={interview.id}
              checklist={interview.prepChecklist}
            />

            {interview.prepNotes && (
              <div className="bg-card border border-border rounded-xl p-5 shadow-2xs space-y-2">
                <h3 className="text-xs font-bold uppercase tracking-wider text-muted-foreground flex items-center gap-1.5">
                  <Sparkles className="h-4 w-4 text-primary" />
                  <span>Personal Prep Notes</span>
                </h3>
                <p className="text-xs text-foreground/90 whitespace-pre-wrap leading-relaxed">
                  {interview.prepNotes}
                </p>
              </div>
            )}
          </div>
        )}

        {/* TAB 2: QUESTIONS LOG */}
        {activeTab === 'questions' && (
          <QuestionsLogCard
            interviewId={interview.id}
            questions={interview.questions}
          />
        )}

        {/* TAB 3: DEBRIEF */}
        {activeTab === 'debrief' && (
          <DebriefCard
            interviewId={interview.id}
            applicationId={interview.applicationId}
            roleTitle={interview.roleTitle}
            companyName={interview.companyName}
            initialSelfRating={interview.selfRating}
            initialWentWell={interview.wentWell}
            initialToImprove={interview.toImprove}
            initialThankYouSent={interview.thankYouSent}
            initialOutcomeNotes={interview.outcomeNotes}
          />
        )}
      </div>
    </div>
  );
};
