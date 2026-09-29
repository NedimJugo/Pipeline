import React from 'react';
import { useNavigate } from 'react-router-dom';
import { ContactListItem } from '../types';
import { WarmthBadge } from './WarmthBadge';
import {
  Mail,
  Phone,
  Linkedin,
  Building2,
  Briefcase,
  Calendar,
  MessageSquarePlus,
  ChevronRight,
} from 'lucide-react';
import { clsx } from 'clsx';

interface ContactCardProps {
  contact: ContactListItem;
  onLogInteraction: (contact: ContactListItem) => void;
}

export const ContactCard: React.FC<ContactCardProps> = ({
  contact,
  onLogInteraction,
}) => {
  const navigate = useNavigate();

  const initials = contact.fullName
    .split(' ')
    .filter(Boolean)
    .slice(0, 2)
    .map((w) => w[0].toUpperCase())
    .join('');

  const getTypeStyle = (type: string) => {
    switch (type) {
      case 'Recruiter':
        return 'bg-blue-500/10 text-blue-600 dark:text-blue-400 border-blue-500/20';
      case 'HiringManager':
        return 'bg-purple-500/10 text-purple-600 dark:text-purple-400 border-purple-500/20';
      case 'Interviewer':
        return 'bg-amber-500/10 text-amber-600 dark:text-amber-400 border-amber-500/20';
      case 'Referrer':
        return 'bg-emerald-500/10 text-emerald-600 dark:text-emerald-400 border-emerald-500/20';
      default:
        return 'bg-muted text-muted-foreground border-border';
    }
  };

  return (
    <div
      onClick={() => navigate(`/contacts/${contact.id}`)}
      className="group bg-card border border-border rounded-xl p-5 shadow-2xs hover:border-primary/40 hover:shadow-xs transition-all cursor-pointer flex flex-col justify-between"
    >
      <div className="space-y-4">
        {/* Header row: Avatar + Name/Role + Warmth */}
        <div className="flex items-start justify-between gap-3">
          <div className="flex items-start gap-3">
            <div className="h-10 w-10 rounded-xl bg-primary/10 border border-primary/20 flex items-center justify-center font-bold text-xs text-primary shrink-0 group-hover:scale-105 transition-transform">
              {initials}
            </div>

            <div>
              <h3 className="font-bold text-sm text-foreground group-hover:text-primary transition-colors flex items-center gap-1.5">
                <span>{contact.fullName}</span>
                <ChevronRight className="h-3.5 w-3.5 opacity-0 -translate-x-1 group-hover:opacity-100 group-hover:translate-x-0 transition-all text-muted-foreground" />
              </h3>

              <div className="flex items-center gap-2 text-xs text-muted-foreground mt-0.5 flex-wrap">
                {contact.role && <span>{contact.role}</span>}
                {contact.role && contact.companyName && <span>•</span>}
                {contact.companyName && (
                  <span className="flex items-center gap-1 font-medium text-foreground/80">
                    <Building2 className="h-3 w-3 text-muted-foreground" />
                    {contact.companyName}
                  </span>
                )}
              </div>
            </div>
          </div>

          <WarmthBadge warmth={contact.warmth} daysSince={contact.daysSinceLastContact} size="sm" />
        </div>

        {/* Badges: Type & Linked Applications */}
        <div className="flex items-center gap-2 flex-wrap">
          <span
            className={clsx(
              'px-2 py-0.5 rounded-md text-[10px] font-semibold uppercase tracking-wider border',
              getTypeStyle(contact.type)
            )}
          >
            {contact.type}
          </span>

          {contact.linkedApplicationsCount > 0 && (
            <span className="inline-flex items-center gap-1 px-2 py-0.5 rounded-md text-[11px] font-medium bg-muted text-foreground border border-border">
              <Briefcase className="h-3 w-3 text-muted-foreground" />
              <span>
                {contact.linkedApplicationsCount} {contact.linkedApplicationsCount === 1 ? 'App' : 'Apps'}
              </span>
            </span>
          )}

          {contact.nextFollowUpAt && (
            <span className="inline-flex items-center gap-1 px-2 py-0.5 rounded-md text-[11px] font-semibold bg-indigo-500/10 text-indigo-600 dark:text-indigo-400 border border-indigo-500/20">
              <Calendar className="h-3 w-3" />
              <span>Follow-up {new Date(contact.nextFollowUpAt).toLocaleDateString([], { month: 'short', day: 'numeric' })}</span>
            </span>
          )}
        </div>
      </div>

      {/* Footer row: Communication shortcuts & Log Interaction button */}
      <div className="mt-4 pt-3 border-t border-border flex items-center justify-between gap-2">
        <div className="flex items-center gap-1" onClick={(e) => e.stopPropagation()}>
          {contact.email && (
            <a
              href={`mailto:${contact.email}`}
              className="p-1.5 rounded-lg text-muted-foreground hover:text-primary hover:bg-primary/10 transition-colors"
              title={`Email ${contact.email}`}
            >
              <Mail className="h-4 w-4" />
            </a>
          )}
          {contact.phone && (
            <a
              href={`tel:${contact.phone}`}
              className="p-1.5 rounded-lg text-muted-foreground hover:text-primary hover:bg-primary/10 transition-colors"
              title={`Call ${contact.phone}`}
            >
              <Phone className="h-4 w-4" />
            </a>
          )}
          {contact.linkedInUrl && (
            <a
              href={contact.linkedInUrl}
              target="_blank"
              rel="noreferrer"
              className="p-1.5 rounded-lg text-muted-foreground hover:text-blue-600 hover:bg-blue-500/10 transition-colors"
              title="LinkedIn Profile"
            >
              <Linkedin className="h-4 w-4" />
            </a>
          )}
        </div>

        <button
          type="button"
          onClick={(e) => {
            e.stopPropagation();
            onLogInteraction(contact);
          }}
          className="inline-flex items-center gap-1.5 px-2.5 py-1 text-xs font-semibold text-primary bg-primary/10 hover:bg-primary/20 rounded-lg transition-colors"
        >
          <MessageSquarePlus className="h-3.5 w-3.5" />
          <span>Log Interaction</span>
        </button>
      </div>
    </div>
  );
};
