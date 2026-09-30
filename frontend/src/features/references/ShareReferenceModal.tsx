import React, { useState, useEffect } from 'react';
import { X, Send, AlertTriangle, Briefcase, CheckCircle2 } from 'lucide-react';
import { ReferenceListItem, ReferenceDetail } from './types';
import { useShareReference } from './useReferences';
import { useApplications } from '@/features/applications/useApplications';

interface ShareReferenceModalProps {
  isOpen: boolean;
  onClose: () => void;
  reference: ReferenceListItem | ReferenceDetail | null;
}

export const ShareReferenceModal: React.FC<ShareReferenceModalProps> = ({
  isOpen,
  onClose,
  reference,
}) => {
  const [selectedApplicationId, setSelectedApplicationId] = useState('');
  const [outcome, setOutcome] = useState('');
  const [overrideConsent, setOverrideConsent] = useState(false);
  const [errorMessage, setErrorMessage] = useState<string | null>(null);

  const { data: applications = [] } = useApplications();
  const shareMutation = useShareReference();

  useEffect(() => {
    setSelectedApplicationId('');
    setOutcome('');
    setOverrideConsent(false);
    setErrorMessage(null);
  }, [isOpen, reference]);

  if (!isOpen || !reference) return null;

  const requiresConsentWarning = reference.consent !== 'Agreed';

  const handleSubmit = async (e: React.FormEvent) => {
    e.preventDefault();
    if (!selectedApplicationId) return;

    setErrorMessage(null);

    const result = await shareMutation.mutateAsync({
      id: reference.id,
      payload: {
        applicationId: selectedApplicationId,
        outcome: outcome.trim() || null,
        overrideConsentWarning: overrideConsent,
      },
    });

    if (result.warningTriggered) {
      setErrorMessage(result.warningMessage || 'Consent must be secured or confirmed before sharing.');
      return;
    }

    onClose();
  };

  return (
    <div
      role="dialog"
      aria-modal="true"
      aria-labelledby="share-reference-modal-title"
      className="fixed inset-0 z-50 flex items-center justify-center p-4 bg-black/60 backdrop-blur-sm animate-in fade-in duration-200"
      onClick={onClose}
    >
      <div
        className="relative w-full max-w-md bg-card border border-border rounded-xl shadow-2xl overflow-hidden flex flex-col max-h-[90vh]"
        onClick={(e) => e.stopPropagation()}
      >
        <div className="flex items-center justify-between px-6 py-4 border-b border-border bg-muted/30">
          <div className="flex items-center gap-2.5">
            <div className="p-2 rounded-lg bg-primary/10 text-primary">
              <Send className="h-5 w-5" />
            </div>
            <div>
              <h2 id="share-reference-modal-title" className="text-base font-bold tracking-tight">
                Share Reference
              </h2>
              <p className="text-xs text-muted-foreground">
                Log where you shared {reference.fullName} with an employer
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

        <form onSubmit={handleSubmit} className="p-6 overflow-y-auto space-y-4 flex-1">
          {requiresConsentWarning && (
            <div className="p-3.5 rounded-lg bg-amber-500/10 border border-amber-500/20 text-amber-500 text-xs space-y-2">
              <div className="flex items-start gap-2">
                <AlertTriangle className="h-4 w-4 shrink-0 mt-0.5" />
                <div>
                  <span className="font-semibold block">Consent Warning</span>
                  <span>
                    Consent for <strong>{reference.fullName}</strong> is currently set to{' '}
                    <strong>{reference.consent}</strong>. It is strongly recommended to receive permission before sharing.
                  </span>
                </div>
              </div>

              <label className="flex items-start gap-2 pt-1 border-t border-amber-500/20 cursor-pointer select-none text-foreground font-medium">
                <input
                  type="checkbox"
                  checked={overrideConsent}
                  onChange={(e) => setOverrideConsent(e.target.checked)}
                  className="rounded border-border mt-0.5 text-primary focus:ring-primary"
                />
                <span>I have secured verbal or offline consent from this reference.</span>
              </label>
            </div>
          )}

          {errorMessage && (
            <div className="p-3 rounded-lg bg-destructive/10 border border-destructive/20 text-destructive text-xs">
              {errorMessage}
            </div>
          )}

          <div className="space-y-1.5">
            <label className="text-xs font-semibold text-muted-foreground flex items-center gap-1.5">
              <Briefcase className="h-3.5 w-3.5" />
              <span>Select Job Application <span className="text-destructive">*</span></span>
            </label>
            <select
              required
              value={selectedApplicationId}
              onChange={(e) => setSelectedApplicationId(e.target.value)}
              className="w-full px-3 py-2 text-sm bg-background border border-border rounded-lg focus:outline-none focus:ring-2 focus:ring-primary/20"
            >
              <option value="">Choose an active application...</option>
              {applications.map((app) => (
                <option key={app.id} value={app.id}>
                  {app.roleTitle} @ {app.companyName} ({app.status})
                </option>
              ))}
            </select>
          </div>

          <div className="space-y-1.5">
            <label className="text-xs font-semibold text-muted-foreground">Notes / Outcome (Optional)</label>
            <textarea
              rows={2}
              value={outcome}
              onChange={(e) => setOutcome(e.target.value)}
              className="w-full p-2.5 text-sm bg-background border border-border rounded-lg focus:outline-none focus:ring-2 focus:ring-primary/20 resize-none"
              placeholder="e.g., Shared via email with hiring manager..."
            />
          </div>

          <div className="flex items-center justify-end gap-3 pt-3 border-t border-border">
            <button
              type="button"
              onClick={onClose}
              className="px-4 py-2 text-xs font-semibold border border-border rounded-lg hover:bg-muted transition-colors"
            >
              Cancel
            </button>
            <button
              type="submit"
              disabled={
                shareMutation.isPending ||
                !selectedApplicationId ||
                (requiresConsentWarning && !overrideConsent)
              }
              className="inline-flex items-center gap-2 px-4 py-2 bg-primary text-primary-foreground text-xs font-bold rounded-lg hover:bg-primary/90 transition-colors shadow-sm disabled:opacity-50"
            >
              <CheckCircle2 className="h-4 w-4" />
              <span>Confirm & Share</span>
            </button>
          </div>
        </form>
      </div>
    </div>
  );
};
