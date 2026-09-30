import React, { useState, useEffect } from 'react';
import { X, UserCheck, Save, Mail, Phone, Building2, ShieldCheck, FileText } from 'lucide-react';
import { ReferenceListItem, ReferenceDetail, ReferenceConsent, CreateReferencePayload } from './types';
import { useCreateReference, useUpdateReference } from './useReferences';

interface ReferenceModalProps {
  isOpen: boolean;
  onClose: () => void;
  referenceToEdit?: ReferenceListItem | ReferenceDetail | null;
}

export const ReferenceModal: React.FC<ReferenceModalProps> = ({
  isOpen,
  onClose,
  referenceToEdit,
}) => {
  const [fullName, setFullName] = useState('');
  const [relationship, setRelationship] = useState('');
  const [company, setCompany] = useState('');
  const [email, setEmail] = useState('');
  const [phone, setPhone] = useState('');
  const [consent, setConsent] = useState<ReferenceConsent>('NotAsked');
  const [notes, setNotes] = useState('');

  const createMutation = useCreateReference();
  const updateMutation = useUpdateReference();

  useEffect(() => {
    if (referenceToEdit) {
      setFullName(referenceToEdit.fullName);
      setRelationship(referenceToEdit.relationship);
      setCompany(referenceToEdit.company || '');
      setEmail(referenceToEdit.email || '');
      setPhone(referenceToEdit.phone || '');
      setConsent(referenceToEdit.consent);
      setNotes(referenceToEdit.notes || '');
    } else {
      setFullName('');
      setRelationship('');
      setCompany('');
      setEmail('');
      setPhone('');
      setConsent('NotAsked');
      setNotes('');
    }
  }, [referenceToEdit, isOpen]);

  useEffect(() => {
    const handleKeyDown = (e: KeyboardEvent) => {
      if (e.key === 'Escape' && isOpen) onClose();
    };
    window.addEventListener('keydown', handleKeyDown);
    return () => window.removeEventListener('keydown', handleKeyDown);
  }, [isOpen, onClose]);

  if (!isOpen) return null;

  const handleSubmit = async (e: React.FormEvent) => {
    e.preventDefault();
    if (!fullName.trim() || !relationship.trim()) return;

    const payload: CreateReferencePayload = {
      fullName: fullName.trim(),
      relationship: relationship.trim(),
      company: company.trim() || null,
      email: email.trim() || null,
      phone: phone.trim() || null,
      consent,
      notes: notes.trim() || null,
    };

    if (referenceToEdit) {
      await updateMutation.mutateAsync({ id: referenceToEdit.id, payload });
    } else {
      await createMutation.mutateAsync(payload);
    }

    onClose();
  };

  return (
    <div
      role="dialog"
      aria-modal="true"
      aria-labelledby="reference-modal-title"
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
              <UserCheck className="h-5 w-5" />
            </div>
            <div>
              <h2 id="reference-modal-title" className="text-base font-bold tracking-tight">
                {referenceToEdit ? 'Edit Reference' : 'Add Professional Reference'}
              </h2>
              <p className="text-xs text-muted-foreground">
                Manage contact details, relationship, and consent permissions
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
              <label className="text-xs font-semibold text-muted-foreground">
                Full Name <span className="text-destructive">*</span>
              </label>
              <input
                type="text"
                required
                value={fullName}
                onChange={(e) => setFullName(e.target.value)}
                className="w-full px-3 py-2 text-sm bg-background border border-border rounded-lg focus:outline-none focus:ring-2 focus:ring-primary/20 font-medium"
                placeholder="e.g., Sarah Connor"
              />
            </div>

            <div className="space-y-1.5">
              <label className="text-xs font-semibold text-muted-foreground">
                Relationship <span className="text-destructive">*</span>
              </label>
              <input
                type="text"
                required
                value={relationship}
                onChange={(e) => setRelationship(e.target.value)}
                className="w-full px-3 py-2 text-sm bg-background border border-border rounded-lg focus:outline-none focus:ring-2 focus:ring-primary/20"
                placeholder="e.g., Former Manager, Tech Lead"
              />
            </div>
          </div>

          <div className="grid grid-cols-1 sm:grid-cols-2 gap-4">
            <div className="space-y-1.5">
              <label className="text-xs font-semibold text-muted-foreground flex items-center gap-1.5">
                <Building2 className="h-3.5 w-3.5" />
                <span>Company</span>
              </label>
              <input
                type="text"
                value={company}
                onChange={(e) => setCompany(e.target.value)}
                className="w-full px-3 py-2 text-sm bg-background border border-border rounded-lg focus:outline-none focus:ring-2 focus:ring-primary/20"
                placeholder="e.g., Cyberdyne Systems"
              />
            </div>

            <div className="space-y-1.5">
              <label className="text-xs font-semibold text-muted-foreground flex items-center gap-1.5">
                <ShieldCheck className="h-3.5 w-3.5" />
                <span>Consent Status</span>
              </label>
              <select
                value={consent}
                onChange={(e) => setConsent(e.target.value as ReferenceConsent)}
                className="w-full px-3 py-2 text-sm bg-background border border-border rounded-lg focus:outline-none focus:ring-2 focus:ring-primary/20"
              >
                <option value="NotAsked">Not Asked (Pending inquiry)</option>
                <option value="Asked">Asked (Awaiting confirmation)</option>
                <option value="Agreed">Agreed (Permission secured)</option>
                <option value="Declined">Declined (Do not contact)</option>
              </select>
            </div>
          </div>

          <div className="grid grid-cols-1 sm:grid-cols-2 gap-4">
            <div className="space-y-1.5">
              <label className="text-xs font-semibold text-muted-foreground flex items-center gap-1.5">
                <Mail className="h-3.5 w-3.5" />
                <span>Email Address</span>
              </label>
              <input
                type="email"
                value={email}
                onChange={(e) => setEmail(e.target.value)}
                className="w-full px-3 py-2 text-sm bg-background border border-border rounded-lg focus:outline-none focus:ring-2 focus:ring-primary/20"
                placeholder="sarah@example.com"
              />
            </div>

            <div className="space-y-1.5">
              <label className="text-xs font-semibold text-muted-foreground flex items-center gap-1.5">
                <Phone className="h-3.5 w-3.5" />
                <span>Phone</span>
              </label>
              <input
                type="tel"
                value={phone}
                onChange={(e) => setPhone(e.target.value)}
                className="w-full px-3 py-2 text-sm bg-background border border-border rounded-lg focus:outline-none focus:ring-2 focus:ring-primary/20"
                placeholder="+1 555-0199"
              />
            </div>
          </div>

          <div className="space-y-1.5">
            <label className="text-xs font-semibold text-muted-foreground flex items-center gap-1.5">
              <FileText className="h-3.5 w-3.5" />
              <span>Notes & Talking Points</span>
            </label>
            <textarea
              rows={3}
              value={notes}
              onChange={(e) => setNotes(e.target.value)}
              className="w-full p-3 text-sm bg-background border border-border rounded-lg focus:outline-none focus:ring-2 focus:ring-primary/20 leading-relaxed resize-none"
              placeholder="Projects worked on together, specific strengths they can speak to..."
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
              disabled={createMutation.isPending || updateMutation.isPending}
              className="inline-flex items-center gap-2 px-4 py-2 bg-primary text-primary-foreground text-xs font-bold rounded-lg hover:bg-primary/90 transition-colors shadow-sm"
            >
              <Save className="h-4 w-4" />
              <span>{referenceToEdit ? 'Save Changes' : 'Add Reference'}</span>
            </button>
          </div>
        </form>
      </div>
    </div>
  );
};
