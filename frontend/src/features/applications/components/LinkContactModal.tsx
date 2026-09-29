import React, { useState } from 'react';
import { useContacts, useLinkContactToApplication } from '@/features/contacts/useContacts';
import { X, UserPlus, Search, Check } from 'lucide-react';
import { WarmthBadge } from '@/features/contacts/components/WarmthBadge';

interface LinkContactModalProps {
  isOpen: boolean;
  onClose: () => void;
  applicationId: string;
  applicationTitle: string;
  companyName: string;
  alreadyLinkedContactIds: string[];
}

export const LinkContactModal: React.FC<LinkContactModalProps> = ({
  isOpen,
  onClose,
  applicationId,
  applicationTitle,
  companyName,
  alreadyLinkedContactIds,
}) => {
  const [selectedContactId, setSelectedContactId] = useState('');
  const [roleInProcess, setRoleInProcess] = useState('');
  const [search, setSearch] = useState('');

  const { data: contacts, isLoading } = useContacts();
  const linkMutation = useLinkContactToApplication();

  if (!isOpen) return null;

  const availableContacts = (contacts || []).filter(
    (c) => !alreadyLinkedContactIds.includes(c.id)
  );

  const filteredContacts = availableContacts.filter((c) => {
    if (!search.trim()) return true;
    const term = search.toLowerCase();
    return (
      c.fullName.toLowerCase().includes(term) ||
      (c.companyName && c.companyName.toLowerCase().includes(term)) ||
      (c.role && c.role.toLowerCase().includes(term))
    );
  });

  const handleSubmit = async (e: React.FormEvent) => {
    e.preventDefault();
    if (!selectedContactId) return;

    await linkMutation.mutateAsync({
      applicationId,
      payload: {
        contactId: selectedContactId,
        roleInProcess: roleInProcess.trim() || undefined,
      },
    });

    onClose();
  };

  return (
    <div className="fixed inset-0 z-50 flex items-center justify-center p-4 bg-background/80 backdrop-blur-sm animate-in fade-in duration-200">
      <div className="border border-border bg-card max-w-md w-full rounded-xl shadow-2xl overflow-hidden flex flex-col max-h-[85vh]">
        {/* Header */}
        <div className="px-6 py-4 border-b border-border flex items-center justify-between">
          <div className="flex items-center gap-2">
            <UserPlus className="h-5 w-5 text-primary" />
            <div>
              <h2 className="font-bold text-base tracking-tight text-foreground">
                Link Contact
              </h2>
              <p className="text-xs text-muted-foreground truncate max-w-xs">
                To {companyName} — {applicationTitle}
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

        {/* Content */}
        <form onSubmit={handleSubmit} className="p-6 space-y-4 flex-1 overflow-y-auto">
          {/* Search Contacts */}
          <div>
            <label className="block text-xs font-semibold uppercase tracking-wider text-muted-foreground mb-1.5">
              Select Contact *
            </label>
            <div className="relative mb-2">
              <Search className="h-3.5 w-3.5 absolute left-3 top-2.5 text-muted-foreground" />
              <input
                type="text"
                placeholder="Search contacts by name, role, company..."
                value={search}
                onChange={(e) => setSearch(e.target.value)}
                className="w-full pl-8 pr-3 py-1.5 text-xs bg-background border border-border rounded-lg focus:outline-none focus:ring-2 focus:ring-primary"
              />
            </div>

            {isLoading ? (
              <div className="p-4 text-center text-xs text-muted-foreground animate-pulse">
                Loading contacts...
              </div>
            ) : filteredContacts.length === 0 ? (
              <div className="p-4 text-center text-xs text-muted-foreground bg-muted/30 border border-border rounded-lg">
                {availableContacts.length === 0
                  ? 'All existing contacts are already linked to this application.'
                  : 'No contacts match your search query.'}
              </div>
            ) : (
              <div className="max-h-48 overflow-y-auto border border-border rounded-lg divide-y divide-border bg-background">
                {filteredContacts.map((contact) => {
                  const isSelected = selectedContactId === contact.id;
                  return (
                    <button
                      key={contact.id}
                      type="button"
                      onClick={() => setSelectedContactId(contact.id)}
                      className={`w-full text-left px-3 py-2.5 flex items-center justify-between text-xs transition-colors ${
                        isSelected
                          ? 'bg-primary/10 text-primary font-medium'
                          : 'hover:bg-muted text-foreground'
                      }`}
                    >
                      <div className="min-w-0 pr-2">
                        <div className="flex items-center gap-1.5">
                          <span className="font-semibold truncate">{contact.fullName}</span>
                          <WarmthBadge warmth={contact.warmth} size="sm" />
                        </div>
                        <div className="text-[11px] text-muted-foreground truncate">
                          {contact.role || contact.type}
                          {contact.companyName ? ` • ${contact.companyName}` : ''}
                        </div>
                      </div>
                      {isSelected && <Check className="h-4 w-4 text-primary shrink-0" />}
                    </button>
                  );
                })}
              </div>
            )}
          </div>

          {/* Role in this process */}
          <div>
            <label className="block text-xs font-semibold uppercase tracking-wider text-muted-foreground mb-1">
              Role in this Job Process (Optional)
            </label>
            <input
              type="text"
              placeholder="e.g. Primary Recruiter, Hiring Manager, Technical Screener"
              value={roleInProcess}
              onChange={(e) => setRoleInProcess(e.target.value)}
              className="w-full px-3 py-1.5 text-xs bg-background border border-border rounded-lg focus:outline-none focus:ring-2 focus:ring-primary"
            />
          </div>

          {/* Footer Actions */}
          <div className="flex items-center justify-end gap-2 pt-3 border-t border-border">
            <button
              type="button"
              onClick={onClose}
              disabled={linkMutation.isPending}
              className="px-3.5 py-1.5 text-xs font-medium text-muted-foreground hover:text-foreground hover:bg-muted rounded-lg transition-colors"
            >
              Cancel
            </button>
            <button
              type="submit"
              disabled={!selectedContactId || linkMutation.isPending}
              className="px-4 py-1.5 text-xs font-semibold bg-primary text-primary-foreground rounded-lg hover:opacity-90 shadow-sm transition-opacity disabled:opacity-50"
            >
              {linkMutation.isPending ? 'Linking...' : 'Link Contact'}
            </button>
          </div>
        </form>
      </div>
    </div>
  );
};
