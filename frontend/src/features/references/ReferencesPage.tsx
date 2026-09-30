import React, { useState } from 'react';
import { Plus, Search, Filter, ShieldCheck, CheckCircle2, Clock, HelpCircle, XCircle, Users } from 'lucide-react';
import { useReferences, useRecordNotification } from './useReferences';
import { ReferenceListItem, ReferenceConsent } from './types';
import { ReferenceCard } from './ReferenceCard';
import { ReferenceModal } from './ReferenceModal';
import { ShareReferenceModal } from './ShareReferenceModal';
import { EmailTemplatePickerModal } from '@/features/templates/EmailTemplatePickerModal';

export const ReferencesPage: React.FC = () => {
  const [activeConsent, setActiveConsent] = useState<ReferenceConsent | 'all'>('all');
  const [search, setSearch] = useState('');
  const [isCreateModalOpen, setIsCreateModalOpen] = useState(false);
  const [referenceToEdit, setReferenceToEdit] = useState<ReferenceListItem | null>(null);
  const [referenceToShare, setReferenceToShare] = useState<ReferenceListItem | null>(null);
  const [notifyingReference, setNotifyingReference] = useState<ReferenceListItem | null>(null);

  const { data: rawReferences, isLoading } = useReferences({
    search: search.trim() || undefined,
    consent: activeConsent === 'all' ? undefined : activeConsent,
  });

  const references = Array.isArray(rawReferences) ? rawReferences : [];

  const recordNotificationMutation = useRecordNotification();

  const handleNotify = (ref: ReferenceListItem) => {
    setNotifyingReference(ref);
    recordNotificationMutation.mutate(ref.id);
  };

  const counts = {
    all: references.length,
    Agreed: references.filter((r) => r.consent === 'Agreed').length,
    Asked: references.filter((r) => r.consent === 'Asked').length,
    NotAsked: references.filter((r) => r.consent === 'NotAsked').length,
    Declined: references.filter((r) => r.consent === 'Declined').length,
  };

  return (
    <div className="max-w-6xl mx-auto space-y-8">
      {/* Header Banner */}
      <div className="flex flex-col sm:flex-row sm:items-center justify-between gap-4 pb-6 border-b border-border">
        <div>
          <h1 className="text-2xl font-bold tracking-tight">Professional References</h1>
          <p className="text-sm text-muted-foreground mt-1">
            Manage your network of advocates, track consent status, and log where references are shared
          </p>
        </div>

        <button
          onClick={() => {
            setReferenceToEdit(null);
            setIsCreateModalOpen(true);
          }}
          className="inline-flex items-center gap-2 px-4 py-2 bg-primary text-primary-foreground text-xs font-bold rounded-lg hover:bg-primary/90 transition-colors shadow-sm self-start sm:self-auto"
        >
          <Plus className="h-4 w-4" />
          <span>Add Reference</span>
        </button>
      </div>

      {/* Filter and Search Bar */}
      <div className="flex flex-col sm:flex-row sm:items-center justify-between gap-4">
        <div className="flex items-center gap-1.5 overflow-x-auto pb-1 sm:pb-0 scrollbar-none">
          <button
            onClick={() => setActiveConsent('all')}
            className={`px-3 py-1.5 rounded-lg text-xs font-semibold transition-colors flex items-center gap-1.5 ${
              activeConsent === 'all'
                ? 'bg-primary text-primary-foreground shadow-xs'
                : 'bg-muted/60 text-muted-foreground hover:bg-muted hover:text-foreground'
            }`}
          >
            <span>All</span>
            <span className="opacity-75 text-[11px]">({references.length})</span>
          </button>

          <button
            onClick={() => setActiveConsent('Agreed')}
            className={`px-3 py-1.5 rounded-lg text-xs font-semibold transition-colors flex items-center gap-1.5 ${
              activeConsent === 'Agreed'
                ? 'bg-emerald-500 text-white shadow-xs'
                : 'bg-muted/60 text-muted-foreground hover:bg-muted hover:text-foreground'
            }`}
          >
            <CheckCircle2 className="h-3.5 w-3.5" />
            <span>Agreed</span>
          </button>

          <button
            onClick={() => setActiveConsent('Asked')}
            className={`px-3 py-1.5 rounded-lg text-xs font-semibold transition-colors flex items-center gap-1.5 ${
              activeConsent === 'Asked'
                ? 'bg-amber-500 text-white shadow-xs'
                : 'bg-muted/60 text-muted-foreground hover:bg-muted hover:text-foreground'
            }`}
          >
            <Clock className="h-3.5 w-3.5" />
            <span>Asked</span>
          </button>

          <button
            onClick={() => setActiveConsent('NotAsked')}
            className={`px-3 py-1.5 rounded-lg text-xs font-semibold transition-colors flex items-center gap-1.5 ${
              activeConsent === 'NotAsked'
                ? 'bg-secondary text-secondary-foreground shadow-xs'
                : 'bg-muted/60 text-muted-foreground hover:bg-muted hover:text-foreground'
            }`}
          >
            <HelpCircle className="h-3.5 w-3.5" />
            <span>Not Asked</span>
          </button>

          <button
            onClick={() => setActiveConsent('Declined')}
            className={`px-3 py-1.5 rounded-lg text-xs font-semibold transition-colors flex items-center gap-1.5 ${
              activeConsent === 'Declined'
                ? 'bg-destructive text-destructive-foreground shadow-xs'
                : 'bg-muted/60 text-muted-foreground hover:bg-muted hover:text-foreground'
            }`}
          >
            <XCircle className="h-3.5 w-3.5" />
            <span>Declined</span>
          </button>
        </div>

        <div className="relative w-full sm:w-64">
          <Search className="h-4 w-4 absolute left-3 top-2.5 text-muted-foreground" />
          <input
            type="text"
            placeholder="Search references..."
            value={search}
            onChange={(e) => setSearch(e.target.value)}
            className="w-full pl-9 pr-3 py-1.5 text-xs bg-card border border-border rounded-lg focus:outline-none focus:ring-2 focus:ring-primary/20"
          />
        </div>
      </div>

      {/* References Grid */}
      {isLoading ? (
        <div className="grid grid-cols-1 md:grid-cols-2 lg:grid-cols-3 gap-4">
          {[1, 2, 3].map((i) => (
            <div key={i} className="h-44 bg-card/40 border border-border/50 rounded-xl animate-pulse" />
          ))}
        </div>
      ) : references.length === 0 ? (
        <div className="text-center py-16 border border-dashed border-border rounded-xl bg-card/20 space-y-3">
          <div className="h-12 w-12 rounded-full bg-primary/10 text-primary flex items-center justify-center mx-auto">
            <Users className="h-6 w-6" />
          </div>
          <h3 className="font-bold text-sm text-foreground">No references found</h3>
          <p className="text-xs text-muted-foreground max-w-sm mx-auto">
            {search
              ? 'No references match your search query.'
              : 'Add former managers, mentors, or senior colleagues to easily share with hiring teams.'}
          </p>
          <button
            onClick={() => {
              setReferenceToEdit(null);
              setIsCreateModalOpen(true);
            }}
            className="inline-flex items-center gap-1.5 px-3 py-1.5 text-xs font-semibold rounded-lg bg-primary text-primary-foreground hover:bg-primary/90 transition-colors"
          >
            <Plus className="h-3.5 w-3.5" />
            <span>Add Reference</span>
          </button>
        </div>
      ) : (
        <div className="grid grid-cols-1 md:grid-cols-2 lg:grid-cols-3 gap-4">
          {references.map((ref) => (
            <ReferenceCard
              key={ref.id}
              reference={ref}
              onEdit={(r) => {
                setReferenceToEdit(r);
                setIsCreateModalOpen(true);
              }}
              onShare={(r) => setReferenceToShare(r)}
              onNotify={handleNotify}
            />
          ))}
        </div>
      )}

      {/* Modals */}
      <ReferenceModal
        isOpen={isCreateModalOpen}
        onClose={() => {
          setIsCreateModalOpen(false);
          setReferenceToEdit(null);
        }}
        referenceToEdit={referenceToEdit}
      />

      <ShareReferenceModal
        isOpen={Boolean(referenceToShare)}
        onClose={() => setReferenceToShare(null)}
        reference={referenceToShare}
      />

      {notifyingReference && (
        <EmailTemplatePickerModal
          isOpen={Boolean(notifyingReference)}
          onClose={() => setNotifyingReference(null)}
          recipientName={notifyingReference.fullName}
          recipientEmail={notifyingReference.email}
          defaultCategory="FollowUp"
        />
      )}
    </div>
  );
};
