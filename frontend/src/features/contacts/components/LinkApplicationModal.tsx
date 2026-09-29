import React, { useState } from 'react';
import { useApplications } from '@/features/applications/useApplications';
import { useLinkApplicationContact } from '../useContacts';
import { X, Briefcase } from 'lucide-react';

interface LinkApplicationModalProps {
  isOpen: boolean;
  onClose: () => void;
  contactId: string;
}

export const LinkApplicationModal: React.FC<LinkApplicationModalProps> = ({
  isOpen,
  onClose,
  contactId,
}) => {
  const [applicationId, setApplicationId] = useState('');
  const [roleInProcess, setRoleInProcess] = useState('');

  const { data: applications } = useApplications();
  const linkMutation = useLinkApplicationContact();

  if (!isOpen) return null;

  const handleSubmit = async (e: React.FormEvent) => {
    e.preventDefault();
    if (!applicationId) return;

    await linkMutation.mutateAsync({
      contactId,
      payload: {
        applicationId,
        roleInProcess: roleInProcess.trim() || undefined,
      },
    });

    onClose();
  };

  return (
    <div className="fixed inset-0 z-50 flex items-center justify-center p-4 bg-background/80 backdrop-blur-sm animate-in fade-in duration-200">
      <div className="border border-border bg-card max-w-md w-full rounded-xl shadow-2xl overflow-hidden flex flex-col">
        {/* Header */}
        <div className="px-6 py-4 border-b border-border flex items-center justify-between">
          <div className="flex items-center gap-2">
            <Briefcase className="h-5 w-5 text-primary" />
            <h2 className="font-bold text-base tracking-tight text-foreground">Link Application</h2>
          </div>
          <button
            type="button"
            onClick={onClose}
            className="p-1 rounded-lg text-muted-foreground hover:text-foreground hover:bg-muted"
          >
            <X className="h-4 w-4" />
          </button>
        </div>

        {/* Form Body */}
        <form onSubmit={handleSubmit} className="p-6 space-y-4">
          <div>
            <label className="block text-xs font-semibold uppercase tracking-wider text-muted-foreground mb-1">
              Select Application *
            </label>
            <select
              required
              value={applicationId}
              onChange={(e) => setApplicationId(e.target.value)}
              className="w-full px-3 py-1.5 text-xs bg-background border border-border rounded-lg focus:outline-none focus:ring-2 focus:ring-primary"
            >
              <option value="">Select an active application...</option>
              {applications?.map((app) => (
                <option key={app.id} value={app.id}>
                  {app.companyName} — {app.roleTitle} ({app.status})
                </option>
              ))}
            </select>
          </div>

          <div>
            <label className="block text-xs font-semibold uppercase tracking-wider text-muted-foreground mb-1">
              Role in Process (Optional)
            </label>
            <input
              type="text"
              placeholder="e.g. Primary Recruiter, Hiring Manager, Technical Screener"
              value={roleInProcess}
              onChange={(e) => setRoleInProcess(e.target.value)}
              className="w-full px-3 py-1.5 text-xs bg-background border border-border rounded-lg focus:outline-none focus:ring-2 focus:ring-primary"
            />
          </div>

          {/* Footer */}
          <div className="flex items-center justify-end gap-2 pt-3 border-t border-border">
            <button
              type="button"
              onClick={onClose}
              disabled={linkMutation.isPending}
              className="px-3.5 py-1.5 text-xs font-medium text-muted-foreground hover:text-foreground hover:bg-muted rounded-lg"
            >
              Cancel
            </button>
            <button
              type="submit"
              disabled={linkMutation.isPending || !applicationId}
              className="px-4 py-1.5 text-xs font-semibold bg-primary text-primary-foreground rounded-lg hover:opacity-90 disabled:opacity-50"
            >
              {linkMutation.isPending ? 'Linking...' : 'Link to Application'}
            </button>
          </div>
        </form>
      </div>
    </div>
  );
};
