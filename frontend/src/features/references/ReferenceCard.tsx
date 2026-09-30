import React, { useState } from 'react';
import {
  Building2,
  Mail,
  Phone,
  CheckCircle2,
  Clock,
  XCircle,
  HelpCircle,
  Share2,
  Bell,
  MoreVertical,
  Edit2,
  Trash2,
  Briefcase,
} from 'lucide-react';
import { ReferenceListItem, ReferenceConsent } from './types';
import { useDeleteReference } from './useReferences';

interface ReferenceCardProps {
  reference: ReferenceListItem;
  onEdit: (ref: ReferenceListItem) => void;
  onShare: (ref: ReferenceListItem) => void;
  onNotify: (ref: ReferenceListItem) => void;
}

export const ReferenceCard: React.FC<ReferenceCardProps> = ({
  reference,
  onEdit,
  onShare,
  onNotify,
}) => {
  const [isMenuOpen, setIsMenuOpen] = useState(false);
  const deleteMutation = useDeleteReference();

  const getConsentBadge = (consent: ReferenceConsent) => {
    switch (consent) {
      case 'Agreed':
        return (
          <span className="inline-flex items-center gap-1 px-2.5 py-0.5 rounded-full text-xs font-semibold bg-emerald-500/10 text-emerald-500 border border-emerald-500/20">
            <CheckCircle2 className="h-3 w-3" />
            <span>Agreed</span>
          </span>
        );
      case 'Asked':
        return (
          <span className="inline-flex items-center gap-1 px-2.5 py-0.5 rounded-full text-xs font-semibold bg-amber-500/10 text-amber-500 border border-amber-500/20">
            <Clock className="h-3 w-3" />
            <span>Asked</span>
          </span>
        );
      case 'Declined':
        return (
          <span className="inline-flex items-center gap-1 px-2.5 py-0.5 rounded-full text-xs font-semibold bg-destructive/10 text-destructive border border-destructive/20">
            <XCircle className="h-3 w-3" />
            <span>Declined</span>
          </span>
        );
      case 'NotAsked':
      default:
        return (
          <span className="inline-flex items-center gap-1 px-2.5 py-0.5 rounded-full text-xs font-semibold bg-muted text-muted-foreground border border-border">
            <HelpCircle className="h-3 w-3" />
            <span>Not Asked</span>
          </span>
        );
    }
  };

  const getInitials = (name: string) => {
    return name
      .split(' ')
      .map((n) => n[0])
      .filter(Boolean)
      .slice(0, 2)
      .join('')
      .toUpperCase();
  };

  const handleDelete = async () => {
    if (confirm(`Are you sure you want to delete reference "${reference.fullName}"?`)) {
      await deleteMutation.mutateAsync(reference.id);
    }
  };

  return (
    <div className="bg-card border border-border rounded-xl p-5 shadow-xs hover:border-primary/40 transition-all flex flex-col justify-between gap-4 group">
      <div>
        <div className="flex items-start justify-between gap-3">
          <div className="flex items-center gap-3">
            <div className="h-10 w-10 rounded-xl bg-primary/10 text-primary font-bold text-sm flex items-center justify-center shrink-0">
              {getInitials(reference.fullName)}
            </div>
            <div>
              <h3 className="font-bold text-foreground text-sm tracking-tight group-hover:text-primary transition-colors">
                {reference.fullName}
              </h3>
              <p className="text-xs text-muted-foreground flex items-center gap-1 mt-0.5">
                <span>{reference.relationship}</span>
                {reference.company && (
                  <>
                    <span>•</span>
                    <span className="inline-flex items-center gap-1">
                      <Building2 className="h-3 w-3" />
                      {reference.company}
                    </span>
                  </>
                )}
              </p>
            </div>
          </div>

          <div className="flex items-center gap-2">
            {getConsentBadge(reference.consent)}

            <div className="relative">
              <button
                onClick={() => setIsMenuOpen(!isMenuOpen)}
                className="p-1 text-muted-foreground hover:text-foreground rounded-md hover:bg-muted transition-colors"
                aria-label="Options"
              >
                <MoreVertical className="h-4 w-4" />
              </button>

              {isMenuOpen && (
                <>
                  <div className="fixed inset-0 z-10" onClick={() => setIsMenuOpen(false)} />
                  <div className="absolute right-0 mt-1 w-32 bg-card border border-border rounded-lg shadow-xl py-1 z-20 text-xs animate-in fade-in duration-100">
                    <button
                      onClick={() => {
                        setIsMenuOpen(false);
                        onEdit(reference);
                      }}
                      className="w-full px-3 py-1.5 text-left hover:bg-muted flex items-center gap-2 text-foreground font-medium"
                    >
                      <Edit2 className="h-3.5 w-3.5" />
                      <span>Edit</span>
                    </button>
                    <button
                      onClick={() => {
                        setIsMenuOpen(false);
                        handleDelete();
                      }}
                      className="w-full px-3 py-1.5 text-left hover:bg-destructive/10 text-destructive flex items-center gap-2 font-medium"
                    >
                      <Trash2 className="h-3.5 w-3.5" />
                      <span>Delete</span>
                    </button>
                  </div>
                </>
              )}
            </div>
          </div>
        </div>

        {/* Contact details */}
        <div className="mt-3.5 space-y-1.5 text-xs text-muted-foreground">
          {reference.email && (
            <div className="flex items-center gap-2 truncate">
              <Mail className="h-3.5 w-3.5 shrink-0" />
              <a href={`mailto:${reference.email}`} className="hover:underline text-foreground/80">
                {reference.email}
              </a>
            </div>
          )}
          {reference.phone && (
            <div className="flex items-center gap-2 truncate">
              <Phone className="h-3.5 w-3.5 shrink-0" />
              <span>{reference.phone}</span>
            </div>
          )}
          {reference.notes && (
            <p className="text-xs text-muted-foreground/80 line-clamp-2 mt-2 italic bg-muted/30 p-2 rounded-lg border border-border/50">
              "{reference.notes}"
            </p>
          )}
        </div>
      </div>

      {/* Footer and quick actions */}
      <div className="pt-3 border-t border-border flex items-center justify-between gap-2 text-xs">
        <div className="flex items-center gap-2 text-muted-foreground">
          <span className="inline-flex items-center gap-1 font-medium bg-muted px-2 py-0.5 rounded-md">
            <Briefcase className="h-3 w-3" />
            {reference.sharedCount} {reference.sharedCount === 1 ? 'app' : 'apps'}
          </span>
          {reference.lastNotifiedAt ? (
            <span className="text-[11px] text-muted-foreground">
              Notified {new Date(reference.lastNotifiedAt).toLocaleDateString()}
            </span>
          ) : (
            <span className="text-[11px] text-muted-foreground/60">Not notified</span>
          )}
        </div>

        <div className="flex items-center gap-1.5">
          <button
            onClick={() => onNotify(reference)}
            className="inline-flex items-center gap-1 px-2.5 py-1 text-xs font-semibold rounded-lg bg-secondary text-secondary-foreground hover:bg-secondary/80 transition-colors shadow-2xs"
            title="Send pre-filled heads-up email template"
          >
            <Bell className="h-3 w-3" />
            <span>Notify</span>
          </button>
          <button
            onClick={() => onShare(reference)}
            className="inline-flex items-center gap-1 px-2.5 py-1 text-xs font-semibold rounded-lg bg-primary/10 text-primary hover:bg-primary/20 transition-colors"
            title="Log sharing with an application"
          >
            <Share2 className="h-3 w-3" />
            <span>Share</span>
          </button>
        </div>
      </div>
    </div>
  );
};
