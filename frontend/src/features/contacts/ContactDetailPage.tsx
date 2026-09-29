import React, { useState } from 'react';
import { useParams, useNavigate, Link } from 'react-router-dom';
import {
  useContact,
  useContactInteractions,
  useDeleteContact,
  useUnlinkApplicationContact,
} from './useContacts';
import { WarmthBadge } from './components/WarmthBadge';
import { ContactTimeline } from './components/ContactTimeline';
import { EditContactModal } from './components/EditContactModal';
import { LogInteractionModal } from './components/LogInteractionModal';
import { LinkApplicationModal } from './components/LinkApplicationModal';
import {
  ArrowLeft,
  Building2,
  Mail,
  Phone,
  Linkedin,
  MessageSquarePlus,
  Edit3,
  Trash2,
  Briefcase,
  Plus,
  Unlink,
  ExternalLink,
  AlertCircle,
  Clock,
  Calendar,
} from 'lucide-react';
import { clsx } from 'clsx';

type TabType = 'timeline' | 'applications' | 'notes';

export const ContactDetailPage: React.FC = () => {
  const { id } = useParams<{ id: string }>();
  const navigate = useNavigate();

  const { data: contact, isLoading, error } = useContact(id);
  const { data: interactions } = useContactInteractions(id);
  const deleteMutation = useDeleteContact();
  const unlinkMutation = useUnlinkApplicationContact();

  const [activeTab, setActiveTab] = useState<TabType>('timeline');
  const [isEditModalOpen, setIsEditModalOpen] = useState(false);
  const [isLogModalOpen, setIsLogModalOpen] = useState(false);
  const [isLinkAppModalOpen, setIsLinkAppModalOpen] = useState(false);
  const [isDeleteConfirmOpen, setIsDeleteConfirmOpen] = useState(false);

  if (isLoading) {
    return (
      <div className="p-8 max-w-5xl mx-auto space-y-6 animate-pulse">
        <div className="h-6 w-32 bg-muted rounded" />
        <div className="h-28 bg-card border border-border rounded-xl" />
        <div className="h-64 bg-card border border-border rounded-xl" />
      </div>
    );
  }

  if (error || !contact) {
    return (
      <div className="p-12 max-w-lg mx-auto text-center space-y-4">
        <AlertCircle className="h-12 w-12 text-destructive mx-auto" />
        <h2 className="text-lg font-bold">Contact Not Found</h2>
        <p className="text-xs text-muted-foreground">
          The requested contact could not be found or you do not have permission to view it.
        </p>
        <Link
          to="/contacts"
          className="inline-flex items-center gap-2 px-4 py-2 text-xs font-semibold bg-primary text-primary-foreground rounded-lg"
        >
          <ArrowLeft className="h-4 w-4" />
          Back to Contacts
        </Link>
      </div>
    );
  }

  const handleDelete = async () => {
    await deleteMutation.mutateAsync(contact.id);
    navigate('/contacts');
  };

  const initials = contact.fullName
    .split(' ')
    .filter(Boolean)
    .slice(0, 2)
    .map((w) => w[0].toUpperCase())
    .join('');

  return (
    <div className="space-y-6 max-w-5xl mx-auto pb-12">
      {/* Top back breadcrumb */}
      <div className="flex items-center justify-between">
        <Link
          to="/contacts"
          className="inline-flex items-center gap-1.5 text-xs font-medium text-muted-foreground hover:text-foreground transition-colors group"
        >
          <ArrowLeft className="h-4 w-4 group-hover:-translate-x-0.5 transition-transform" />
          <span>Back to Contacts</span>
        </Link>

        {/* Action buttons */}
        <div className="flex items-center gap-2">
          <button
            type="button"
            onClick={() => setIsLogModalOpen(true)}
            className="inline-flex items-center gap-1.5 px-3 py-1.5 text-xs font-semibold bg-primary text-primary-foreground rounded-lg hover:opacity-90 shadow-sm transition-opacity"
          >
            <MessageSquarePlus className="h-4 w-4" />
            <span>Log Interaction</span>
          </button>

          <button
            type="button"
            onClick={() => setIsEditModalOpen(true)}
            className="inline-flex items-center gap-1.5 px-3 py-1.5 text-xs font-medium bg-card border border-border rounded-lg hover:bg-muted text-foreground transition-colors shadow-2xs"
          >
            <Edit3 className="h-3.5 w-3.5 text-muted-foreground" />
            <span>Edit</span>
          </button>

          <button
            type="button"
            onClick={() => setIsDeleteConfirmOpen(true)}
            className="p-1.5 text-muted-foreground hover:text-destructive hover:bg-destructive/10 rounded-lg transition-colors"
            title="Delete contact"
          >
            <Trash2 className="h-4 w-4" />
          </button>
        </div>
      </div>

      {/* Hero Header Card */}
      <div className="bg-card border border-border rounded-xl p-6 shadow-xs space-y-5">
        <div className="flex flex-col sm:flex-row sm:items-center justify-between gap-4">
          <div className="flex items-start gap-4">
            <div className="h-16 w-16 rounded-2xl bg-primary/10 border border-primary/20 flex items-center justify-center font-bold text-xl text-primary shrink-0 shadow-2xs">
              {initials}
            </div>

            <div>
              <div className="flex items-center gap-2.5 flex-wrap">
                <h1 className="text-xl font-bold tracking-tight text-foreground">
                  {contact.fullName}
                </h1>
                <span className="px-2.5 py-0.5 rounded-full text-xs font-semibold bg-muted text-foreground border border-border">
                  {contact.type}
                </span>
              </div>

              <div className="flex items-center gap-3 text-xs text-muted-foreground mt-1 flex-wrap">
                {contact.role && <span className="font-medium text-foreground">{contact.role}</span>}
                {contact.role && contact.companyName && <span>•</span>}
                {contact.companyName && (
                  <span className="flex items-center gap-1">
                    <Building2 className="h-3.5 w-3.5" />
                    {contact.companyName}
                  </span>
                )}
              </div>

              {/* Direct links */}
              <div className="flex items-center gap-3 text-xs mt-3 flex-wrap">
                {contact.email && (
                  <a
                    href={`mailto:${contact.email}`}
                    className="inline-flex items-center gap-1.5 text-primary hover:underline"
                  >
                    <Mail className="h-3.5 w-3.5 text-muted-foreground" />
                    <span>{contact.email}</span>
                  </a>
                )}

                {contact.phone && (
                  <a
                    href={`tel:${contact.phone}`}
                    className="inline-flex items-center gap-1.5 text-muted-foreground hover:text-foreground"
                  >
                    <Phone className="h-3.5 w-3.5 text-muted-foreground" />
                    <span>{contact.phone}</span>
                  </a>
                )}

                {contact.linkedInUrl && (
                  <a
                    href={contact.linkedInUrl}
                    target="_blank"
                    rel="noreferrer"
                    className="inline-flex items-center gap-1.5 text-blue-600 dark:text-blue-400 hover:underline"
                  >
                    <Linkedin className="h-3.5 w-3.5" />
                    <span>LinkedIn Profile</span>
                  </a>
                )}
              </div>
            </div>
          </div>

          {/* Warmth & Follow-up Card */}
          <div className="flex sm:flex-col items-end sm:items-end justify-between sm:justify-center gap-3 border-t sm:border-t-0 sm:border-l border-border pt-3 sm:pt-0 sm:pl-6 shrink-0">
            <WarmthBadge warmth={contact.warmth} daysSince={contact.daysSinceLastContact} size="md" />

            {contact.nextFollowUpAt && (
              <div className="flex items-center gap-1.5 text-xs text-amber-600 dark:text-amber-400 bg-amber-500/10 px-2.5 py-1 rounded-lg border border-amber-500/20">
                <Calendar className="h-3.5 w-3.5" />
                <span>Follow-up {new Date(contact.nextFollowUpAt).toLocaleDateString()}</span>
              </div>
            )}
          </div>
        </div>
      </div>

      {/* Tab Navigation */}
      <div className="border-b border-border flex items-center gap-6">
        {[
          { key: 'timeline', label: `Interactions (${interactions?.length ?? contact.recentInteractions?.length ?? 0})` },
          { key: 'applications', label: `Linked Applications (${contact.linkedApplications.length})` },
          { key: 'notes', label: 'Notes & Strategy' },
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
        {/* TAB 1: INTERACTION TIMELINE */}
        {activeTab === 'timeline' && (
          <div className="space-y-4">
            <div className="flex items-center justify-between">
              <span className="text-xs font-bold uppercase tracking-wider text-muted-foreground">
                Chronological Communication History
              </span>
              <button
                type="button"
                onClick={() => setIsLogModalOpen(true)}
                className="inline-flex items-center gap-1.5 px-3 py-1 text-xs font-semibold text-primary bg-primary/10 hover:bg-primary/20 rounded-md transition-colors"
              >
                <Plus className="h-3.5 w-3.5" />
                <span>Log Interaction</span>
              </button>
            </div>

            <ContactTimeline
              interactions={interactions || contact.recentInteractions}
              onNewInteractionClick={() => setIsLogModalOpen(true)}
            />
          </div>
        )}

        {/* TAB 2: LINKED APPLICATIONS */}
        {activeTab === 'applications' && (
          <div className="space-y-4">
            <div className="flex items-center justify-between">
              <span className="text-xs font-bold uppercase tracking-wider text-muted-foreground">
                Associated Applications ({contact.linkedApplications.length})
              </span>
              <button
                type="button"
                onClick={() => setIsLinkAppModalOpen(true)}
                className="inline-flex items-center gap-1.5 px-3 py-1 text-xs font-semibold text-primary bg-primary/10 hover:bg-primary/20 rounded-md transition-colors"
              >
                <Plus className="h-3.5 w-3.5" />
                <span>Link Application</span>
              </button>
            </div>

            {contact.linkedApplications.length === 0 ? (
              <div className="flex flex-col items-center justify-center p-12 text-center border border-dashed border-border rounded-xl bg-card">
                <Briefcase className="h-10 w-10 text-muted-foreground/50 mb-3" />
                <h3 className="font-semibold text-sm text-foreground">No Linked Applications</h3>
                <p className="text-xs text-muted-foreground mt-1 max-w-sm">
                  Associate this contact with one or more job candidacies to track their role in your hiring process.
                </p>
                <button
                  type="button"
                  onClick={() => setIsLinkAppModalOpen(true)}
                  className="mt-4 px-4 py-1.5 text-xs font-semibold bg-primary text-primary-foreground rounded-lg hover:opacity-90 shadow-sm"
                >
                  Link to an Application
                </button>
              </div>
            ) : (
              <div className="grid grid-cols-1 sm:grid-cols-2 gap-4">
                {contact.linkedApplications.map((app) => (
                  <div
                    key={app.applicationId}
                    className="bg-card border border-border rounded-xl p-4 shadow-2xs hover:border-primary/40 transition-colors flex flex-col justify-between"
                  >
                    <div>
                      <div className="flex items-start justify-between gap-2">
                        <div>
                          <span className="text-[10px] font-semibold text-muted-foreground flex items-center gap-1 uppercase tracking-wider">
                            <Building2 className="h-3 w-3" />
                            {app.companyName}
                          </span>
                          <Link
                            to={`/applications/${app.applicationId}`}
                            className="font-bold text-sm text-foreground hover:text-primary transition-colors flex items-center gap-1 mt-0.5"
                          >
                            <span>{app.roleTitle}</span>
                            <ExternalLink className="h-3.5 w-3.5 text-muted-foreground" />
                          </Link>
                        </div>

                        <span className="px-2 py-0.5 rounded-full text-[10px] font-semibold bg-muted text-foreground border border-border">
                          {app.status}
                        </span>
                      </div>

                      {app.roleInProcess && (
                        <div className="mt-3 pt-2 border-t border-border/60">
                          <span className="text-[11px] text-muted-foreground">
                            Role in process:{' '}
                            <strong className="text-foreground font-semibold">
                              {app.roleInProcess}
                            </strong>
                          </span>
                        </div>
                      )}
                    </div>

                    <div className="mt-4 pt-3 border-t border-border flex items-center justify-end">
                      <button
                        type="button"
                        onClick={() =>
                          unlinkMutation.mutate({
                            contactId: contact.id,
                            applicationId: app.applicationId,
                          })
                        }
                        disabled={unlinkMutation.isPending}
                        className="inline-flex items-center gap-1 text-[11px] text-muted-foreground hover:text-destructive transition-colors"
                      >
                        <Unlink className="h-3 w-3" />
                        <span>Unlink</span>
                      </button>
                    </div>
                  </div>
                ))}
              </div>
            )}
          </div>
        )}

        {/* TAB 3: NOTES & STRATEGY */}
        {activeTab === 'notes' && (
          <div className="bg-card border border-border rounded-xl p-5 shadow-2xs space-y-3">
            <div className="flex items-center justify-between">
              <h3 className="text-xs font-bold uppercase tracking-wider text-muted-foreground">
                Private Notes & Relationship Strategy
              </h3>
              <button
                type="button"
                onClick={() => setIsEditModalOpen(true)}
                className="text-xs text-primary hover:underline font-semibold"
              >
                Edit Notes
              </button>
            </div>
            <p className="text-xs text-foreground/90 leading-relaxed whitespace-pre-wrap font-sans">
              {contact.notes || (
                <span className="text-muted-foreground italic">
                  No notes recorded yet. Click Edit to record background information, personal details, or communication preferences.
                </span>
              )}
            </p>
          </div>
        )}
      </div>

      {/* Edit Modal */}
      {isEditModalOpen && (
        <EditContactModal
          isOpen={isEditModalOpen}
          onClose={() => setIsEditModalOpen(false)}
          contact={contact}
        />
      )}

      {/* Log Interaction Modal */}
      {isLogModalOpen && (
        <LogInteractionModal
          isOpen={isLogModalOpen}
          onClose={() => setIsLogModalOpen(false)}
          contactId={contact.id}
          contactName={contact.fullName}
        />
      )}

      {/* Link Application Modal */}
      {isLinkAppModalOpen && (
        <LinkApplicationModal
          isOpen={isLinkAppModalOpen}
          onClose={() => setIsLinkAppModalOpen(false)}
          contactId={contact.id}
        />
      )}

      {/* Delete Confirmation Modal */}
      {isDeleteConfirmOpen && (
        <div className="fixed inset-0 z-50 flex items-center justify-center p-4 bg-background/80 backdrop-blur-sm animate-in fade-in duration-200">
          <div className="border border-border bg-card max-w-sm w-full rounded-xl shadow-2xl p-6 space-y-4">
            <h3 className="font-bold text-sm text-foreground">Delete Contact</h3>
            <p className="text-xs text-muted-foreground">
              Are you sure you want to delete <strong className="text-foreground">{contact.fullName}</strong>? Their interaction history will be preserved.
            </p>
            <div className="flex items-center justify-end gap-2 pt-2">
              <button
                type="button"
                onClick={() => setIsDeleteConfirmOpen(false)}
                className="px-3.5 py-1.5 text-xs font-medium text-muted-foreground hover:text-foreground hover:bg-muted rounded-lg"
              >
                Cancel
              </button>
              <button
                type="button"
                onClick={handleDelete}
                disabled={deleteMutation.isPending}
                className="px-4 py-1.5 text-xs font-semibold bg-destructive text-destructive-foreground rounded-lg hover:opacity-90 disabled:opacity-50"
              >
                {deleteMutation.isPending ? 'Deleting...' : 'Delete'}
              </button>
            </div>
          </div>
        </div>
      )}
    </div>
  );
};
