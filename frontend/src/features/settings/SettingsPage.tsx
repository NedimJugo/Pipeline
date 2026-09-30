import React, { useState } from 'react';
import {
  User,
  Sliders,
  Database,
  Palette,
  Loader2,
  AlertCircle,
} from 'lucide-react';
import { useProfile } from './useSettings';
import { ProfileSettingsTab } from './ProfileSettingsTab';
import { PipelineSettingsTab } from './PipelineSettingsTab';
import { DataManagementTab } from './DataManagementTab';
import { AppearanceTab } from './AppearanceTab';

type SettingsTab = 'profile' | 'pipeline' | 'data' | 'appearance';

export const SettingsPage: React.FC = () => {
  const [activeTab, setActiveTab] = useState<SettingsTab>('profile');
  const { data: profile, isLoading, error } = useProfile();

  if (isLoading) {
    return (
      <div className="flex items-center justify-center min-h-[400px]">
        <div className="flex flex-col items-center gap-3">
          <Loader2 className="h-8 w-8 animate-spin text-primary" />
          <span className="text-xs text-muted-foreground">Loading account settings...</span>
        </div>
      </div>
    );
  }

  if (error || !profile) {
    return (
      <div className="p-6 rounded-xl border border-destructive/20 bg-destructive/5 text-destructive flex items-center gap-3">
        <AlertCircle className="h-5 w-5 shrink-0" />
        <span className="text-sm font-medium">Failed to load user settings. Please refresh the page.</span>
      </div>
    );
  }

  return (
    <div className="max-w-5xl mx-auto space-y-6 pb-12">
      {/* Header */}
      <div className="pb-4 border-b border-border">
        <h1 className="text-2xl font-bold tracking-tight">Account Settings & Data Management</h1>
        <p className="text-sm text-muted-foreground mt-1">
          Manage your career identity, pipeline rules, CSV import/export, and GDPR data portability
        </p>
      </div>

      {/* Tabs Bar */}
      <div className="flex items-center gap-2 border-b border-border pb-px overflow-x-auto">
        <button
          onClick={() => setActiveTab('profile')}
          className={`flex items-center gap-2 px-4 py-2.5 text-xs font-semibold border-b-2 transition-colors whitespace-nowrap ${
            activeTab === 'profile'
              ? 'border-primary text-primary font-bold'
              : 'border-transparent text-muted-foreground hover:text-foreground'
          }`}
        >
          <User className="h-4 w-4" />
          <span>Profile & Career</span>
        </button>

        <button
          onClick={() => setActiveTab('pipeline')}
          className={`flex items-center gap-2 px-4 py-2.5 text-xs font-semibold border-b-2 transition-colors whitespace-nowrap ${
            activeTab === 'pipeline'
              ? 'border-primary text-primary font-bold'
              : 'border-transparent text-muted-foreground hover:text-foreground'
          }`}
        >
          <Sliders className="h-4 w-4" />
          <span>Pipeline Rules</span>
        </button>

        <button
          onClick={() => setActiveTab('data')}
          className={`flex items-center gap-2 px-4 py-2.5 text-xs font-semibold border-b-2 transition-colors whitespace-nowrap ${
            activeTab === 'data'
              ? 'border-primary text-primary font-bold'
              : 'border-transparent text-muted-foreground hover:text-foreground'
          }`}
        >
          <Database className="h-4 w-4" />
          <span>Data Management & GDPR</span>
        </button>

        <button
          onClick={() => setActiveTab('appearance')}
          className={`flex items-center gap-2 px-4 py-2.5 text-xs font-semibold border-b-2 transition-colors whitespace-nowrap ${
            activeTab === 'appearance'
              ? 'border-primary text-primary font-bold'
              : 'border-transparent text-muted-foreground hover:text-foreground'
          }`}
        >
          <Palette className="h-4 w-4" />
          <span>Appearance & PWA</span>
        </button>
      </div>

      {/* Tab Panels */}
      <div>
        {activeTab === 'profile' && <ProfileSettingsTab profile={profile} />}
        {activeTab === 'pipeline' && <PipelineSettingsTab profile={profile} />}
        {activeTab === 'data' && <DataManagementTab />}
        {activeTab === 'appearance' && <AppearanceTab />}
      </div>
    </div>
  );
};
