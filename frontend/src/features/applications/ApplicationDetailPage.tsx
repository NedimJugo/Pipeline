import React, { useState } from 'react';
import { useParams, useNavigate, Link } from 'react-router-dom';
import {
  useApplication,
  useUpdateApplicationStatus,
  useDeleteApplication,
  useDuplicateApplication,
} from './useApplications';
import { ApplicationStatus, UpdateStatusPayload } from './types';
import { StatusStepper } from './components/StatusStepper';
import { JobDescriptionHighlighter } from './components/JobDescriptionHighlighter';
import { ApplicationTimeline } from './components/ApplicationTimeline';
import { EditApplicationModal } from './components/EditApplicationModal';
import { TerminalStatusModal } from './components/TerminalStatusModal';
import {
  ArrowLeft,
  Building2,
  ExternalLink,
  MapPin,
  Calendar,
  DollarSign,
  Copy,
  Trash2,
  Edit3,
  ThumbsUp,
  ThumbsDown,
  Star,
  Clock,
  Sparkles,
  AlertCircle,
  Award,
  CheckCircle,
  XCircle,
} from 'lucide-react';
import { clsx } from 'clsx';

type TabType = 'overview' | 'timeline' | 'job_description' | 'closing_offer';

export const ApplicationDetailPage: React.FC = () => {
  const { id } = useParams<{ id: string }>();
  const navigate = useNavigate();

  const { data: application, isLoading, error } = useApplication(id);
  const updateStatusMutation = useUpdateApplicationStatus();
  const deleteMutation = useDeleteApplication();
  const duplicateMutation = useDuplicateApplication();

  const [activeTab, setActiveTab] = useState<TabType>('overview');
  const [isEditModalOpen, setIsEditModalOpen] = useState(false);
  const [isTerminalModalOpen, setIsTerminalModalOpen] = useState(false);
  const [targetTerminalStatus, setTargetTerminalStatus] = useState<ApplicationStatus>('Rejected');
  const [isDeleteConfirmOpen, setIsDeleteConfirmOpen] = useState(false);

  if (isLoading) {
    return (
      <div className="p-8 max-w-6xl mx-auto space-y-6 animate-pulse">
        <div className="h-6 w-32 bg-muted rounded" />
        <div className="h-24 bg-card border border-border rounded-xl" />
        <div className="h-64 bg-card border border-border rounded-xl" />
      </div>
    );
  }

  if (error || !application) {
    return (
      <div className="p-12 max-w-lg mx-auto text-center space-y-4">
        <AlertCircle className="h-12 w-12 text-destructive mx-auto" />
        <h2 className="text-lg font-bold">Application Not Found</h2>
        <p className="text-xs text-muted-foreground">
          The requested application could not be found or you do not have permission to view it.
        </p>
        <Link
          to="/applications"
          className="inline-flex items-center gap-2 px-4 py-2 text-xs font-semibold bg-primary text-primary-foreground rounded-lg"
        >
          <ArrowLeft className="h-4 w-4" />
          Back to Applications
        </Link>
      </div>
    );
  }

  const isTerminal = ['Rejected', 'Withdrawn', 'Ghosted', 'Declined'].includes(application.status);

  // Status transition handler
  const handleStatusChange = async (newStatus: ApplicationStatus) => {
    if (['Rejected', 'Withdrawn', 'Ghosted', 'Declined'].includes(newStatus)) {
      setTargetTerminalStatus(newStatus);
      setIsTerminalModalOpen(true);
    } else {
      await updateStatusMutation.mutateAsync({
        id: application.id,
        payload: { status: newStatus },
      });
    }
  };

  const handleTerminalSubmit = async (payload: UpdateStatusPayload) => {
    await updateStatusMutation.mutateAsync({
      id: application.id,
      payload,
    });
  };

  const handleDelete = async () => {
    await deleteMutation.mutateAsync(application.id);
    navigate('/applications');
  };

  const handleDuplicate = async () => {
    const duplicated = await duplicateMutation.mutateAsync(application.id);
    navigate(`/applications/${duplicated.id}`);
  };

  // Company Initials badge
  const companyInitials = application.companyName
    .split(' ')
    .filter(Boolean)
    .slice(0, 2)
    .map((word) => word[0].toUpperCase())
    .join('');

  return (
    <div className="space-y-6 max-w-7xl mx-auto pb-12">
      {/* Top breadcrumb & back button */}
      <div className="flex items-center justify-between">
        <Link
          to="/applications"
          className="inline-flex items-center gap-1.5 text-xs font-medium text-muted-foreground hover:text-foreground transition-colors group"
        >
          <ArrowLeft className="h-4 w-4 group-hover:-translate-x-0.5 transition-transform" />
          <span>Back to Applications</span>
        </Link>

        {/* Quick action buttons */}
        <div className="flex items-center gap-2">
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
            onClick={handleDuplicate}
            disabled={duplicateMutation.isPending}
            className="inline-flex items-center gap-1.5 px-3 py-1.5 text-xs font-medium bg-card border border-border rounded-lg hover:bg-muted text-foreground transition-colors shadow-2xs disabled:opacity-50"
          >
            <Copy className="h-3.5 w-3.5 text-muted-foreground" />
            <span>Duplicate</span>
          </button>

          {!isTerminal && (
            <button
              type="button"
              onClick={() => {
                setTargetTerminalStatus('Rejected');
                setIsTerminalModalOpen(true);
              }}
              className="inline-flex items-center gap-1.5 px-3 py-1.5 text-xs font-medium bg-destructive/10 border border-destructive/20 rounded-lg hover:bg-destructive/20 text-destructive transition-colors"
            >
              <XCircle className="h-3.5 w-3.5" />
              <span>Mark Closed</span>
            </button>
          )}

          <button
            type="button"
            onClick={() => setIsDeleteConfirmOpen(true)}
            className="p-1.5 text-muted-foreground hover:text-destructive hover:bg-destructive/10 rounded-lg transition-colors"
            title="Delete Application"
          >
            <Trash2 className="h-4 w-4" />
          </button>
        </div>
      </div>

      {/* Hero Header Card */}
      <div className="bg-card border border-border rounded-xl p-6 shadow-xs space-y-6">
        <div className="flex flex-col md:flex-row md:items-center justify-between gap-4">
          <div className="flex items-start gap-4">
            <div className="h-14 w-14 rounded-xl bg-primary/10 border border-primary/20 flex items-center justify-center font-mono font-bold text-lg text-primary shrink-0 shadow-2xs">
              {companyInitials}
            </div>

            <div>
              <div className="flex items-center gap-2.5 flex-wrap">
                <h1 className="text-xl font-bold tracking-tight text-foreground">
                  {application.roleTitle}
                </h1>
                {application.jobUrl && (
                  <a
                    href={application.jobUrl}
                    target="_blank"
                    rel="noreferrer"
                    className="text-muted-foreground hover:text-primary transition-colors"
                    title="Open Job Posting"
                  >
                    <ExternalLink className="h-4 w-4" />
                  </a>
                )}
              </div>

              <div className="flex items-center gap-3 text-xs text-muted-foreground mt-1 flex-wrap">
                <span className="font-semibold text-foreground flex items-center gap-1">
                  <Building2 className="h-3.5 w-3.5" />
                  {application.companyName}
                </span>

                {application.location && (
                  <span className="flex items-center gap-1">
                    <MapPin className="h-3.5 w-3.5" />
                    {application.location}
                  </span>
                )}

                <span className="px-2 py-0.5 rounded-full bg-muted text-[11px] font-medium border border-border">
                  {application.workMode}
                </span>

                <span className="px-2 py-0.5 rounded-full bg-muted text-[11px] font-medium border border-border">
                  {application.employmentType}
                </span>

                <span className="flex items-center gap-1 font-mono text-[11px]">
                  <Clock className="h-3 w-3 text-muted-foreground" />
                  {application.daysInStage}d in stage
                </span>
              </div>
            </div>
          </div>

          {/* Quick Metrics (Rating & Priority) */}
          <div className="flex items-center gap-4 border-t md:border-t-0 md:border-l border-border pt-3 md:pt-0 md:pl-6 shrink-0">
            <div className="text-center">
              <span className="block text-[10px] font-semibold uppercase tracking-wider text-muted-foreground">
                Priority
              </span>
              <span
                className={clsx(
                  'inline-block px-2 py-0.5 rounded-full text-xs font-semibold mt-0.5',
                  application.priority === 3
                    ? 'bg-rose-500/10 text-rose-600 dark:text-rose-400'
                    : application.priority === 2
                    ? 'bg-blue-500/10 text-blue-600 dark:text-blue-400'
                    : 'bg-muted text-muted-foreground'
                )}
              >
                {application.priority === 3 ? 'High' : application.priority === 2 ? 'Medium' : 'Low'}
              </span>
            </div>

            <div className="text-center">
              <span className="block text-[10px] font-semibold uppercase tracking-wider text-muted-foreground">
                Excitement
              </span>
              <div className="flex items-center justify-center gap-0.5 mt-1">
                {[1, 2, 3, 4, 5].map((star) => (
                  <Star
                    key={star}
                    className={clsx(
                      'h-3.5 w-3.5',
                      star <= application.excitementRating
                        ? 'fill-amber-400 text-amber-400'
                        : 'text-muted-foreground/30'
                    )}
                  />
                ))}
              </div>
            </div>
          </div>
        </div>

        {/* Status Stepper */}
        <div className="pt-4 border-t border-border">
          <div className="flex items-center justify-between mb-1.5">
            <span className="text-[11px] font-bold uppercase tracking-wider text-muted-foreground">
              Pipeline Stage Progression
            </span>
            {isTerminal && (
              <span className="text-xs font-semibold text-destructive">
                Status: {application.status}
              </span>
            )}
          </div>
          <StatusStepper
            currentStatus={application.status}
            onStatusSelect={handleStatusChange}
            disabled={updateStatusMutation.isPending}
          />
        </div>
      </div>

      {/* Tab Navigation */}
      <div className="border-b border-border flex items-center gap-6">
        {[
          { key: 'overview', label: 'Overview' },
          { key: 'timeline', label: 'Timeline & History' },
          { key: 'job_description', label: 'Job Description' },
          { key: 'closing_offer', label: 'Offer & Closure' },
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
        {/* TAB 1: OVERVIEW */}
        {activeTab === 'overview' && (
          <div className="grid grid-cols-1 md:grid-cols-3 gap-6">
            {/* Left 2 Cols: Details, Notes, Pros/Cons */}
            <div className="md:col-span-2 space-y-6">
              {/* Pros & Cons Card */}
              <div className="grid grid-cols-1 sm:grid-cols-2 gap-4">
                <div className="bg-card border border-border rounded-xl p-4 shadow-2xs">
                  <div className="flex items-center gap-1.5 text-xs font-bold uppercase tracking-wider text-emerald-600 dark:text-emerald-400 mb-2">
                    <ThumbsUp className="h-4 w-4" />
                    <span>Pros / Upsides</span>
                  </div>
                  <p className="text-xs text-foreground/80 leading-relaxed whitespace-pre-wrap">
                    {application.pros || (
                      <span className="text-muted-foreground italic">No pros recorded.</span>
                    )}
                  </p>
                </div>

                <div className="bg-card border border-border rounded-xl p-4 shadow-2xs">
                  <div className="flex items-center gap-1.5 text-xs font-bold uppercase tracking-wider text-rose-600 dark:text-rose-400 mb-2">
                    <ThumbsDown className="h-4 w-4" />
                    <span>Cons / Trade-offs</span>
                  </div>
                  <p className="text-xs text-foreground/80 leading-relaxed whitespace-pre-wrap">
                    {application.cons || (
                      <span className="text-muted-foreground italic">No cons recorded.</span>
                    )}
                  </p>
                </div>
              </div>

              {/* Strategy & Notes */}
              <div className="bg-card border border-border rounded-xl p-5 shadow-2xs space-y-2">
                <h3 className="text-xs font-bold uppercase tracking-wider text-muted-foreground flex items-center gap-1.5">
                  <Sparkles className="h-4 w-4 text-primary" />
                  <span>Interview Strategy & Personal Notes</span>
                </h3>
                <p className="text-xs text-foreground/90 leading-relaxed whitespace-pre-wrap font-sans">
                  {application.notes || (
                    <span className="text-muted-foreground italic">
                      No notes recorded yet. Click Edit to add strategy notes, questions to ask, or recruiter points.
                    </span>
                  )}
                </p>
              </div>
            </div>

            {/* Right 1 Col: Metadata Sidebar */}
            <div className="space-y-4">
              <div className="bg-card border border-border rounded-xl p-5 shadow-2xs space-y-4">
                <h3 className="text-xs font-bold uppercase tracking-wider text-muted-foreground">
                  Role Specifics
                </h3>

                <div className="space-y-3 text-xs">
                  <div>
                    <span className="text-muted-foreground block text-[11px]">Salary Range</span>
                    <span className="font-semibold text-foreground flex items-center gap-1 mt-0.5">
                      <DollarSign className="h-3.5 w-3.5 text-muted-foreground" />
                      {application.salaryMin || application.salaryMax ? (
                        <>
                          {application.salaryMin ? `${application.currency} ${application.salaryMin.toLocaleString()}` : ''}
                          {application.salaryMin && application.salaryMax ? ' — ' : ''}
                          {application.salaryMax ? `${application.currency} ${application.salaryMax.toLocaleString()}` : ''}
                        </>
                      ) : (
                        'Not specified'
                      )}
                    </span>
                  </div>

                  <div>
                    <span className="text-muted-foreground block text-[11px]">Application Source</span>
                    <span className="font-medium text-foreground mt-0.5 block">
                      {application.source}
                      {application.sourceDetail ? ` (${application.sourceDetail})` : ''}
                    </span>
                  </div>

                  <div>
                    <span className="text-muted-foreground block text-[11px]">Date Applied</span>
                    <span className="font-medium text-foreground mt-0.5 block font-mono">
                      {application.appliedAt
                        ? new Date(application.appliedAt).toLocaleDateString()
                        : 'Not set'}
                    </span>
                  </div>

                  <div>
                    <span className="text-muted-foreground block text-[11px]">Last Updated</span>
                    <span className="font-medium text-foreground mt-0.5 block font-mono">
                      {new Date(application.updatedAt).toLocaleDateString()}
                    </span>
                  </div>
                </div>
              </div>
            </div>
          </div>
        )}

        {/* TAB 2: TIMELINE */}
        {activeTab === 'timeline' && (
          <div className="max-w-3xl">
            <ApplicationTimeline applicationId={application.id} />
          </div>
        )}

        {/* TAB 3: JOB DESCRIPTION */}
        {activeTab === 'job_description' && (
          <JobDescriptionHighlighter
            jobDescription={application.jobDescription}
            onEditClick={() => setIsEditModalOpen(true)}
          />
        )}

        {/* TAB 4: CLOSING & OFFER */}
        {activeTab === 'closing_offer' && (
          <div className="max-w-2xl space-y-6">
            {/* Offer section */}
            <div className="bg-card border border-border rounded-xl p-5 space-y-4 shadow-2xs">
              <div className="flex items-center gap-2">
                <Award className="h-5 w-5 text-amber-500" />
                <h3 className="text-sm font-bold text-foreground">Offer Details</h3>
              </div>

              {application.offerSalary || application.offerBenefits || application.offerDeadline ? (
                <div className="space-y-3 text-xs">
                  {application.offerSalary && (
                    <div>
                      <span className="text-muted-foreground block text-[11px]">Offered Salary</span>
                      <span className="font-bold text-foreground text-sm">
                        {application.currency} {application.offerSalary.toLocaleString()}
                      </span>
                    </div>
                  )}

                  {application.offerDeadline && (
                    <div>
                      <span className="text-muted-foreground block text-[11px]">Decision Deadline</span>
                      <span className="font-semibold text-foreground font-mono">
                        {new Date(application.offerDeadline).toLocaleDateString()}
                      </span>
                    </div>
                  )}

                  {application.offerBenefits && (
                    <div>
                      <span className="text-muted-foreground block text-[11px]">Benefits Package</span>
                      <p className="text-foreground/90 whitespace-pre-wrap mt-0.5">
                        {application.offerBenefits}
                      </p>
                    </div>
                  )}
                </div>
              ) : (
                <p className="text-xs text-muted-foreground">
                  No offer terms recorded for this application. Once an offer is extended, record compensation, equity, and deadlines here.
                </p>
              )}
            </div>

            {/* Closure section */}
            <div className="bg-card border border-border rounded-xl p-5 space-y-4 shadow-2xs">
              <div className="flex items-center justify-between">
                <div className="flex items-center gap-2">
                  <XCircle className="h-5 w-5 text-destructive" />
                  <h3 className="text-sm font-bold text-foreground">Closure & Retrospective</h3>
                </div>

                <button
                  type="button"
                  onClick={() => {
                    setTargetTerminalStatus(isTerminal ? application.status : 'Rejected');
                    setIsTerminalModalOpen(true);
                  }}
                  className="px-3 py-1 text-xs font-semibold bg-muted hover:bg-muted/80 rounded-md transition-colors"
                >
                  {isTerminal ? 'Update Closure Details' : 'Record Closure'}
                </button>
              </div>

              {isTerminal ? (
                <div className="space-y-3 text-xs">
                  <div>
                    <span className="text-muted-foreground block text-[11px]">Terminal Status</span>
                    <span className="font-semibold text-destructive">{application.status}</span>
                  </div>

                  {application.rejectionStage && (
                    <div>
                      <span className="text-muted-foreground block text-[11px]">Stage Reached</span>
                      <span className="font-semibold text-foreground">{application.rejectionStage}</span>
                    </div>
                  )}

                  {application.closedReason && (
                    <div>
                      <span className="text-muted-foreground block text-[11px]">Primary Reason</span>
                      <span className="font-medium text-foreground">{application.closedReason}</span>
                    </div>
                  )}

                  {application.lessonsLearned && (
                    <div>
                      <span className="text-muted-foreground block text-[11px]">Lessons Learned</span>
                      <p className="text-foreground/90 bg-muted/30 p-3 rounded-lg border border-border mt-1 whitespace-pre-wrap">
                        {application.lessonsLearned}
                      </p>
                    </div>
                  )}
                </div>
              ) : (
                <p className="text-xs text-muted-foreground">
                  This application is currently active. If it concludes or you decide to withdraw, capture feedback and lessons learned to feed your retrospective analytics.
                </p>
              )}
            </div>
          </div>
        )}
      </div>

      {/* Edit Modal */}
      {isEditModalOpen && (
        <EditApplicationModal
          isOpen={isEditModalOpen}
          onClose={() => setIsEditModalOpen(false)}
          application={application}
        />
      )}

      {/* Terminal Status Modal */}
      {isTerminalModalOpen && (
        <TerminalStatusModal
          isOpen={isTerminalModalOpen}
          onClose={() => setIsTerminalModalOpen(false)}
          targetStatus={targetTerminalStatus}
          onSubmit={handleTerminalSubmit}
          isSubmitting={updateStatusMutation.isPending}
        />
      )}

      {/* Delete Confirmation Dialog */}
      {isDeleteConfirmOpen && (
        <div className="fixed inset-0 z-50 flex items-center justify-center p-4 bg-background/80 backdrop-blur-sm animate-in fade-in duration-200">
          <div className="border border-border bg-card max-w-sm w-full rounded-xl shadow-2xl p-6 space-y-4">
            <h3 className="font-bold text-sm text-foreground">Delete Application</h3>
            <p className="text-xs text-muted-foreground">
              Are you sure you want to delete this application for{' '}
              <strong className="text-foreground">{application.roleTitle}</strong> at{' '}
              <strong className="text-foreground">{application.companyName}</strong>? This action can be undone from trash.
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
