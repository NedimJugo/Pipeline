import React, { useState, useEffect } from 'react';
import { User, Check, Loader2, Sparkles, DollarSign, Globe, MapPin } from 'lucide-react';
import { UserSettingsProfile, SearchStatus } from './types';
import { useUpdateProfile } from './useSettings';

interface ProfileSettingsTabProps {
  profile: UserSettingsProfile;
}

export const ProfileSettingsTab: React.FC<ProfileSettingsTabProps> = ({ profile }) => {
  const updateMutation = useUpdateProfile();
  const [success, setSuccess] = useState(false);

  const [displayName, setDisplayName] = useState(profile.displayName || '');
  const [targetRole, setTargetRole] = useState(profile.targetRole || '');
  const [seniority, setSeniority] = useState(profile.seniority || '');
  const [location, setLocation] = useState(profile.location || '');
  const [salaryMin, setSalaryMin] = useState<string>(profile.salaryExpectationMin?.toString() || '');
  const [salaryMax, setSalaryMax] = useState<string>(profile.salaryExpectationMax?.toString() || '');
  const [currency, setCurrency] = useState(profile.currency || 'USD');
  const [searchStatus, setSearchStatus] = useState<SearchStatus>(profile.searchStatus || 'Active');
  const [timezone, setTimezone] = useState(profile.timezone || 'UTC');

  useEffect(() => {
    setDisplayName(profile.displayName || '');
    setTargetRole(profile.targetRole || '');
    setSeniority(profile.seniority || '');
    setLocation(profile.location || '');
    setSalaryMin(profile.salaryExpectationMin?.toString() || '');
    setSalaryMax(profile.salaryExpectationMax?.toString() || '');
    setCurrency(profile.currency || 'USD');
    setSearchStatus(profile.searchStatus || 'Active');
    setTimezone(profile.timezone || 'UTC');
  }, [profile]);

  const handleSubmit = async (e: React.FormEvent) => {
    e.preventDefault();
    setSuccess(false);

    await updateMutation.mutateAsync({
      displayName: displayName.trim() || null,
      targetRole: targetRole.trim() || null,
      seniority: seniority.trim() || null,
      location: location.trim() || null,
      salaryExpectationMin: salaryMin ? parseFloat(salaryMin) : null,
      salaryExpectationMax: salaryMax ? parseFloat(salaryMax) : null,
      currency,
      searchStatus,
      timezone,
    });

    setSuccess(true);
    setTimeout(() => setSuccess(false), 3000);
  };

  return (
    <form onSubmit={handleSubmit} className="space-y-6">
      <div className="bg-card border border-border rounded-xl p-6 shadow-xs space-y-6">
        <div>
          <h2 className="text-base font-bold tracking-tight">Career Goals & Identity</h2>
          <p className="text-xs text-muted-foreground mt-0.5">
            Define your target role, salary parameters, and search status used across pipeline scoring
          </p>
        </div>

        {success && (
          <div className="p-3 bg-emerald-500/10 border border-emerald-500/20 text-emerald-600 dark:text-emerald-400 rounded-lg text-xs font-semibold flex items-center gap-2">
            <Check className="h-4 w-4 shrink-0" />
            <span>Profile and career preferences updated successfully.</span>
          </div>
        )}

        <div className="grid grid-cols-1 md:grid-cols-2 gap-4">
          <div>
            <label className="block text-xs font-semibold text-muted-foreground uppercase tracking-wider mb-1.5">
              Full Name / Display Name
            </label>
            <input
              type="text"
              value={displayName}
              onChange={(e) => setDisplayName(e.target.value)}
              placeholder="e.g. Alex Mercer"
              className="w-full px-3 py-2 bg-background border border-border rounded-lg text-sm text-foreground focus:outline-none focus:ring-1 focus:ring-primary"
            />
          </div>

          <div>
            <label className="block text-xs font-semibold text-muted-foreground uppercase tracking-wider mb-1.5">
              Email Address
            </label>
            <input
              type="text"
              disabled
              value={profile.email}
              className="w-full px-3 py-2 bg-muted/40 border border-border rounded-lg text-sm text-muted-foreground cursor-not-allowed"
            />
          </div>

          <div>
            <label className="block text-xs font-semibold text-muted-foreground uppercase tracking-wider mb-1.5">
              Target Role Title
            </label>
            <input
              type="text"
              value={targetRole}
              onChange={(e) => setTargetRole(e.target.value)}
              placeholder="e.g. Staff Distributed Systems Engineer"
              className="w-full px-3 py-2 bg-background border border-border rounded-lg text-sm text-foreground focus:outline-none focus:ring-1 focus:ring-primary"
            />
          </div>

          <div>
            <label className="block text-xs font-semibold text-muted-foreground uppercase tracking-wider mb-1.5">
              Seniority Level
            </label>
            <select
              value={seniority}
              onChange={(e) => setSeniority(e.target.value)}
              className="w-full px-3 py-2 bg-background border border-border rounded-lg text-sm text-foreground focus:outline-none focus:ring-1 focus:ring-primary"
            >
              <option value="">Select seniority</option>
              <option value="Junior">Junior</option>
              <option value="Mid">Mid-Level</option>
              <option value="Senior">Senior</option>
              <option value="Staff">Staff</option>
              <option value="Principal">Principal / Lead</option>
              <option value="Director">Director / VP</option>
            </select>
          </div>

          <div>
            <label className="block text-xs font-semibold text-muted-foreground uppercase tracking-wider mb-1.5">
              Preferred Location / Remote Mode
            </label>
            <input
              type="text"
              value={location}
              onChange={(e) => setLocation(e.target.value)}
              placeholder="e.g. San Francisco, CA (Remote)"
              className="w-full px-3 py-2 bg-background border border-border rounded-lg text-sm text-foreground focus:outline-none focus:ring-1 focus:ring-primary"
            />
          </div>

          <div>
            <label className="block text-xs font-semibold text-muted-foreground uppercase tracking-wider mb-1.5">
              Search Status
            </label>
            <select
              value={searchStatus}
              onChange={(e) => setSearchStatus(e.target.value as SearchStatus)}
              className="w-full px-3 py-2 bg-background border border-border rounded-lg text-sm text-foreground focus:outline-none focus:ring-1 focus:ring-primary"
            >
              <option value="Active">Actively Interviewing</option>
              <option value="Passive">Casually Exploring</option>
              <option value="Paused">Search Paused / Decided</option>
            </select>
          </div>
        </div>

        {/* Compensation & Currency */}
        <div className="pt-4 border-t border-border">
          <div className="text-xs font-bold uppercase tracking-wider text-muted-foreground mb-3">
            Target Compensation
          </div>
          <div className="grid grid-cols-1 sm:grid-cols-3 gap-4">
            <div>
              <label className="block text-xs text-muted-foreground mb-1">Target Minimum</label>
              <div className="relative">
                <DollarSign className="h-4 w-4 absolute left-3 top-2.5 text-muted-foreground" />
                <input
                  type="number"
                  value={salaryMin}
                  onChange={(e) => setSalaryMin(e.target.value)}
                  placeholder="160000"
                  className="w-full pl-9 pr-3 py-2 bg-background border border-border rounded-lg text-sm text-foreground focus:outline-none focus:ring-1 focus:ring-primary"
                />
              </div>
            </div>

            <div>
              <label className="block text-xs text-muted-foreground mb-1">Target Maximum</label>
              <div className="relative">
                <DollarSign className="h-4 w-4 absolute left-3 top-2.5 text-muted-foreground" />
                <input
                  type="number"
                  value={salaryMax}
                  onChange={(e) => setSalaryMax(e.target.value)}
                  placeholder="220000"
                  className="w-full pl-9 pr-3 py-2 bg-background border border-border rounded-lg text-sm text-foreground focus:outline-none focus:ring-1 focus:ring-primary"
                />
              </div>
            </div>

            <div>
              <label className="block text-xs text-muted-foreground mb-1">Currency</label>
              <select
                value={currency}
                onChange={(e) => setCurrency(e.target.value)}
                className="w-full px-3 py-2 bg-background border border-border rounded-lg text-sm text-foreground focus:outline-none focus:ring-1 focus:ring-primary"
              >
                <option value="USD">USD ($)</option>
                <option value="EUR">EUR (€)</option>
                <option value="GBP">GBP (£)</option>
                <option value="CAD">CAD ($)</option>
                <option value="CHF">CHF</option>
              </select>
            </div>
          </div>
        </div>

        {/* Timezone */}
        <div className="pt-4 border-t border-border">
          <label className="block text-xs font-semibold text-muted-foreground uppercase tracking-wider mb-1.5">
            Timezone
          </label>
          <div className="max-w-md">
            <select
              value={timezone}
              onChange={(e) => setTimezone(e.target.value)}
              className="w-full px-3 py-2 bg-background border border-border rounded-lg text-sm text-foreground focus:outline-none focus:ring-1 focus:ring-primary"
            >
              <option value="UTC">UTC (Universal Time)</option>
              <option value="America/New_York">Eastern Time (US & Canada)</option>
              <option value="America/Chicago">Central Time (US & Canada)</option>
              <option value="America/Denver">Mountain Time (US & Canada)</option>
              <option value="America/Los_Angeles">Pacific Time (US & Canada)</option>
              <option value="Europe/London">London (GMT / BST)</option>
              <option value="Europe/Berlin">Central European Time (CET)</option>
              <option value="Asia/Tokyo">Tokyo (JST)</option>
            </select>
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
                <span>Saving Changes...</span>
              </>
            ) : (
              <>
                <Check className="h-4 w-4" />
                <span>Save Profile Changes</span>
              </>
            )}
          </button>
        </div>
      </div>
    </form>
  );
};
