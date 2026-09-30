import React, { useState } from 'react';
import { UpdateDebriefPayload } from '../types';
import { useUpdateDebrief } from '../useInterviews';
import {
  Award,
  Star,
  ThumbsUp,
  ThumbsDown,
  MailCheck,
  Check,
  Copy,
  Sparkles,
  FileCheck,
  Send,
} from 'lucide-react';
import { clsx } from 'clsx';
import { EmailTemplatePickerModal } from '@/features/templates/EmailTemplatePickerModal';

interface DebriefCardProps {
  interviewId: string;
  applicationId?: string;
  roleTitle: string;
  companyName: string;
  initialSelfRating?: number | null;
  initialWentWell?: string | null;
  initialToImprove?: string | null;
  initialThankYouSent: boolean;
  initialOutcomeNotes?: string | null;
}

export const DebriefCard: React.FC<DebriefCardProps> = ({
  interviewId,
  applicationId,
  roleTitle,
  companyName,
  initialSelfRating,
  initialWentWell,
  initialToImprove,
  initialThankYouSent,
  initialOutcomeNotes,
}) => {
  const [selfRating, setSelfRating] = useState<number | null>(initialSelfRating ?? null);
  const [wentWell, setWentWell] = useState(initialWentWell || '');
  const [toImprove, setToImprove] = useState(initialToImprove || '');
  const [thankYouSent, setThankYouSent] = useState(initialThankYouSent);
  const [outcomeNotes, setOutcomeNotes] = useState(initialOutcomeNotes || '');
  const [copiedTemplate, setCopiedTemplate] = useState(false);
  const [saveSuccess, setSaveSuccess] = useState(false);
  const [isPickerOpen, setIsPickerOpen] = useState(false);

  const updateMutation = useUpdateDebrief();

  const handleRatingSelect = (rating: number) => {
    setSelfRating(rating);
  };

  const getRatingLabel = (rating: number | null) => {
    switch (rating) {
      case 5:
        return 'Exceptional — Clear hire signal, aced core questions';
      case 4:
        return 'Strong — Demonstrated expertise, good rapport';
      case 3:
        return 'Satisfactory — Competent, but room for improvement';
      case 2:
        return 'Struggled — Difficulty on key topics or trade-offs';
      case 1:
        return 'Tough Round — Substantial knowledge gaps';
      default:
        return 'Select a rating to assess your overall performance';
    }
  };

  const thankYouTemplateText = `Hi [Interviewer Name],

Thank you so much for taking the time to speak with me today about the ${roleTitle} role at ${companyName}. I really enjoyed our discussion, particularly learning more about [mention 1 specific topic discussed].

Our conversation reaffirmed my excitement about joining the team and contributing to [key company initiative or challenge].

Please let me know if you need any additional information from my side. Looking forward to hearing about next steps!

Best regards,
[My Name]`;

  const handleCopyTemplate = async () => {
    try {
      await navigator.clipboard.writeText(thankYouTemplateText);
      setCopiedTemplate(true);
      setTimeout(() => setCopiedTemplate(false), 2000);
    } catch {
      // fallback
    }
  };

  const handleSubmit = async (e: React.FormEvent) => {
    e.preventDefault();

    const payload: UpdateDebriefPayload = {
      selfRating,
      wentWell: wentWell.trim() || undefined,
      toImprove: toImprove.trim() || undefined,
      thankYouSent,
      outcomeNotes: outcomeNotes.trim() || undefined,
    };

    await updateMutation.mutateAsync({ id: interviewId, payload });
    setSaveSuccess(true);
    setTimeout(() => setSaveSuccess(false), 3000);
  };

  return (
    <form onSubmit={handleSubmit} className="bg-card border border-border rounded-xl p-5 shadow-2xs space-y-6">
      {/* Header */}
      <div className="flex items-center justify-between border-b border-border pb-4">
        <div>
          <h3 className="font-bold text-sm text-foreground flex items-center gap-2">
            <Award className="h-4 w-4 text-amber-500" />
            <span>Post-Interview Debrief & Self-Assessment</span>
          </h3>
          <p className="text-xs text-muted-foreground mt-0.5">
            Capture fresh reflections immediately after the interview while memory is highest.
          </p>
        </div>

        {saveSuccess && (
          <span className="text-xs font-semibold text-emerald-600 dark:text-emerald-400 flex items-center gap-1 bg-emerald-500/10 px-2.5 py-1 rounded-md">
            <Check className="h-3.5 w-3.5" />
            Debrief Saved!
          </span>
        )}
      </div>

      {/* 1. Self Rating */}
      <div className="space-y-2">
        <label className="block text-xs font-semibold uppercase tracking-wider text-muted-foreground">
          Overall Self-Rating
        </label>
        <div className="flex items-center gap-2">
          {[1, 2, 3, 4, 5].map((star) => (
            <button
              key={star}
              type="button"
              onClick={() => handleRatingSelect(star)}
              className="p-1.5 hover:scale-110 transition-transform"
            >
              <Star
                className={clsx(
                  'h-6 w-6 transition-colors',
                  selfRating !== null && star <= selfRating
                    ? 'fill-amber-400 text-amber-400'
                    : 'text-muted-foreground/30 hover:text-amber-400/60'
                )}
              />
            </button>
          ))}
          <span className="text-xs font-medium text-foreground ml-2">
            {getRatingLabel(selfRating)}
          </span>
        </div>
      </div>

      {/* 2. What Went Well & To Improve */}
      <div className="grid grid-cols-1 md:grid-cols-2 gap-4">
        <div className="space-y-1.5">
          <label className="text-xs font-semibold uppercase tracking-wider text-emerald-600 dark:text-emerald-400 flex items-center gap-1.5">
            <ThumbsUp className="h-3.5 w-3.5" />
            <span>What Went Well</span>
          </label>
          <textarea
            rows={3}
            placeholder="Strong talking points, questions that landed well, positive rapport with the panel..."
            value={wentWell}
            onChange={(e) => setWentWell(e.target.value)}
            className="w-full px-3 py-2 text-xs bg-background border border-border rounded-lg focus:outline-none focus:ring-2 focus:ring-primary resize-none"
          />
        </div>

        <div className="space-y-1.5">
          <label className="text-xs font-semibold uppercase tracking-wider text-rose-600 dark:text-rose-400 flex items-center gap-1.5">
            <ThumbsDown className="h-3.5 w-3.5" />
            <span>Areas to Improve & Weak Spots</span>
          </label>
          <textarea
            rows={3}
            placeholder="Questions where you hesitated, architectural trade-offs you missed, topics to study..."
            value={toImprove}
            onChange={(e) => setToImprove(e.target.value)}
            className="w-full px-3 py-2 text-xs bg-background border border-border rounded-lg focus:outline-none focus:ring-2 focus:ring-primary resize-none"
          />
        </div>
      </div>

      {/* 3. Thank-you note tracker & quick copy */}
      <div className="bg-muted/30 border border-border rounded-xl p-4 space-y-3">
        <div className="flex flex-col sm:flex-row sm:items-center justify-between gap-3">
          <label className="flex items-center gap-2.5 cursor-pointer">
            <input
              type="checkbox"
              checked={thankYouSent}
              onChange={(e) => setThankYouSent(e.target.checked)}
              className="rounded border-border text-primary focus:ring-primary h-4 w-4"
            />
            <span className="text-xs font-semibold text-foreground flex items-center gap-1.5">
              <MailCheck className="h-4 w-4 text-primary" />
              <span>Thank-you email sent to interviewers</span>
            </span>
          </label>

          <div className="flex items-center gap-2">
            <button
              type="button"
              onClick={() => setIsPickerOpen(true)}
              className="inline-flex items-center gap-1.5 px-3 py-1.5 text-xs font-semibold bg-primary/10 border border-primary/20 hover:bg-primary/20 text-primary rounded-lg transition-colors self-start sm:self-auto shadow-2xs"
            >
              <Send className="h-3.5 w-3.5" />
              <span>Customize & Send Template</span>
            </button>

            <button
              type="button"
              onClick={handleCopyTemplate}
              className="inline-flex items-center gap-1.5 px-3 py-1.5 text-xs font-semibold bg-card border border-border hover:bg-muted text-foreground rounded-lg transition-colors self-start sm:self-auto shadow-2xs"
            >
              {copiedTemplate ? (
                <>
                  <Check className="h-3.5 w-3.5 text-emerald-500" />
                  <span>Template Copied!</span>
                </>
              ) : (
                <>
                  <Copy className="h-3.5 w-3.5 text-muted-foreground" />
                  <span>Copy Thank-You Email Template</span>
                </>
              )}
            </button>
          </div>
        </div>
        <p className="text-[11px] text-muted-foreground">
          Tip: Sending a tailored thank-you within 24 hours reinforces your interest and gives an opportunity to clarify points discussed during the interview.
        </p>
      </div>

      {/* 4. Outcome Notes */}
      <div className="space-y-1.5">
        <label className="block text-xs font-semibold uppercase tracking-wider text-muted-foreground">
          Outcome & Follow-up Notes
        </label>
        <textarea
          rows={2}
          placeholder="Expected turnaround time, next steps promised by recruiter, or verbal feedback received..."
          value={outcomeNotes}
          onChange={(e) => setOutcomeNotes(e.target.value)}
          className="w-full px-3 py-2 text-xs bg-background border border-border rounded-lg focus:outline-none focus:ring-2 focus:ring-primary resize-none"
        />
      </div>

      {/* Submit Button */}
      <div className="flex items-center justify-end pt-2 border-t border-border">
        <button
          type="submit"
          disabled={updateMutation.isPending}
          className="inline-flex items-center gap-1.5 px-4 py-2 text-xs font-semibold bg-primary text-primary-foreground rounded-lg hover:opacity-90 shadow-sm transition-opacity disabled:opacity-50"
        >
          <FileCheck className="h-4 w-4" />
          <span>{updateMutation.isPending ? 'Saving Debrief...' : 'Save Debrief'}</span>
        </button>
      </div>

      {/* Email Template Picker Modal */}
      <EmailTemplatePickerModal
        isOpen={isPickerOpen}
        onClose={() => setIsPickerOpen(false)}
        applicationId={applicationId}
        defaultCategory="ThankYou"
      />
    </form>
  );
};
