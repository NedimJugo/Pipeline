import React, { useState } from 'react';
import {
  CreateInterviewPayload,
  InterviewType,
  InterviewFormat,
} from '../types';
import { useCreateInterview } from '../useInterviews';
import { useApplications } from '@/features/applications/useApplications';
import { useContacts } from '@/features/contacts/useContacts';
import {
  X,
  Calendar,
  Clock,
  Video,
  Building2,
  Users,
  Link as LinkIcon,
  FileText,
} from 'lucide-react';

interface ScheduleInterviewModalProps {
  isOpen: boolean;
  onClose: () => void;
  preselectedApplicationId?: string;
  preselectedRoleTitle?: string;
}

export const ScheduleInterviewModal: React.FC<ScheduleInterviewModalProps> = ({
  isOpen,
  onClose,
  preselectedApplicationId,
  preselectedRoleTitle,
}) => {
  const [applicationId, setApplicationId] = useState(preselectedApplicationId || '');
  const [type, setType] = useState<InterviewType>('Technical');
  const [format, setFormat] = useState<InterviewFormat>('Video');
  const [scheduledAt, setScheduledAt] = useState<string>(
    new Date(Date.now() + 86400000).toISOString().slice(0, 16)
  );
  const [durationMinutes, setDurationMinutes] = useState(45);
  const [meetingLink, setMeetingLink] = useState('');
  const [location, setLocation] = useState('');
  const [selectedInterviewerIds, setSelectedInterviewerIds] = useState<string[]>([]);
  const [prepNotes, setPrepNotes] = useState('');

  const { data: applications } = useApplications();
  const { data: contacts } = useContacts();
  const createMutation = useCreateInterview();

  if (!isOpen) return null;

  const handleInterviewerToggle = (contactId: string) => {
    setSelectedInterviewerIds((prev) =>
      prev.includes(contactId)
        ? prev.filter((id) => id !== contactId)
        : [...prev, contactId]
    );
  };

  const handleSubmit = async (e: React.FormEvent) => {
    e.preventDefault();
    if (!applicationId) return;

    const payload: CreateInterviewPayload = {
      applicationId,
      type,
      format,
      scheduledAt: new Date(scheduledAt).toISOString(),
      durationMinutes,
      meetingLink: meetingLink.trim() || undefined,
      location: location.trim() || undefined,
      interviewerContactIds: selectedInterviewerIds.length > 0 ? selectedInterviewerIds : undefined,
      prepNotes: prepNotes.trim() || undefined,
    };

    await createMutation.mutateAsync(payload);
    onClose();
  };

  return (
    <div className="fixed inset-0 z-50 flex items-center justify-center p-4 bg-background/80 backdrop-blur-sm animate-in fade-in duration-200">
      <div className="border border-border bg-card max-w-lg w-full rounded-xl shadow-2xl overflow-hidden flex flex-col max-h-[90vh]">
        {/* Header */}
        <div className="px-6 py-4 border-b border-border flex items-center justify-between">
          <div className="flex items-center gap-2">
            <Calendar className="h-5 w-5 text-primary" />
            <div>
              <h2 className="font-bold text-base tracking-tight text-foreground">
                Schedule Interview
              </h2>
              {preselectedRoleTitle && (
                <p className="text-xs text-muted-foreground truncate max-w-xs">
                  For {preselectedRoleTitle}
                </p>
              )}
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

        {/* Form Body */}
        <form onSubmit={handleSubmit} className="flex-1 overflow-y-auto p-6 space-y-4">
          {/* Application Selection (if not preselected) */}
          {!preselectedApplicationId && (
            <div>
              <label className="block text-xs font-semibold uppercase tracking-wider text-muted-foreground mb-1">
                Target Application *
              </label>
              <div className="relative">
                <Building2 className="h-4 w-4 absolute left-3 top-2.5 text-muted-foreground" />
                <select
                  required
                  value={applicationId}
                  onChange={(e) => setApplicationId(e.target.value)}
                  className="w-full pl-9 pr-3 py-1.5 text-xs bg-background border border-border rounded-lg focus:outline-none focus:ring-2 focus:ring-primary"
                >
                  <option value="">Select an application...</option>
                  {applications?.map((app) => (
                    <option key={app.id} value={app.id}>
                      {app.companyName} — {app.roleTitle}
                    </option>
                  ))}
                </select>
              </div>
            </div>
          )}

          {/* Round Type & Format */}
          <div className="grid grid-cols-1 sm:grid-cols-2 gap-4">
            <div>
              <label className="block text-xs font-semibold uppercase tracking-wider text-muted-foreground mb-1">
                Interview Round *
              </label>
              <select
                value={type}
                onChange={(e) => setType(e.target.value as InterviewType)}
                className="w-full px-3 py-1.5 text-xs bg-background border border-border rounded-lg focus:outline-none focus:ring-2 focus:ring-primary"
              >
                <option value="HR">HR / Recruiter Screen</option>
                <option value="Technical">Technical / Coding / Architecture</option>
                <option value="Culture">Culture / Behavioral</option>
                <option value="Manager">Hiring Manager</option>
                <option value="Final">Final / Executive</option>
                <option value="Assignment">Take-Home / Assignment Review</option>
                <option value="Other">Other</option>
              </select>
            </div>

            <div>
              <label className="block text-xs font-semibold uppercase tracking-wider text-muted-foreground mb-1">
                Format *
              </label>
              <select
                value={format}
                onChange={(e) => setFormat(e.target.value as InterviewFormat)}
                className="w-full px-3 py-1.5 text-xs bg-background border border-border rounded-lg focus:outline-none focus:ring-2 focus:ring-primary"
              >
                <option value="Video">Video Call (Google Meet, Zoom, Teams)</option>
                <option value="Phone">Phone Call</option>
                <option value="Onsite">Onsite / In-Person</option>
              </select>
            </div>
          </div>

          {/* Scheduled Date & Duration */}
          <div className="grid grid-cols-1 sm:grid-cols-2 gap-4">
            <div>
              <label className="block text-xs font-semibold uppercase tracking-wider text-muted-foreground mb-1">
                Date & Time *
              </label>
              <input
                type="datetime-local"
                required
                value={scheduledAt}
                onChange={(e) => setScheduledAt(e.target.value)}
                className="w-full px-3 py-1.5 text-xs bg-background border border-border rounded-lg focus:outline-none focus:ring-2 focus:ring-primary font-mono"
              />
            </div>

            <div>
              <label className="block text-xs font-semibold uppercase tracking-wider text-muted-foreground mb-1">
                Duration
              </label>
              <div className="relative">
                <Clock className="h-4 w-4 absolute left-3 top-2.5 text-muted-foreground" />
                <select
                  value={durationMinutes}
                  onChange={(e) => setDurationMinutes(parseInt(e.target.value, 10))}
                  className="w-full pl-9 pr-3 py-1.5 text-xs bg-background border border-border rounded-lg focus:outline-none focus:ring-2 focus:ring-primary font-mono"
                >
                  <option value={15}>15 minutes</option>
                  <option value={30}>30 minutes</option>
                  <option value={45}>45 minutes</option>
                  <option value={60}>60 minutes</option>
                  <option value={90}>90 minutes</option>
                  <option value={120}>2 hours</option>
                </select>
              </div>
            </div>
          </div>

          {/* Location or Meeting Link */}
          {format === 'Video' ? (
            <div>
              <label className="block text-xs font-semibold uppercase tracking-wider text-muted-foreground mb-1">
                Meeting Link (Zoom, Google Meet, Teams)
              </label>
              <div className="relative">
                <Video className="h-4 w-4 absolute left-3 top-2.5 text-muted-foreground" />
                <input
                  type="url"
                  placeholder="https://meet.google.com/xyz or https://zoom.us/j/..."
                  value={meetingLink}
                  onChange={(e) => setMeetingLink(e.target.value)}
                  className="w-full pl-9 pr-3 py-1.5 text-xs bg-background border border-border rounded-lg focus:outline-none focus:ring-2 focus:ring-primary"
                />
              </div>
            </div>
          ) : (
            <div>
              <label className="block text-xs font-semibold uppercase tracking-wider text-muted-foreground mb-1">
                Location or Phone Number
              </label>
              <input
                type="text"
                placeholder={format === 'Phone' ? '+1 (555) 000-0000' : 'Company Office Address'}
                value={location}
                onChange={(e) => setLocation(e.target.value)}
                className="w-full px-3 py-1.5 text-xs bg-background border border-border rounded-lg focus:outline-none focus:ring-2 focus:ring-primary"
              />
            </div>
          )}

          {/* Interviewers Multi-Select */}
          {contacts && contacts.length > 0 && (
            <div>
              <label className="block text-xs font-semibold uppercase tracking-wider text-muted-foreground mb-1.5">
                Interviewers / Panel (Optional)
              </label>
              <div className="max-h-36 overflow-y-auto border border-border rounded-lg divide-y divide-border bg-background p-1 space-y-1">
                {contacts.map((contact) => {
                  const isChecked = selectedInterviewerIds.includes(contact.id);
                  return (
                    <label
                      key={contact.id}
                      className="flex items-center gap-2 p-1.5 rounded hover:bg-muted/50 cursor-pointer text-xs"
                    >
                      <input
                        type="checkbox"
                        checked={isChecked}
                        onChange={() => handleInterviewerToggle(contact.id)}
                        className="rounded border-border text-primary focus:ring-primary h-3.5 w-3.5"
                      />
                      <span className="font-semibold text-foreground">{contact.fullName}</span>
                      <span className="text-muted-foreground text-[11px]">
                        ({contact.role || contact.type}{contact.companyName ? ` • ${contact.companyName}` : ''})
                      </span>
                    </label>
                  );
                })}
              </div>
            </div>
          )}

          {/* Prep Notes */}
          <div>
            <label className="block text-xs font-semibold uppercase tracking-wider text-muted-foreground mb-1">
              Initial Prep Notes (Optional)
            </label>
            <textarea
              rows={2}
              placeholder="Focus areas, topics to emphasize, or panel background..."
              value={prepNotes}
              onChange={(e) => setPrepNotes(e.target.value)}
              className="w-full px-3 py-1.5 text-xs bg-background border border-border rounded-lg focus:outline-none focus:ring-2 focus:ring-primary resize-none"
            />
          </div>

          {/* Footer Actions */}
          <div className="flex items-center justify-end gap-2 pt-3 border-t border-border">
            <button
              type="button"
              onClick={onClose}
              disabled={createMutation.isPending}
              className="px-3.5 py-1.5 text-xs font-medium text-muted-foreground hover:text-foreground hover:bg-muted rounded-lg transition-colors"
            >
              Cancel
            </button>
            <button
              type="submit"
              disabled={createMutation.isPending || !applicationId}
              className="px-4 py-1.5 text-xs font-semibold bg-primary text-primary-foreground rounded-lg hover:opacity-90 shadow-sm transition-opacity disabled:opacity-50"
            >
              {createMutation.isPending ? 'Scheduling...' : 'Schedule Interview'}
            </button>
          </div>
        </form>
      </div>
    </div>
  );
};
