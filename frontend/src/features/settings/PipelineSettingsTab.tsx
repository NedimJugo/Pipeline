import React, { useState, useEffect } from 'react';
import { Sliders, Clock, AlertTriangle, Check, Loader2, Zap } from 'lucide-react';
import { UserSettingsProfile } from './types';
import { useUpdatePreferences } from './useSettings';

interface PipelineSettingsTabProps {
  profile: UserSettingsProfile;
}

export const PipelineSettingsTab: React.FC<PipelineSettingsTabProps> = ({ profile }) => {
  const updateMutation = useUpdatePreferences();
  const [success, setSuccess] = useState(false);
  const [staleAfterDays, setStaleAfterDays] = useState(profile.staleAfterDays || 14);

  useEffect(() => {
    setStaleAfterDays(profile.staleAfterDays || 14);
  }, [profile]);

  const handleSubmit = async (e: React.FormEvent) => {
    e.preventDefault();
    setSuccess(false);

    await updateMutation.mutateAsync({
      staleAfterDays,
    });

    setSuccess(true);
    setTimeout(() => setSuccess(false), 3000);
  };

  return (
    <form onSubmit={handleSubmit} className="space-y-6">
      <div className="bg-card border border-border rounded-xl p-6 shadow-xs space-y-6">
        <div>
          <h2 className="text-base font-bold tracking-tight">Pipeline Rules & Inactivity Thresholds</h2>
          <p className="text-xs text-muted-foreground mt-0.5">
            Configure how the next-action engine detects stale applications and triggers follow-up reminders
          </p>
        </div>

        {success && (
          <div className="p-3 bg-emerald-500/10 border border-emerald-500/20 text-emerald-600 dark:text-emerald-400 rounded-lg text-xs font-semibold flex items-center gap-2">
            <Check className="h-4 w-4 shrink-0" />
            <span>Pipeline settings updated successfully.</span>
          </div>
        )}

        {/* Stale Threshold Slider & Radios */}
        <div className="space-y-3">
          <label className="block text-xs font-semibold text-muted-foreground uppercase tracking-wider">
            Stale Inactivity Threshold
          </label>
          <p className="text-xs text-muted-foreground">
            Applications with no status updates, notes, or contact interactions for this duration are highlighted as stale on the Kanban board and trigger follow-up alerts.
          </p>

          <div className="grid grid-cols-2 sm:grid-cols-4 gap-3 pt-2">
            {[7, 14, 21, 30].map((days) => {
              const isSelected = staleAfterDays === days;
              return (
                <button
                  key={days}
                  type="button"
                  onClick={() => setStaleAfterDays(days)}
                  className={`p-3.5 rounded-xl border text-center transition-all ${
                    isSelected
                      ? 'border-primary bg-primary/10 text-primary font-bold shadow-2xs'
                      : 'border-border hover:bg-muted/50 text-foreground font-medium'
                  }`}
                >
                  <div className="text-lg font-bold">{days} Days</div>
                  <div className="text-[11px] text-muted-foreground mt-0.5">
                    {days === 7 ? 'Aggressive' : days === 14 ? 'Standard' : days === 21 ? 'Relaxed' : 'Long-cycle'}
                  </div>
                </button>
              );
            })}
          </div>
        </div>

        {/* Automation Rules Overview */}
        <div className="pt-4 border-t border-border space-y-3">
          <div className="flex items-center gap-2 text-xs font-bold uppercase tracking-wider text-muted-foreground">
            <Zap className="h-4 w-4 text-amber-500" />
            <span>Active Next-Action Automation Rules</span>
          </div>
          <div className="grid grid-cols-1 sm:grid-cols-2 gap-3 text-xs">
            <div className="p-3 rounded-lg border border-border/60 bg-muted/20">
              <div className="font-semibold text-foreground">Follow-up After Application</div>
              <div className="text-muted-foreground mt-0.5">Generates task after 7 days without response</div>
            </div>
            <div className="p-3 rounded-lg border border-border/60 bg-muted/20">
              <div className="font-semibold text-foreground">Post-Interview Thank You</div>
              <div className="text-muted-foreground mt-0.5">Schedules thank-you deliverable due within 24h</div>
            </div>
            <div className="p-3 rounded-lg border border-border/60 bg-muted/20">
              <div className="font-semibold text-foreground">Interview Prep Reminder</div>
              <div className="text-muted-foreground mt-0.5">Alerts 48h prior if questions checklist is incomplete</div>
            </div>
            <div className="p-3 rounded-lg border border-border/60 bg-muted/20">
              <div className="font-semibold text-foreground">Offer Decision Deadline</div>
              <div className="text-muted-foreground mt-0.5">Urgent reminder 3 days before offer expiration</div>
            </div>
          </div>
        </div>

        <div className="pt-4 border-t border-border flex justify-end">
          <button
            type="submit"
            disabled={updateMutation.isPending}
            className="inline-flex items-center gap-2 px-5 py-2.5 bg-primary text-primary-foreground text-xs font-bold rounded-lg hover:bg-primary/90 transition-colors shadow-2xs disabled:opacity-50"
          >
            {updateMutation.isPending ? (
              <>
                <Loader2 className="h-4 w-4 animate-spin" />
                <span>Saving Rules...</span>
              </>
            ) : (
              <>
                <Check className="h-4 w-4" />
                <span>Save Pipeline Rules</span>
              </>
            )}
          </button>
        </div>
      </div>
    </form>
  );
};
