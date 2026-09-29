import React, { useState } from 'react';
import { useApplicationInterviews } from '@/features/interviews/useInterviews';
import { InterviewCard } from '@/features/interviews/components/InterviewCard';
import { ScheduleInterviewModal } from '@/features/interviews/components/ScheduleInterviewModal';
import { Calendar, Plus, AlertCircle, Video } from 'lucide-react';

interface ApplicationInterviewsTabProps {
  applicationId: string;
  roleTitle: string;
  companyName: string;
}

export const ApplicationInterviewsTab: React.FC<ApplicationInterviewsTabProps> = ({
  applicationId,
  roleTitle,
  companyName,
}) => {
  const { data: interviews, isLoading, error } = useApplicationInterviews(applicationId);
  const [isScheduleModalOpen, setIsScheduleModalOpen] = useState(false);

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
        <h3 className="text-sm font-bold text-foreground">Failed to load interviews</h3>
        <p className="text-xs text-muted-foreground">
          There was an error loading the interviews for this application.
        </p>
      </div>
    );
  }

  const interviewList = interviews || [];

  return (
    <div className="space-y-6">
      {/* Header Controls */}
      <div className="flex flex-col sm:flex-row sm:items-center justify-between gap-3 bg-card border border-border rounded-xl p-4 shadow-2xs">
        <div className="flex items-center gap-2.5">
          <div className="p-2 bg-primary/10 rounded-lg text-primary">
            <Calendar className="h-5 w-5" />
          </div>
          <div>
            <div className="flex items-center gap-2">
              <h2 className="text-sm font-bold text-foreground">Interview Rounds</h2>
              <span className="px-2 py-0.5 text-[11px] font-mono font-semibold bg-muted text-muted-foreground rounded-full border border-border">
                {interviewList.length}
              </span>
            </div>
            <p className="text-xs text-muted-foreground">
              Prepare checklists, record questions, and track debriefs for each round.
            </p>
          </div>
        </div>

        <button
          type="button"
          onClick={() => setIsScheduleModalOpen(true)}
          className="inline-flex items-center gap-1.5 px-3.5 py-1.5 text-xs font-semibold bg-primary text-primary-foreground hover:opacity-90 rounded-lg shadow-sm transition-opacity self-start sm:self-auto"
        >
          <Plus className="h-3.5 w-3.5" />
          <span>Schedule Interview</span>
        </button>
      </div>

      {/* Interview List or Empty State */}
      {interviewList.length === 0 ? (
        <div className="bg-card border border-dashed border-border rounded-xl p-10 text-center space-y-4">
          <div className="w-12 h-12 bg-muted/50 rounded-xl flex items-center justify-center mx-auto text-muted-foreground">
            <Video className="h-6 w-6" />
          </div>
          <div className="max-w-md mx-auto space-y-1">
            <h3 className="text-sm font-bold text-foreground">No interviews scheduled yet</h3>
            <p className="text-xs text-muted-foreground leading-relaxed">
              When an employer invites you to an initial screen, technical challenge, or hiring manager loop,
              schedule it here to automatically unlock prep checklists and calendar exports.
            </p>
          </div>
          <div className="pt-2">
            <button
              type="button"
              onClick={() => setIsScheduleModalOpen(true)}
              className="inline-flex items-center gap-1.5 px-4 py-2 text-xs font-semibold bg-primary text-primary-foreground rounded-lg hover:opacity-90 transition-opacity shadow-sm"
            >
              <Plus className="h-4 w-4" />
              <span>Schedule First Interview</span>
            </button>
          </div>
        </div>
      ) : (
        <div className="grid grid-cols-1 md:grid-cols-2 gap-4">
          {interviewList.map((interview) => (
            <InterviewCard key={interview.id} interview={interview} />
          ))}
        </div>
      )}

      {/* Schedule Interview Modal */}
      {isScheduleModalOpen && (
        <ScheduleInterviewModal
          isOpen={isScheduleModalOpen}
          onClose={() => setIsScheduleModalOpen(false)}
          preselectedApplicationId={applicationId}
          preselectedRoleTitle={`${roleTitle} at ${companyName}`}
        />
      )}
    </div>
  );
};
