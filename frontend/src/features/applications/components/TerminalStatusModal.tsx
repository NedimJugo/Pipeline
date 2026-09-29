import React, { useState } from 'react';
import { ApplicationStatus, UpdateStatusPayload } from '../types';
import { X, AlertCircle, Sparkles } from 'lucide-react';

interface TerminalStatusModalProps {
  isOpen: boolean;
  onClose: () => void;
  targetStatus: ApplicationStatus;
  onSubmit: (payload: UpdateStatusPayload) => Promise<void>;
  isSubmitting?: boolean;
}

const REJECTION_STAGES = [
  'Resume Screening',
  'Recruiter Phone Screen',
  'Hiring Manager Screen',
  'Technical Screening',
  'Take-Home Assignment',
  'Live Coding / System Design',
  'Onsite / Final Loop',
  'Reference Check',
  'Offer Stage',
];

const COMMON_REASONS = [
  'Selected another candidate with more experience',
  'Role placed on hold / hiring freeze',
  'Compensation / salary expectations mismatch',
  'Work arrangement / location mismatch',
  'Skills mismatch for current team needs',
  'Ghosted / no feedback provided',
  'Accepted another compelling offer',
  'Did not feel right during interviews',
];

export const TerminalStatusModal: React.FC<TerminalStatusModalProps> = ({
  isOpen,
  onClose,
  targetStatus,
  onSubmit,
  isSubmitting = false,
}) => {
  const [status, setStatus] = useState<ApplicationStatus>(targetStatus);
  const [rejectionStage, setRejectionStage] = useState(REJECTION_STAGES[0]);
  const [closedReason, setClosedReason] = useState(COMMON_REASONS[0]);
  const [customReason, setCustomReason] = useState('');
  const [lessonsLearned, setLessonsLearned] = useState('');
  const [note, setNote] = useState('');

  if (!isOpen) return null;

  const handleSubmit = async (e: React.FormEvent) => {
    e.preventDefault();
    const finalReason = customReason.trim() ? customReason.trim() : closedReason;

    await onSubmit({
      status,
      rejectionStage: status === 'Rejected' || status === 'Withdrawn' ? rejectionStage : undefined,
      closedReason: finalReason,
      lessonsLearned: lessonsLearned.trim() || undefined,
      note: note.trim() || undefined,
    });

    onClose();
  };

  return (
    <div className="fixed inset-0 z-50 flex items-center justify-center p-4 bg-background/80 backdrop-blur-sm animate-in fade-in duration-200">
      <div className="border border-border bg-card max-w-lg w-full rounded-xl shadow-2xl overflow-hidden flex flex-col max-h-[90vh]">
        {/* Header */}
        <div className="px-6 py-4 border-b border-border flex items-center justify-between bg-destructive/5">
          <div className="flex items-center gap-2">
            <AlertCircle className="h-5 w-5 text-destructive" />
            <div>
              <h2 className="font-bold text-sm text-foreground">Mark Application as Closed</h2>
              <p className="text-xs text-muted-foreground">
                Capture lessons and closure details to improve future conversion rates.
              </p>
            </div>
          </div>
          <button
            type="button"
            onClick={onClose}
            className="p-1 rounded-lg text-muted-foreground hover:text-foreground hover:bg-muted"
          >
            <X className="h-4 w-4" />
          </button>
        </div>

        {/* Form Body */}
        <form onSubmit={handleSubmit} className="flex-1 overflow-y-auto p-6 space-y-4">
          <div>
            <label className="block text-xs font-semibold uppercase tracking-wider text-muted-foreground mb-1">
              Closure Status
            </label>
            <select
              value={status}
              onChange={(e) => setStatus(e.target.value as ApplicationStatus)}
              className="w-full px-3 py-1.5 text-xs bg-background border border-border rounded-lg focus:outline-none focus:ring-2 focus:ring-primary"
            >
              <option value="Rejected">Rejected</option>
              <option value="Withdrawn">Withdrawn</option>
              <option value="Ghosted">Ghosted</option>
              <option value="Declined">Declined</option>
            </select>
          </div>

          <div>
            <label className="block text-xs font-semibold uppercase tracking-wider text-muted-foreground mb-1">
              Stage Reached
            </label>
            <select
              value={rejectionStage}
              onChange={(e) => setRejectionStage(e.target.value)}
              className="w-full px-3 py-1.5 text-xs bg-background border border-border rounded-lg focus:outline-none focus:ring-2 focus:ring-primary"
            >
              {REJECTION_STAGES.map((s) => (
                <option key={s} value={s}>
                  {s}
                </option>
              ))}
            </select>
          </div>

          <div>
            <label className="block text-xs font-semibold uppercase tracking-wider text-muted-foreground mb-1">
              Primary Reason
            </label>
            <select
              value={closedReason}
              onChange={(e) => setClosedReason(e.target.value)}
              className="w-full px-3 py-1.5 text-xs bg-background border border-border rounded-lg focus:outline-none focus:ring-2 focus:ring-primary mb-2"
            >
              {COMMON_REASONS.map((r) => (
                <option key={r} value={r}>
                  {r}
                </option>
              ))}
            </select>
            <input
              type="text"
              placeholder="Or enter custom reason..."
              value={customReason}
              onChange={(e) => setCustomReason(e.target.value)}
              className="w-full px-3 py-1.5 text-xs bg-background border border-border rounded-lg focus:outline-none focus:ring-2 focus:ring-primary"
            />
          </div>

          <div>
            <div className="flex items-center gap-1.5 mb-1">
              <Sparkles className="h-3.5 w-3.5 text-primary" />
              <label className="block text-xs font-semibold uppercase tracking-wider text-muted-foreground">
                Lessons Learned / Reflection
              </label>
            </div>
            <textarea
              rows={3}
              placeholder="What could be improved next time? What went well? Any gaps in tech stack or communication?"
              value={lessonsLearned}
              onChange={(e) => setLessonsLearned(e.target.value)}
              className="w-full px-3 py-2 text-xs bg-background border border-border rounded-lg focus:outline-none focus:ring-2 focus:ring-primary resize-none"
            />
          </div>

          <div>
            <label className="block text-xs font-semibold uppercase tracking-wider text-muted-foreground mb-1">
              Timeline Note (Optional)
            </label>
            <input
              type="text"
              placeholder="e.g. Received email rejection from recruiter"
              value={note}
              onChange={(e) => setNote(e.target.value)}
              className="w-full px-3 py-1.5 text-xs bg-background border border-border rounded-lg focus:outline-none focus:ring-2 focus:ring-primary"
            />
          </div>

          {/* Footer Actions */}
          <div className="flex items-center justify-end gap-2 pt-3 border-t border-border">
            <button
              type="button"
              onClick={onClose}
              disabled={isSubmitting}
              className="px-3.5 py-1.5 text-xs font-medium text-muted-foreground hover:text-foreground hover:bg-muted rounded-lg transition-colors"
            >
              Cancel
            </button>
            <button
              type="submit"
              disabled={isSubmitting}
              className="px-4 py-1.5 text-xs font-semibold bg-destructive text-destructive-foreground rounded-lg hover:opacity-90 transition-opacity shadow-sm disabled:opacity-50"
            >
              {isSubmitting ? 'Updating...' : 'Confirm Status Change'}
            </button>
          </div>
        </form>
      </div>
    </div>
  );
};
