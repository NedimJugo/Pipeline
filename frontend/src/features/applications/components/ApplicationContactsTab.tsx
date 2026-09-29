import React, { useState } from 'react';
import { Link } from 'react-router-dom';
import {
  useApplicationContacts,
  useUnlinkContactFromApplication,
} from '@/features/contacts/useContacts';
import { WarmthBadge } from '@/features/contacts/components/WarmthBadge';
import { CreateContactModal } from '@/features/contacts/components/CreateContactModal';
import { LogInteractionModal } from '@/features/contacts/components/LogInteractionModal';
import { LinkContactModal } from './LinkContactModal';
import {
  Users,
  UserPlus,
  Link as LinkIcon,
  MessageSquare,
  Mail,
  Phone,
  Linkedin,
  ExternalLink,
  Unlink,
  Clock,
  Calendar,
  AlertCircle,
} from 'lucide-react';
import { ContactListItem } from '@/features/contacts/types';

interface ApplicationContactsTabProps {
  applicationId: string;
  roleTitle: string;
  companyName: string;
}

export const ApplicationContactsTab: React.FC<ApplicationContactsTabProps> = ({
  applicationId,
  roleTitle,
  companyName,
}) => {
  const { data: contacts, isLoading, error } = useApplicationContacts(applicationId);
  const unlinkMutation = useUnlinkContactFromApplication();

  const [isCreateModalOpen, setIsCreateModalOpen] = useState(false);
  const [isLinkModalOpen, setIsLinkModalOpen] = useState(false);
  const [isLogModalOpen, setIsLogModalOpen] = useState(false);
  const [selectedContactForLog, setSelectedContactForLog] = useState<{
    id: string;
    name: string;
  } | null>(null);

  const handleOpenLogModal = (contact?: { id: string; name: string }) => {
    setSelectedContactForLog(contact || null);
    setIsLogModalOpen(true);
  };

  const handleUnlink = async (contactId: string, contactName: string) => {
    if (
      window.confirm(
        `Are you sure you want to unlink ${contactName} from this application? The contact record will remain in your address book.`
      )
    ) {
      await unlinkMutation.mutateAsync({ applicationId, contactId });
    }
  };

  if (isLoading) {
    return (
      <div className="space-y-4">
        <div className="h-8 w-48 bg-muted rounded animate-pulse" />
        <div className="grid grid-cols-1 md:grid-cols-2 gap-4">
          <div className="h-36 bg-card border border-border rounded-xl animate-pulse" />
          <div className="h-36 bg-card border border-border rounded-xl animate-pulse" />
        </div>
      </div>
    );
  }

  if (error) {
    return (
      <div className="p-8 text-center bg-card border border-border rounded-xl space-y-2">
        <AlertCircle className="h-8 w-8 text-destructive mx-auto" />
        <h3 className="text-sm font-bold text-foreground">Failed to load contacts</h3>
        <p className="text-xs text-muted-foreground">
          There was an error loading the contacts linked to this application.
        </p>
      </div>
    );
  }

  const linkedContactList = contacts || [];

  return (
    <div className="space-y-6">
      {/* Tab Header Controls */}
      <div className="flex flex-col sm:flex-row sm:items-center justify-between gap-3 bg-card border border-border rounded-xl p-4 shadow-2xs">
        <div className="flex items-center gap-2.5">
          <div className="p-2 bg-primary/10 rounded-lg text-primary">
            <Users className="h-5 w-5" />
          </div>
          <div>
            <div className="flex items-center gap-2">
              <h2 className="text-sm font-bold text-foreground">Process Contacts</h2>
              <span className="px-2 py-0.5 text-[11px] font-mono font-semibold bg-muted text-muted-foreground rounded-full border border-border">
                {linkedContactList.length}
              </span>
            </div>
            <p className="text-xs text-muted-foreground">
              Recruiters, hiring managers, and interviewers associated with this position.
            </p>
          </div>
        </div>

        <div className="flex items-center gap-2 flex-wrap">
          <button
            type="button"
            onClick={() => handleOpenLogModal()}
            className="inline-flex items-center gap-1.5 px-3 py-1.5 text-xs font-semibold bg-card border border-border hover:bg-muted text-foreground rounded-lg transition-colors shadow-2xs"
          >
            <MessageSquare className="h-3.5 w-3.5 text-primary" />
            <span>Log Interaction</span>
          </button>

          <button
            type="button"
            onClick={() => setIsLinkModalOpen(true)}
            className="inline-flex items-center gap-1.5 px-3 py-1.5 text-xs font-semibold bg-card border border-border hover:bg-muted text-foreground rounded-lg transition-colors shadow-2xs"
          >
            <LinkIcon className="h-3.5 w-3.5 text-muted-foreground" />
            <span>Link Existing</span>
          </button>

          <button
            type="button"
            onClick={() => setIsCreateModalOpen(true)}
            className="inline-flex items-center gap-1.5 px-3 py-1.5 text-xs font-semibold bg-primary text-primary-foreground hover:opacity-90 rounded-lg shadow-sm transition-opacity"
          >
            <UserPlus className="h-3.5 w-3.5" />
            <span>Add Contact</span>
          </button>
        </div>
      </div>

      {/* Contacts List or Empty State */}
      {linkedContactList.length === 0 ? (
        <div className="bg-card border border-dashed border-border rounded-xl p-10 text-center space-y-4">
          <div className="w-12 h-12 bg-muted/50 rounded-xl flex items-center justify-center mx-auto text-muted-foreground">
            <Users className="h-6 w-6" />
          </div>
          <div className="max-w-md mx-auto space-y-1">
            <h3 className="text-sm font-bold text-foreground">No contacts linked yet</h3>
            <p className="text-xs text-muted-foreground leading-relaxed">
              Connect the hiring team, recruiter, or internal referrers for this role.
              Logging your communications updates contact warmth and timeline history automatically.
            </p>
          </div>
          <div className="flex items-center justify-center gap-3 pt-2">
            <button
              type="button"
              onClick={() => setIsCreateModalOpen(true)}
              className="inline-flex items-center gap-1.5 px-4 py-2 text-xs font-semibold bg-primary text-primary-foreground rounded-lg hover:opacity-90 transition-opacity shadow-sm"
            >
              <UserPlus className="h-4 w-4" />
              <span>Create New Contact</span>
            </button>
            <button
              type="button"
              onClick={() => setIsLinkModalOpen(true)}
              className="inline-flex items-center gap-1.5 px-4 py-2 text-xs font-semibold bg-card border border-border hover:bg-muted text-foreground rounded-lg transition-colors"
            >
              <LinkIcon className="h-4 w-4 text-muted-foreground" />
              <span>Link Existing Contact</span>
            </button>
          </div>
        </div>
      ) : (
        <div className="grid grid-cols-1 md:grid-cols-2 gap-4">
          {linkedContactList.map((contact: ContactListItem) => {
            const initials = contact.fullName
              .split(' ')
              .filter(Boolean)
              .slice(0, 2)
              .map((w) => w[0].toUpperCase())
              .join('');

            return (
              <div
                key={contact.id}
                className="bg-card border border-border rounded-xl p-5 shadow-2xs hover:border-primary/30 transition-all flex flex-col justify-between space-y-4 group"
              >
                {/* Header: Name, Avatar, Warmth Badge */}
                <div className="flex items-start justify-between gap-3">
                  <div className="flex items-start gap-3 min-w-0">
                    <div className="h-10 w-10 rounded-xl bg-primary/10 border border-primary/20 flex items-center justify-center font-mono font-bold text-xs text-primary shrink-0">
                      {initials}
                    </div>

                    <div className="min-w-0">
                      <Link
                        to={`/contacts/${contact.id}`}
                        className="font-bold text-sm text-foreground hover:text-primary transition-colors flex items-center gap-1.5 group-hover:text-primary truncate"
                      >
                        <span className="truncate">{contact.fullName}</span>
                        <ExternalLink className="h-3 w-3 opacity-0 group-hover:opacity-100 transition-opacity shrink-0" />
                      </Link>

                      <div className="text-xs text-muted-foreground truncate">
                        {contact.role || contact.type}
                        {contact.companyName ? ` at ${contact.companyName}` : ''}
                      </div>

                      <div className="mt-1">
                        <span className="inline-block px-2 py-0.5 rounded-full text-[10px] font-semibold bg-muted border border-border text-foreground/80">
                          {contact.type}
                        </span>
                      </div>
                    </div>
                  </div>

                  <div className="shrink-0 flex flex-col items-end gap-1.5">
                    <WarmthBadge warmth={contact.warmth} size="sm" />
                    <span className="text-[10px] font-mono text-muted-foreground">
                      {contact.daysSinceLastContact !== null && contact.daysSinceLastContact !== undefined
                        ? `${contact.daysSinceLastContact}d ago`
                        : 'No activity'}
                    </span>
                  </div>
                </div>

                {/* Communication channels */}
                <div className="flex items-center gap-3 text-xs text-muted-foreground pt-1 border-t border-border/50">
                  {contact.email && (
                    <a
                      href={`mailto:${contact.email}`}
                      className="inline-flex items-center gap-1 hover:text-foreground transition-colors truncate"
                      title={contact.email}
                    >
                      <Mail className="h-3.5 w-3.5 shrink-0" />
                      <span className="truncate">{contact.email}</span>
                    </a>
                  )}

                  {contact.phone && (
                    <a
                      href={`tel:${contact.phone}`}
                      className="inline-flex items-center gap-1 hover:text-foreground transition-colors shrink-0"
                      title={contact.phone}
                    >
                      <Phone className="h-3.5 w-3.5" />
                      <span>{contact.phone}</span>
                    </a>
                  )}

                  {contact.linkedInUrl && (
                    <a
                      href={contact.linkedInUrl}
                      target="_blank"
                      rel="noreferrer"
                      className="inline-flex items-center gap-1 hover:text-primary transition-colors shrink-0 ml-auto"
                      title="LinkedIn"
                    >
                      <Linkedin className="h-3.5 w-3.5 text-blue-500" />
                    </a>
                  )}
                </div>

                {/* Follow up alert if set */}
                {contact.nextFollowUpAt && (
                  <div className="flex items-center gap-1.5 text-[11px] text-amber-600 dark:text-amber-400 bg-amber-500/10 px-2.5 py-1 rounded-md border border-amber-500/20">
                    <Calendar className="h-3 w-3 shrink-0" />
                    <span className="font-medium truncate">
                      Follow-up due {new Date(contact.nextFollowUpAt).toLocaleDateString()}
                    </span>
                  </div>
                )}

                {/* Bottom Actions */}
                <div className="flex items-center justify-between pt-2 border-t border-border">
                  <button
                    type="button"
                    onClick={() =>
                      handleOpenLogModal({ id: contact.id, name: contact.fullName })
                    }
                    className="inline-flex items-center gap-1.5 text-xs font-semibold text-primary hover:underline"
                  >
                    <MessageSquare className="h-3.5 w-3.5" />
                    <span>Log Interaction</span>
                  </button>

                  <button
                    type="button"
                    onClick={() => handleUnlink(contact.id, contact.fullName)}
                    disabled={unlinkMutation.isPending}
                    className="inline-flex items-center gap-1 text-xs text-muted-foreground hover:text-destructive transition-colors disabled:opacity-50"
                    title="Unlink from this application"
                  >
                    <Unlink className="h-3.5 w-3.5" />
                    <span>Unlink</span>
                  </button>
                </div>
              </div>
            );
          })}
        </div>
      )}

      {/* Modals */}
      {isCreateModalOpen && (
        <CreateContactModal
          isOpen={isCreateModalOpen}
          onClose={() => setIsCreateModalOpen(false)}
          preselectedApplicationId={applicationId}
          preselectedCompanyName={companyName}
        />
      )}

      {isLinkModalOpen && (
        <LinkContactModal
          isOpen={isLinkModalOpen}
          onClose={() => setIsLinkModalOpen(false)}
          applicationId={applicationId}
          applicationTitle={roleTitle}
          companyName={companyName}
          alreadyLinkedContactIds={linkedContactList.map((c) => c.id)}
        />
      )}

      {isLogModalOpen && (
        <LogInteractionModal
          isOpen={isLogModalOpen}
          onClose={() => {
            setIsLogModalOpen(false);
            setSelectedContactForLog(null);
          }}
          contactId={selectedContactForLog?.id}
          contactName={selectedContactForLog?.name}
          preselectedApplicationId={applicationId}
        />
      )}
    </div>
  );
};
