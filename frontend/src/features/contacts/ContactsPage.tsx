import React, { useState, useMemo } from 'react';
import { useContacts } from './useContacts';
import { ContactFilter, ContactListItem } from './types';
import { ContactCard } from './components/ContactCard';
import { ContactsFilterBar } from './components/ContactsFilterBar';
import { CreateContactModal } from './components/CreateContactModal';
import { LogInteractionModal } from './components/LogInteractionModal';
import { Users, Flame, Snowflake, Calendar, UserPlus } from 'lucide-react';

export const ContactsPage: React.FC = () => {
  const [filter, setFilter] = useState<ContactFilter>({});
  const [isCreateModalOpen, setIsCreateModalOpen] = useState(false);
  const [activeLogContact, setActiveLogContact] = useState<ContactListItem | null>(null);

  const { data: contacts, isLoading, error } = useContacts(filter);

  // Compute CRM metrics
  const metrics = useMemo(() => {
    if (!contacts) {
      return { total: 0, hot: 0, cold: 0, followUpsDue: 0 };
    }
    const total = contacts.length;
    const hot = contacts.filter((c) => c.warmth === 'Hot').length;
    const cold = contacts.filter((c) => c.warmth === 'Cold').length;
    const followUpsDue = contacts.filter(
      (c) => c.nextFollowUpAt && new Date(c.nextFollowUpAt) <= new Date()
    ).length;

    return { total, hot, cold, followUpsDue };
  }, [contacts]);

  return (
    <div className="space-y-6 max-w-7xl mx-auto pb-12">
      {/* Header */}
      <div className="flex flex-col sm:flex-row sm:items-center justify-between gap-4">
        <div>
          <h1 className="text-xl font-bold tracking-tight text-foreground">
            Contacts & Recruiter CRM
          </h1>
          <p className="text-xs text-muted-foreground mt-0.5">
            Manage recruiter relationships, interviewers, warmth indicators, and interaction history.
          </p>
        </div>

        <button
          type="button"
          onClick={() => setIsCreateModalOpen(true)}
          className="inline-flex items-center gap-1.5 px-3.5 py-2 text-xs font-semibold bg-primary text-primary-foreground rounded-lg hover:opacity-90 shadow-sm transition-opacity self-start sm:self-auto"
        >
          <UserPlus className="h-4 w-4" />
          <span>New Contact</span>
        </button>
      </div>

      {/* Metrics Row */}
      <div className="grid grid-cols-2 lg:grid-cols-4 gap-4">
        <div className="bg-card border border-border rounded-xl p-4 shadow-2xs">
          <div className="flex items-center justify-between">
            <span className="text-xs font-semibold text-muted-foreground">Total Contacts</span>
            <Users className="h-4 w-4 text-muted-foreground" />
          </div>
          <p className="text-2xl font-bold text-foreground mt-2">{metrics.total}</p>
        </div>

        <div className="bg-card border border-border rounded-xl p-4 shadow-2xs">
          <div className="flex items-center justify-between">
            <span className="text-xs font-semibold text-muted-foreground">Active Relationships</span>
            <Flame className="h-4 w-4 text-rose-500" />
          </div>
          <p className="text-2xl font-bold text-rose-600 dark:text-rose-400 mt-2">
            {metrics.hot}
          </p>
        </div>

        <div className="bg-card border border-border rounded-xl p-4 shadow-2xs">
          <div className="flex items-center justify-between">
            <span className="text-xs font-semibold text-muted-foreground">Needs Re-engagement</span>
            <Snowflake className="h-4 w-4 text-sky-500" />
          </div>
          <p className="text-2xl font-bold text-sky-600 dark:text-sky-400 mt-2">
            {metrics.cold}
          </p>
        </div>

        <div className="bg-card border border-border rounded-xl p-4 shadow-2xs">
          <div className="flex items-center justify-between">
            <span className="text-xs font-semibold text-muted-foreground">Follow-ups Due</span>
            <Calendar className="h-4 w-4 text-amber-500" />
          </div>
          <p className="text-2xl font-bold text-amber-600 dark:text-amber-400 mt-2">
            {metrics.followUpsDue}
          </p>
        </div>
      </div>

      {/* Filter Bar */}
      <ContactsFilterBar
        filter={filter}
        onFilterChange={setFilter}
        onNewContactClick={() => setIsCreateModalOpen(true)}
        totalCount={contacts?.length || 0}
      />

      {/* Contact Cards Grid */}
      {isLoading ? (
        <div className="grid grid-cols-1 md:grid-cols-2 lg:grid-cols-3 gap-4">
          {[1, 2, 3, 4, 5, 6].map((i) => (
            <div
              key={i}
              className="bg-card border border-border rounded-xl p-5 shadow-2xs animate-pulse space-y-4"
            >
              <div className="flex items-start gap-3">
                <div className="h-10 w-10 rounded-xl bg-muted shrink-0" />
                <div className="space-y-2 flex-1">
                  <div className="h-4 bg-muted rounded w-2/3" />
                  <div className="h-3 bg-muted rounded w-1/2" />
                </div>
              </div>
              <div className="h-6 bg-muted rounded w-1/3" />
              <div className="h-8 bg-muted rounded" />
            </div>
          ))}
        </div>
      ) : error ? (
        <div className="p-8 text-center text-xs text-destructive border border-destructive/20 rounded-xl bg-destructive/5">
          Failed to load contacts. Please verify your connection.
        </div>
      ) : !contacts || contacts.length === 0 ? (
        <div className="flex flex-col items-center justify-center p-12 text-center border border-dashed border-border rounded-xl bg-card">
          <Users className="h-10 w-10 text-muted-foreground/50 mb-3" />
          <h3 className="font-semibold text-sm text-foreground">No Contacts Found</h3>
          <p className="text-xs text-muted-foreground mt-1 max-w-sm">
            {filter.search || filter.type || filter.warmth
              ? 'No contacts match your current filter criteria.'
              : 'Add your first recruiter, hiring manager, or referrer to track communications and warmth.'}
          </p>
          <button
            type="button"
            onClick={() => setIsCreateModalOpen(true)}
            className="mt-4 px-4 py-2 text-xs font-semibold bg-primary text-primary-foreground rounded-lg hover:opacity-90 shadow-sm"
          >
            Add First Contact
          </button>
        </div>
      ) : (
        <div className="grid grid-cols-1 md:grid-cols-2 lg:grid-cols-3 gap-4">
          {contacts.map((contact) => (
            <ContactCard
              key={contact.id}
              contact={contact}
              onLogInteraction={(c) => setActiveLogContact(c)}
            />
          ))}
        </div>
      )}

      {/* Create Modal */}
      {isCreateModalOpen && (
        <CreateContactModal
          isOpen={isCreateModalOpen}
          onClose={() => setIsCreateModalOpen(false)}
        />
      )}

      {/* Log Interaction Modal */}
      {activeLogContact && (
        <LogInteractionModal
          isOpen={Boolean(activeLogContact)}
          onClose={() => setActiveLogContact(null)}
          contactId={activeLogContact.id}
          contactName={activeLogContact.fullName}
        />
      )}
    </div>
  );
};
