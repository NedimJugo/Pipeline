import React, { useState } from 'react';
import { LogInteractionPayload, InteractionChannel, InteractionDirection } from '../types';
import { useLogInteraction } from '../useContacts';
import { useApplications } from '@/features/applications/useApplications';
import { X, MessageSquare, ArrowUpRight, ArrowDownLeft, Calendar, FileText } from 'lucide-react';
import { clsx } from 'clsx';

interface LogInteractionModalProps {
  isOpen: boolean;
  onClose: () => void;
  contactId?: string;
  contactName?: string;
  preselectedApplicationId?: string;
}

export const LogInteractionModal: React.FC<LogInteractionModalProps> = ({
  isOpen,
  onClose,
  contactId,
  contactName,
  preselectedApplicationId,
}) => {
  const [channel, setChannel] = useState<InteractionChannel>('Email');
  const [direction, setDirection] = useState<InteractionDirection>('Outbound');
  const [occurredAt, setOccurredAt] = useState<string>(
    new Date().toISOString().slice(0, 16)
  );
  const [applicationId, setApplicationId] = useState<string>(
    preselectedApplicationId || ''
  );
  const [summary, setSummary] = useState('');
  const [sentContent, setSentContent] = useState('');
  const [followUpRequired, setFollowUpRequired] = useState(false);
  const [followUpDueAt, setFollowUpDueAt] = useState('');

  const logMutation = useLogInteraction();
  const { data: applications } = useApplications();

  if (!isOpen) return null;

  const handleSubmit = async (e: React.FormEvent) => {
    e.preventDefault();
    if (!summary.trim()) return;

    const payload: LogInteractionPayload = {
      contactId: contactId || undefined,
      applicationId: applicationId || undefined,
      channel,
      direction,
      occurredAt: occurredAt ? new Date(occurredAt).toISOString() : undefined,
      summary: summary.trim(),
      sentContent: sentContent.trim() || undefined,
      followUpRequired,
      followUpDueAt: followUpRequired && followUpDueAt ? new Date(followUpDueAt).toISOString() : undefined,
    };

    await logMutation.mutateAsync(payload);
    onClose();
  };

  return (
    <div className="fixed inset-0 z-50 flex items-center justify-center p-4 bg-background/80 backdrop-blur-sm animate-in fade-in duration-200">
      <div className="border border-border bg-card max-w-lg w-full rounded-xl shadow-2xl overflow-hidden flex flex-col max-h-[90vh]">
        {/* Header */}
        <div className="px-6 py-4 border-b border-border flex items-center justify-between">
          <div className="flex items-center gap-2">
            <MessageSquare className="h-5 w-5 text-primary" />
            <div>
              <h2 className="font-bold text-base tracking-tight text-foreground">
                Log Interaction
              </h2>
              {contactName && (
                <p className="text-xs text-muted-foreground">
                  With <span className="font-semibold text-foreground">{contactName}</span>
                </p>
              )}
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
          {/* Direction toggle */}
          <div>
            <label className="block text-xs font-semibold uppercase tracking-wider text-muted-foreground mb-1.5">
              Direction
            </label>
            <div className="grid grid-cols-2 gap-2">
              <button
                type="button"
                onClick={() => setDirection('Outbound')}
                className={clsx(
                  'flex items-center justify-center gap-2 py-1.5 px-3 rounded-lg text-xs font-semibold border transition-all',
                  direction === 'Outbound'
                    ? 'bg-primary text-primary-foreground border-primary shadow-2xs'
                    : 'bg-muted/40 text-muted-foreground border-border hover:bg-muted'
                )}
              >
                <ArrowUpRight className="h-3.5 w-3.5" />
                <span>Outbound (I contacted them)</span>
              </button>

              <button
                type="button"
                onClick={() => setDirection('Inbound')}
                className={clsx(
                  'flex items-center justify-center gap-2 py-1.5 px-3 rounded-lg text-xs font-semibold border transition-all',
                  direction === 'Inbound'
                    ? 'bg-primary text-primary-foreground border-primary shadow-2xs'
                    : 'bg-muted/40 text-muted-foreground border-border hover:bg-muted'
                )}
              >
                <ArrowDownLeft className="h-3.5 w-3.5" />
                <span>Inbound (They contacted me)</span>
              </button>
            </div>
          </div>

          <div className="grid grid-cols-1 sm:grid-cols-2 gap-4">
            <div>
              <label className="block text-xs font-semibold uppercase tracking-wider text-muted-foreground mb-1">
                Channel
              </label>
              <select
                value={channel}
                onChange={(e) => setChannel(e.target.value as InteractionChannel)}
                className="w-full px-3 py-1.5 text-xs bg-background border border-border rounded-lg focus:outline-none focus:ring-2 focus:ring-primary"
              >
                <option value="Email">Email</option>
                <option value="LinkedIn">LinkedIn</option>
                <option value="Phone">Phone Call</option>
                <option value="Video">Video Call</option>
                <option value="InPerson">In-Person Meeting</option>
                <option value="Message">Direct Message / SMS</option>
                <option value="Other">Other</option>
              </select>
            </div>

            <div>
              <label className="block text-xs font-semibold uppercase tracking-wider text-muted-foreground mb-1">
                Date & Time
              </label>
              <input
                type="datetime-local"
                value={occurredAt}
                onChange={(e) => setOccurredAt(e.target.value)}
                className="w-full px-3 py-1.5 text-xs bg-background border border-border rounded-lg focus:outline-none focus:ring-2 focus:ring-primary font-mono"
              />
            </div>
          </div>

          {/* Linked Application */}
          <div>
            <label className="block text-xs font-semibold uppercase tracking-wider text-muted-foreground mb-1">
              Associated Application (Optional)
            </label>
            <select
              value={applicationId}
              onChange={(e) => setApplicationId(e.target.value)}
              className="w-full px-3 py-1.5 text-xs bg-background border border-border rounded-lg focus:outline-none focus:ring-2 focus:ring-primary"
            >
              <option value="">None / General Interaction</option>
              {applications?.map((app) => (
                <option key={app.id} value={app.id}>
                  {app.companyName} — {app.roleTitle}
                </option>
              ))}
            </select>
          </div>

          {/* Summary */}
          <div>
            <label className="block text-xs font-semibold uppercase tracking-wider text-muted-foreground mb-1">
              Discussion Summary *
            </label>
            <textarea
              rows={3}
              required
              placeholder="What was discussed? Next steps, feedback received, salary mentioned, or screening questions..."
              value={summary}
              onChange={(e) => setSummary(e.target.value)}
              className="w-full px-3 py-1.5 text-xs bg-background border border-border rounded-lg focus:outline-none focus:ring-2 focus:ring-primary resize-none"
            />
          </div>

          {/* Sent Content (Optional) */}
          <div>
            <label className="block text-xs font-semibold uppercase tracking-wider text-muted-foreground mb-1">
              Sent Content / Message Body (Optional)
            </label>
            <textarea
              rows={2}
              placeholder="Paste email or message text sent..."
              value={sentContent}
              onChange={(e) => setSentContent(e.target.value)}
              className="w-full px-3 py-1.5 text-xs bg-background border border-border rounded-lg focus:outline-none focus:ring-2 focus:ring-primary resize-none"
            />
          </div>

          {/* Follow-up Required Toggle */}
          <div className="bg-muted/30 border border-border rounded-lg p-3 space-y-2">
            <label className="flex items-center gap-2 cursor-pointer">
              <input
                type="checkbox"
                checked={followUpRequired}
                onChange={(e) => setFollowUpRequired(e.target.checked)}
                className="rounded border-border text-primary focus:ring-primary h-4 w-4"
              />
              <span className="text-xs font-semibold text-foreground">
                Follow-up required
              </span>
            </label>

            {followUpRequired && (
              <div className="pt-2">
                <label className="block text-[11px] text-muted-foreground mb-1">
                  Follow-up Due Date
                </label>
                <div className="relative">
                  <Calendar className="h-3.5 w-3.5 absolute left-3 top-2.5 text-muted-foreground" />
                  <input
                    type="date"
                    required={followUpRequired}
                    value={followUpDueAt}
                    onChange={(e) => setFollowUpDueAt(e.target.value)}
                    className="w-full pl-9 pr-3 py-1.5 text-xs bg-background border border-border rounded-lg focus:outline-none focus:ring-2 focus:ring-primary font-mono"
                  />
                </div>
              </div>
            )}
          </div>

          {/* Footer Actions */}
          <div className="flex items-center justify-end gap-2 pt-3 border-t border-border">
            <button
              type="button"
              onClick={onClose}
              disabled={logMutation.isPending}
              className="px-3.5 py-1.5 text-xs font-medium text-muted-foreground hover:text-foreground hover:bg-muted rounded-lg transition-colors"
            >
              Cancel
            </button>
            <button
              type="submit"
              disabled={logMutation.isPending}
              className="px-4 py-1.5 text-xs font-semibold bg-primary text-primary-foreground rounded-lg hover:opacity-90 shadow-sm transition-opacity disabled:opacity-50"
            >
              {logMutation.isPending ? 'Logging...' : 'Save Interaction'}
            </button>
          </div>
        </form>
      </div>
    </div>
  );
};
