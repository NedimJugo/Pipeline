import React, { useState } from 'react';
import { AlertCircle, Ghost, Mail, ArrowRight, Check } from 'lucide-react';
import { Link } from 'react-router-dom';
import { StaleApplication } from './types';
import { useUpdateApplicationStatus } from '@/features/applications/useApplications';
import { EmailTemplatePickerModal } from '@/features/templates/EmailTemplatePickerModal';

interface StaleApplicationsWidgetProps {
  applications: StaleApplication[];
}

export const StaleApplicationsWidget: React.FC<StaleApplicationsWidgetProps> = ({
  applications,
}) => {
  const [selectedAppId, setSelectedAppId] = useState<string | null>(null);
  const [isTemplateModalOpen, setIsTemplateModalOpen] = useState(false);

  const updateStatusMutation = useUpdateApplicationStatus();

  const handleMarkGhosted = async (id: string, company: string) => {
    if (window.confirm(`Mark ${company} application as Ghosted?`)) {
      await updateStatusMutation.mutateAsync({
        id,
        payload: {
          status: 'Ghosted',
          rejectionStage: 'No response / Ghosted after application',
          lessonsLearned: 'Candidate followed up but received no response.',
        },
      });
    }
  };

  const handleOpenFollowUp = (id: string) => {
    setSelectedAppId(id);
    setIsTemplateModalOpen(true);
  };

  if (applications.length === 0) {
    return null;
  }

  return (
    <div className="rounded-xl border border-amber-500/20 bg-amber-500/5 p-5 space-y-4">
      <div className="flex items-center justify-between">
        <div className="flex items-center gap-2">
          <AlertCircle className="h-4 w-4 text-amber-500 shrink-0" />
          <h3 className="font-bold text-sm text-foreground">
            Stale Applications ({applications.length})
          </h3>
        </div>
        <span className="text-xs text-muted-foreground">
          No activity for over the stale threshold
        </span>
      </div>

      <div className="grid grid-cols-1 md:grid-cols-2 gap-3">
        {applications.map((app) => (
          <div
            key={app.id}
            className="p-3.5 rounded-lg border border-border bg-card shadow-2xs flex flex-col justify-between gap-3"
          >
            <div>
              <div className="flex items-start justify-between gap-2">
                <div>
                  <h4 className="font-bold text-xs text-foreground">{app.roleTitle}</h4>
                  <p className="text-xs text-muted-foreground font-medium">{app.companyName}</p>
                </div>
                <span className="px-2 py-0.5 rounded text-[10px] font-bold bg-amber-500/10 text-amber-600 dark:text-amber-400 border border-amber-500/20 shrink-0">
                  {app.daysSinceUpdate}d inactive
                </span>
              </div>
            </div>

            <div className="flex items-center justify-end gap-2 pt-2 border-t border-border/50">
              <button
                type="button"
                onClick={() => handleOpenFollowUp(app.id)}
                className="inline-flex items-center gap-1 px-2.5 py-1 text-xs font-semibold bg-primary/10 border border-primary/20 text-primary rounded-lg hover:bg-primary/20 transition-colors"
              >
                <Mail className="h-3 w-3" />
                <span>Follow Up</span>
              </button>

              <button
                type="button"
                onClick={() => handleMarkGhosted(app.id, app.companyName)}
                disabled={updateStatusMutation.isPending}
                className="inline-flex items-center gap-1 px-2.5 py-1 text-xs font-semibold bg-muted text-muted-foreground hover:text-foreground hover:bg-muted/80 rounded-lg transition-colors"
                title="Mark as Ghosted"
              >
                <Ghost className="h-3 w-3" />
                <span>Mark Ghosted</span>
              </button>
            </div>
          </div>
        ))}
      </div>

      {/* Follow-up template modal */}
      <EmailTemplatePickerModal
        isOpen={isTemplateModalOpen}
        onClose={() => setIsTemplateModalOpen(false)}
        applicationId={selectedAppId}
        defaultCategory="FollowUp"
      />
    </div>
  );
};
