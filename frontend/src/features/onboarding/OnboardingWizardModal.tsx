import React, { useState } from 'react';
import {
  Briefcase,
  Building2,
  DollarSign,
  Calendar,
  Sparkles,
  ArrowRight,
  ArrowLeft,
  CheckCircle2,
  Globe,
  Sliders,
  Check,
} from 'lucide-react';
import { api } from '@/lib/api-client';
import { useCreateApplication } from '@/features/applications/useApplications';
import { ApplicationStatus, WorkMode } from '@/features/applications/types';
import { AppTourModal } from './AppTourModal';

interface OnboardingWizardModalProps {
  isOpen: boolean;
  onComplete: () => void;
}

export const OnboardingWizardModal: React.FC<OnboardingWizardModalProps> = ({
  isOpen,
  onComplete,
}) => {
  const [step, setStep] = useState<1 | 2 | 3>(1);
  const [isSubmitting, setIsSubmitting] = useState(false);

  // Step 1: Preferences state
  const [targetRole, setTargetRole] = useState('');
  const [workMode, setWorkMode] = useState<WorkMode>('Remote');
  const [salaryMin, setSalaryMin] = useState('');
  const [currency, setCurrency] = useState('USD');
  const [searchStatus, setSearchStatus] = useState<'Active' | 'Interviewing' | 'CasuallyLooking'>('Active');

  // Step 3: First Application state
  const todayStr = new Date().toISOString().split('T')[0];
  const [companyName, setCompanyName] = useState('');
  const [roleTitle, setRoleTitle] = useState('');
  const [appStatus, setAppStatus] = useState<ApplicationStatus>('Applied');
  const [appliedAt, setAppliedAt] = useState<string>(todayStr);
  const [dateError, setDateError] = useState<string | null>(null);

  const createApplicationMutation = useCreateApplication();

  if (!isOpen) return null;

  const handleSavePreferencesAndNext = async (e?: React.FormEvent) => {
    if (e) e.preventDefault();
    setIsSubmitting(true);
    try {
      await api.put('/api/me', {
        targetRole: targetRole.trim() || undefined,
        salaryExpectationMin: salaryMin ? parseFloat(salaryMin) : undefined,
        currency,
        searchStatus,
      });
    } catch {
      // Continue even if network error occurs locally
    } finally {
      setIsSubmitting(false);
      setStep(2);
    }
  };

  const handleFinishOnboarding = async () => {
    setIsSubmitting(true);
    try {
      await api.post('/api/me/complete-onboarding');
    } catch {
      // Ignore fallback
    } finally {
      setIsSubmitting(false);
      onComplete();
    }
  };

  const handleSaveFirstApplication = async (e: React.FormEvent) => {
    e.preventDefault();
    if (!companyName.trim() || !roleTitle.trim()) return;

    if (appliedAt && appliedAt > todayStr) {
      setDateError('Application date cannot be in the future.');
      return;
    }

    setIsSubmitting(true);
    try {
      await createApplicationMutation.mutateAsync({
        companyName: companyName.trim(),
        roleTitle: roleTitle.trim(),
        status: appStatus,
        appliedAt: appliedAt ? new Date(appliedAt).toISOString() : undefined,
        workMode,
        currency,
      });
      await handleFinishOnboarding();
    } catch {
      setIsSubmitting(false);
    }
  };

  return (
    <div className="fixed inset-0 z-50 flex items-center justify-center p-4 bg-background/85 backdrop-blur-md animate-in fade-in duration-200">
      <div className="border border-border bg-card max-w-xl w-full rounded-2xl shadow-2xl overflow-hidden flex flex-col max-h-[92vh]">
        {/* Wizard Header */}
        <div className="px-6 py-5 border-b border-border bg-muted/20">
          <div className="flex items-center justify-between mb-3">
            <div className="flex items-center gap-2">
              <img src="/app_icon.png" alt="Pipeline" className="h-7 w-7 rounded-lg object-contain shadow-xs" />
              <span className="font-bold text-base tracking-tight">Welcome to Pipeline</span>
            </div>
            <span className="text-xs font-mono font-medium text-muted-foreground">
              Step {step} of 3
            </span>
          </div>

          {/* Stepper Progress Bar */}
          <div className="grid grid-cols-3 gap-2">
            <div className={`h-1.5 rounded-full transition-all ${step >= 1 ? 'bg-primary' : 'bg-muted'}`} />
            <div className={`h-1.5 rounded-full transition-all ${step >= 2 ? 'bg-primary' : 'bg-muted'}`} />
            <div className={`h-1.5 rounded-full transition-all ${step >= 3 ? 'bg-primary' : 'bg-muted'}`} />
          </div>
        </div>

        {/* Wizard Body */}
        <div className="p-6 overflow-y-auto flex-1">
          {step === 1 && (
            <form onSubmit={handleSavePreferencesAndNext} className="space-y-5">
              <div>
                <h3 className="text-lg font-bold tracking-tight">Set Up Your Job Search Profile</h3>
                <p className="text-xs text-muted-foreground mt-1">
                  Customize your search criteria so Pipeline can prioritize your daily tasks and calibrate salary data.
                </p>
              </div>

              <div className="space-y-4">
                <div>
                  <label className="block text-xs font-semibold uppercase tracking-wider text-muted-foreground mb-1">
                    Target Role / Title
                  </label>
                  <div className="relative">
                    <Briefcase className="h-4 w-4 absolute left-3 top-2.5 text-muted-foreground" />
                    <input
                      type="text"
                      placeholder="e.g. Senior Software Engineer"
                      value={targetRole}
                      onChange={(e) => setTargetRole(e.target.value)}
                      className="w-full pl-9 pr-3 py-2 text-xs bg-background border border-border rounded-lg focus:outline-none focus:ring-2 focus:ring-primary"
                    />
                  </div>
                </div>

                <div className="grid grid-cols-1 sm:grid-cols-2 gap-3">
                  <div>
                    <label className="block text-xs font-semibold uppercase tracking-wider text-muted-foreground mb-1">
                      Preferred Work Mode
                    </label>
                    <select
                      value={workMode}
                      onChange={(e) => setWorkMode(e.target.value as WorkMode)}
                      className="w-full px-3 py-2 text-xs bg-background border border-border rounded-lg focus:outline-none focus:ring-2 focus:ring-primary"
                    >
                      <option value="Remote">Remote</option>
                      <option value="Hybrid">Hybrid</option>
                      <option value="Onsite">Onsite</option>
                    </select>
                  </div>

                  <div>
                    <label className="block text-xs font-semibold uppercase tracking-wider text-muted-foreground mb-1">
                      Current Search Status
                    </label>
                    <select
                      value={searchStatus}
                      onChange={(e) => setSearchStatus(e.target.value as any)}
                      className="w-full px-3 py-2 text-xs bg-background border border-border rounded-lg focus:outline-none focus:ring-2 focus:ring-primary"
                    >
                      <option value="Active">Actively Interviewing & Applying</option>
                      <option value="Interviewing">Final Rounds / Interviewing</option>
                      <option value="CasuallyLooking">Casually Exploring</option>
                    </select>
                  </div>
                </div>

                <div className="grid grid-cols-1 sm:grid-cols-2 gap-3">
                  <div>
                    <label className="block text-xs font-semibold uppercase tracking-wider text-muted-foreground mb-1">
                      Target Minimum Salary
                    </label>
                    <div className="relative">
                      <DollarSign className="h-4 w-4 absolute left-3 top-2.5 text-muted-foreground" />
                      <input
                        type="number"
                        placeholder="e.g. 130000"
                        value={salaryMin}
                        onChange={(e) => setSalaryMin(e.target.value)}
                        className="w-full pl-9 pr-3 py-2 text-xs bg-background border border-border rounded-lg focus:outline-none focus:ring-2 focus:ring-primary"
                      />
                    </div>
                  </div>

                  <div>
                    <label className="block text-xs font-semibold uppercase tracking-wider text-muted-foreground mb-1">
                      Currency
                    </label>
                    <select
                      value={currency}
                      onChange={(e) => setCurrency(e.target.value)}
                      className="w-full px-3 py-2 text-xs bg-background border border-border rounded-lg focus:outline-none focus:ring-2 focus:ring-primary"
                    >
                      <option value="USD">USD ($)</option>
                      <option value="EUR">EUR (€)</option>
                      <option value="GBP">GBP (£)</option>
                      <option value="CAD">CAD ($)</option>
                    </select>
                  </div>
                </div>
              </div>

              <div className="pt-3 flex items-center justify-end gap-2 border-t border-border">
                <button
                  type="submit"
                  disabled={isSubmitting}
                  className="px-5 py-2 rounded-xl bg-primary text-primary-foreground text-xs font-bold hover:bg-primary/90 transition-colors shadow-xs flex items-center gap-1.5"
                >
                  <span>Continue to Tour</span>
                  <ArrowRight className="h-4 w-4" />
                </button>
              </div>
            </form>
          )}

          {step === 2 && (
            <div className="space-y-6">
              <div>
                <h3 className="text-lg font-bold tracking-tight">Welcome to Pipeline Tour</h3>
                <p className="text-xs text-muted-foreground mt-1">
                  Here is how the command center turns chaotic job applications into an organized pipeline.
                </p>
              </div>

              <div className="grid grid-cols-1 sm:grid-cols-2 gap-3">
                <div className="p-3.5 rounded-xl border border-border bg-card/60 space-y-1.5">
                  <div className="flex items-center gap-2 text-xs font-bold text-indigo-500">
                    <Sparkles className="h-4 w-4" />
                    <span>Daily "Do Today" Queue</span>
                  </div>
                  <p className="text-[11px] text-muted-foreground leading-relaxed">
                    Prioritizes tasks, alerts you to stale applications, and prompts debriefing notes immediately after interviews.
                  </p>
                </div>

                <div className="p-3.5 rounded-xl border border-border bg-card/60 space-y-1.5">
                  <div className="flex items-center gap-2 text-xs font-bold text-emerald-500">
                    <Briefcase className="h-4 w-4" />
                    <span>Visual Pipeline</span>
                  </div>
                  <p className="text-[11px] text-muted-foreground leading-relaxed">
                    Drag and drop applications across custom stages, backfill historical dates, and compare side-by-side offers.
                  </p>
                </div>

                <div className="p-3.5 rounded-xl border border-border bg-card/60 space-y-1.5">
                  <div className="flex items-center gap-2 text-xs font-bold text-amber-500">
                    <Calendar className="h-4 w-4" />
                    <span>Live .ICS Calendar Sync</span>
                  </div>
                  <p className="text-[11px] text-muted-foreground leading-relaxed">
                    Subscribe your Google or Apple Calendar to keep interview rounds, tasks, and deadlines in sync.
                  </p>
                </div>

                <div className="p-3.5 rounded-xl border border-border bg-card/60 space-y-1.5">
                  <div className="flex items-center gap-2 text-xs font-bold text-sky-500">
                    <Globe className="h-4 w-4" />
                    <span>Command Palette (Ctrl+K)</span>
                  </div>
                  <p className="text-[11px] text-muted-foreground leading-relaxed">
                    Press Ctrl+K or Cmd+K anywhere to jump across applications, contacts, and discovery feeds in milliseconds.
                  </p>
                </div>
              </div>

              <div className="pt-3 flex items-center justify-between border-t border-border">
                <button
                  type="button"
                  onClick={() => setStep(1)}
                  className="px-4 py-2 rounded-xl border border-border text-xs font-semibold hover:bg-muted transition-colors flex items-center gap-1.5"
                >
                  <ArrowLeft className="h-4 w-4" />
                  <span>Back to Profile</span>
                </button>
                <button
                  type="button"
                  onClick={() => setStep(3)}
                  className="px-5 py-2 rounded-xl bg-primary text-primary-foreground text-xs font-bold hover:bg-primary/90 transition-colors shadow-xs flex items-center gap-1.5"
                >
                  <span>Continue to Quick Start</span>
                  <ArrowRight className="h-4 w-4" />
                </button>
              </div>
            </div>
          )}

          {step === 3 && (
            <form onSubmit={handleSaveFirstApplication} className="space-y-5">
              <div>
                <h3 className="text-lg font-bold tracking-tight">Log Your First Opportunity</h3>
                <p className="text-xs text-muted-foreground mt-1">
                  Have an application in progress? Log it now to jump right in, or skip ahead to the dashboard.
                </p>
              </div>

              <div className="space-y-4 bg-muted/20 border border-border p-4 rounded-xl">
                <div className="grid grid-cols-1 sm:grid-cols-2 gap-3">
                  <div>
                    <label className="block text-xs font-semibold uppercase tracking-wider text-muted-foreground mb-1">
                      Company Name *
                    </label>
                    <div className="relative">
                      <Building2 className="h-4 w-4 absolute left-3 top-2.5 text-muted-foreground" />
                      <input
                        type="text"
                        placeholder="e.g. Stripe, OpenAI, Figma"
                        value={companyName}
                        onChange={(e) => setCompanyName(e.target.value)}
                        className="w-full pl-9 pr-3 py-1.5 text-xs bg-background border border-border rounded-lg focus:outline-none focus:ring-2 focus:ring-primary"
                      />
                    </div>
                  </div>

                  <div>
                    <label className="block text-xs font-semibold uppercase tracking-wider text-muted-foreground mb-1">
                      Role Title *
                    </label>
                    <input
                      type="text"
                      placeholder="e.g. Frontend Engineer"
                      value={roleTitle}
                      onChange={(e) => setRoleTitle(e.target.value)}
                      className="w-full px-3 py-1.5 text-xs bg-background border border-border rounded-lg focus:outline-none focus:ring-2 focus:ring-primary"
                    />
                  </div>
                </div>

                <div className="grid grid-cols-1 sm:grid-cols-2 gap-3">
                  <div>
                    <label className="block text-xs font-semibold uppercase tracking-wider text-muted-foreground mb-1">
                      Status
                    </label>
                    <select
                      value={appStatus}
                      onChange={(e) => setAppStatus(e.target.value as ApplicationStatus)}
                      className="w-full px-3 py-1.5 text-xs bg-background border border-border rounded-lg focus:outline-none focus:ring-2 focus:ring-primary"
                    >
                      <option value="Wishlist">Wishlist</option>
                      <option value="Applied">Applied</option>
                      <option value="Screening">Screening</option>
                      <option value="Interview">Interview</option>
                      <option value="Offer">Offer</option>
                    </select>
                  </div>

                  <div>
                    <label htmlFor="onboarding-app-date" className="block text-xs font-semibold uppercase tracking-wider text-muted-foreground mb-1">
                      Application Date
                    </label>
                    <div className="relative">
                      <Calendar className="h-4 w-4 absolute left-3 top-2 text-muted-foreground pointer-events-none" />
                      <input
                        id="onboarding-app-date"
                        type="date"
                        max={todayStr}
                        value={appliedAt}
                        onChange={(e) => {
                          setAppliedAt(e.target.value);
                          if (e.target.value > todayStr) {
                            setDateError('Application date cannot be in the future.');
                          } else {
                            setDateError(null);
                          }
                        }}
                        className={`w-full pl-9 pr-3 py-1.5 text-xs bg-background border rounded-lg focus:outline-none focus:ring-2 focus:ring-primary ${
                          dateError ? 'border-destructive' : 'border-border'
                        }`}
                      />
                    </div>
                    {dateError && <p className="text-[10px] text-destructive mt-1">{dateError}</p>}
                  </div>
                </div>
              </div>

              <div className="pt-3 flex items-center justify-between border-t border-border">
                <button
                  type="button"
                  onClick={handleFinishOnboarding}
                  disabled={isSubmitting}
                  className="px-4 py-2 rounded-xl text-xs font-semibold text-muted-foreground hover:text-foreground hover:bg-muted transition-colors"
                >
                  Skip to Dashboard
                </button>

                <div className="flex items-center gap-2">
                  <button
                    type="button"
                    onClick={() => setStep(2)}
                    className="px-3 py-2 rounded-xl border border-border text-xs font-semibold hover:bg-muted transition-colors flex items-center gap-1"
                  >
                    <ArrowLeft className="h-3.5 w-3.5" />
                    <span>Back</span>
                  </button>
                  <button
                    type="submit"
                    disabled={isSubmitting || !companyName.trim() || !roleTitle.trim()}
                    className="px-5 py-2 rounded-xl bg-primary text-primary-foreground text-xs font-bold hover:bg-primary/90 disabled:opacity-50 transition-colors shadow-xs flex items-center gap-1.5"
                  >
                    <span>Save & Enter Command Center</span>
                    <Check className="h-4 w-4" />
                  </button>
                </div>
              </div>
            </form>
          )}
        </div>
      </div>
    </div>
  );
};
