import React, { useState, useEffect } from 'react';
import { X, Award, DollarSign, Calendar, FileText, CheckCircle2 } from 'lucide-react';
import { useUpdateOfferDetails } from './useOffers';

interface EditOfferModalProps {
  isOpen: boolean;
  onClose: () => void;
  applicationId: string;
  roleTitle: string;
  companyName: string;
  currency: string;
  initialSalary?: number | null;
  initialBonus?: number | null;
  initialBenefits?: string | null;
  initialDeadline?: string | null;
  initialNotes?: string | null;
}

export const EditOfferModal: React.FC<EditOfferModalProps> = ({
  isOpen,
  onClose,
  applicationId,
  roleTitle,
  companyName,
  currency,
  initialSalary,
  initialBonus,
  initialBenefits,
  initialDeadline,
  initialNotes,
}) => {
  const [salary, setSalary] = useState<string>('');
  const [bonus, setBonus] = useState<string>('');
  const [benefits, setBenefits] = useState('');
  const [deadline, setDeadline] = useState('');
  const [notes, setNotes] = useState('');

  const updateMutation = useUpdateOfferDetails();

  useEffect(() => {
    setSalary(initialSalary != null ? String(initialSalary) : '');
    setBonus(initialBonus != null ? String(initialBonus) : '');
    setBenefits(initialBenefits || '');
    setDeadline(initialDeadline ? initialDeadline.substring(0, 10) : '');
    setNotes(initialNotes || '');
  }, [isOpen, initialSalary, initialBonus, initialBenefits, initialDeadline, initialNotes]);

  if (!isOpen) return null;

  const handleSubmit = async (e: React.FormEvent) => {
    e.preventDefault();

    await updateMutation.mutateAsync({
      applicationId,
      payload: {
        offerSalary: salary ? Number(salary) : null,
        offerBonus: bonus ? Number(bonus) : null,
        offerBenefits: benefits.trim() || null,
        offerDeadline: deadline ? new Date(`${deadline}T12:00:00Z`).toISOString() : null,
        offerNegotiationNotes: notes.trim() || null,
      },
    });

    onClose();
  };

  return (
    <div
      role="dialog"
      aria-modal="true"
      aria-labelledby="edit-offer-modal-title"
      className="fixed inset-0 z-50 flex items-center justify-center p-4 bg-black/60 backdrop-blur-sm animate-in fade-in duration-200"
      onClick={onClose}
    >
      <div
        className="relative w-full max-w-lg bg-card border border-border rounded-xl shadow-2xl overflow-hidden flex flex-col max-h-[90vh]"
        onClick={(e) => e.stopPropagation()}
      >
        <div className="flex items-center justify-between px-6 py-4 border-b border-border bg-muted/30">
          <div className="flex items-center gap-2.5">
            <div className="p-2 rounded-lg bg-amber-500/10 text-amber-500">
              <Award className="h-5 w-5" />
            </div>
            <div>
              <h2 id="edit-offer-modal-title" className="text-base font-bold tracking-tight">
                Record Offer Terms
              </h2>
              <p className="text-xs text-muted-foreground">
                {roleTitle} @ {companyName}
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
          <div className="grid grid-cols-1 sm:grid-cols-2 gap-4">
            <div className="space-y-1.5">
              <label className="text-xs font-semibold text-muted-foreground flex items-center gap-1.5">
                <DollarSign className="h-3.5 w-3.5" />
                <span>Base Salary ({currency})</span>
              </label>
              <input
                type="number"
                min="0"
                step="1000"
                value={salary}
                onChange={(e) => setSalary(e.target.value)}
                className="w-full px-3 py-2 text-sm bg-background border border-border rounded-lg focus:outline-none focus:ring-2 focus:ring-primary/20 font-mono"
                placeholder="e.g. 185000"
              />
            </div>

            <div className="space-y-1.5">
              <label className="text-xs font-semibold text-muted-foreground flex items-center gap-1.5">
                <DollarSign className="h-3.5 w-3.5" />
                <span>Bonus / Sign-on ({currency})</span>
              </label>
              <input
                type="number"
                min="0"
                step="1000"
                value={bonus}
                onChange={(e) => setBonus(e.target.value)}
                className="w-full px-3 py-2 text-sm bg-background border border-border rounded-lg focus:outline-none focus:ring-2 focus:ring-primary/20 font-mono"
                placeholder="e.g. 25000"
              />
            </div>
          </div>

          <div className="space-y-1.5">
            <label className="text-xs font-semibold text-muted-foreground flex items-center gap-1.5">
              <Calendar className="h-3.5 w-3.5" />
              <span>Offer Decision Deadline</span>
            </label>
            <input
              type="date"
              value={deadline}
              onChange={(e) => setDeadline(e.target.value)}
              className="w-full px-3 py-2 text-sm bg-background border border-border rounded-lg focus:outline-none focus:ring-2 focus:ring-primary/20"
            />
          </div>

          <div className="space-y-1.5">
            <label className="text-xs font-semibold text-muted-foreground">Benefits & Perks Package</label>
            <textarea
              rows={3}
              value={benefits}
              onChange={(e) => setBenefits(e.target.value)}
              className="w-full p-3 text-sm bg-background border border-border rounded-lg focus:outline-none focus:ring-2 focus:ring-primary/20 resize-none"
              placeholder="Health coverage, 401k match, remote stipends, annual leave policy..."
            />
          </div>

          <div className="space-y-1.5">
            <label className="text-xs font-semibold text-muted-foreground flex items-center gap-1.5">
              <FileText className="h-3.5 w-3.5" />
              <span>Negotiation Strategy & Notes</span>
            </label>
            <textarea
              rows={3}
              value={notes}
              onChange={(e) => setNotes(e.target.value)}
              className="w-full p-3 text-sm bg-background border border-border rounded-lg focus:outline-none focus:ring-2 focus:ring-primary/20 resize-none"
              placeholder="Target counter-offer, leverage points from other processes, pending questions..."
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
              disabled={updateMutation.isPending}
              className="inline-flex items-center gap-2 px-4 py-2 bg-primary text-primary-foreground text-xs font-bold rounded-lg hover:bg-primary/90 transition-colors shadow-sm"
            >
              <CheckCircle2 className="h-4 w-4" />
              <span>Save Offer Details</span>
            </button>
          </div>
        </form>
      </div>
    </div>
  );
};
