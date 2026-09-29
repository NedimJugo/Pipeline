import React from 'react';

export const ContactsPage: React.FC = () => (
  <div className="space-y-4">
    <h1 className="text-2xl font-bold tracking-tight">Contacts & Recruiter Memory</h1>
    <p className="text-sm text-muted-foreground">Directory of recruiters, interviewers, and relationship warmth</p>
    <div className="border border-dashed border-border rounded-xl p-12 text-center text-muted-foreground">
      Contacts & Interaction Logging (Milestone 3 deliverable)
    </div>
  </div>
);

export const InterviewsPage: React.FC = () => (
  <div className="space-y-4">
    <h1 className="text-2xl font-bold tracking-tight">Interviews & Preparation</h1>
    <p className="text-sm text-muted-foreground">Preparation checklists, questions log, and debrief notes</p>
    <div className="border border-dashed border-border rounded-xl p-12 text-center text-muted-foreground">
      Interview Command Center (Milestone 4 deliverable)
    </div>
  </div>
);

export const DocumentsPage: React.FC = () => (
  <div className="space-y-4">
    <h1 className="text-2xl font-bold tracking-tight">Documents & CV Versions</h1>
    <p className="text-sm text-muted-foreground">CV & cover letter version performance metrics and storage</p>
    <div className="border border-dashed border-border rounded-xl p-12 text-center text-muted-foreground">
      CV Versioning & Stats (Milestone 5 deliverable)
    </div>
  </div>
);

export const TasksPage: React.FC = () => (
  <div className="space-y-4">
    <h1 className="text-2xl font-bold tracking-tight">Tasks & Automation Engine</h1>
    <p className="text-sm text-muted-foreground">Prioritized follow-ups and system-generated reminders</p>
    <div className="border border-dashed border-border rounded-xl p-12 text-center text-muted-foreground">
      Tasks & Next-Action Engine (Milestone 6 deliverable)
    </div>
  </div>
);

export const ReferencesPage: React.FC = () => (
  <div className="space-y-4">
    <h1 className="text-2xl font-bold tracking-tight">Reference Manager</h1>
    <p className="text-sm text-muted-foreground">Manage reference consent status and application links</p>
    <div className="border border-dashed border-border rounded-xl p-12 text-center text-muted-foreground">
      Reference Manager (Milestone 7 deliverable)
    </div>
  </div>
);

export const OffersPage: React.FC = () => (
  <div className="space-y-4">
    <h1 className="text-2xl font-bold tracking-tight">Offer Comparison Matrix</h1>
    <p className="text-sm text-muted-foreground">Side-by-side weighted comparison of job offers</p>
    <div className="border border-dashed border-border rounded-xl p-12 text-center text-muted-foreground">
      Offer Comparison (Milestone 7 deliverable)
    </div>
  </div>
);

export const AnalyticsPage: React.FC = () => (
  <div className="space-y-4">
    <h1 className="text-2xl font-bold tracking-tight">Funnel & Analytics</h1>
    <p className="text-sm text-muted-foreground">Conversion metrics, source performance, and automated insight cards</p>
    <div className="border border-dashed border-border rounded-xl p-12 text-center text-muted-foreground">
      Funnel Analytics & Insights (Milestone 8 deliverable)
    </div>
  </div>
);

export const CalendarPage: React.FC = () => (
  <div className="space-y-4">
    <h1 className="text-2xl font-bold tracking-tight">Calendar & Schedule Feed</h1>
    <p className="text-sm text-muted-foreground">Interviews, follow-ups, and offer deadlines with personal .ics feed</p>
    <div className="border border-dashed border-border rounded-xl p-12 text-center text-muted-foreground">
      Calendar View (Milestone 8 deliverable)
    </div>
  </div>
);

export const TemplatesPage: React.FC = () => (
  <div className="space-y-4">
    <h1 className="text-2xl font-bold tracking-tight">Email Templates</h1>
    <p className="text-sm text-muted-foreground">Ready-to-use message templates with auto-populated placeholders</p>
    <div className="border border-dashed border-border rounded-xl p-12 text-center text-muted-foreground">
      Templates Manager (Milestone 6 deliverable)
    </div>
  </div>
);

export const DiscoveryPage: React.FC = () => (
  <div className="max-w-xl mx-auto py-12 text-center space-y-4">
    <h1 className="text-3xl font-bold tracking-tight">Job Discovery</h1>
    <p className="text-muted-foreground">
      Coming soon: automatically discover and save matching roles straight into your Pipeline CRM.
    </p>
    <div className="p-6 border border-border rounded-xl bg-card/60 shadow-sm text-left space-y-4">
      <div className="flex items-center justify-between">
        <div>
          <h3 className="font-semibold text-sm">Notify me when ready</h3>
          <p className="text-xs text-muted-foreground">Get notified as soon as aggregation connectors launch</p>
        </div>
        <input type="checkbox" defaultChecked className="h-4 w-4 accent-primary rounded" />
      </div>
    </div>
  </div>
);

export const SettingsPage: React.FC = () => (
  <div className="space-y-4">
    <h1 className="text-2xl font-bold tracking-tight">Settings & Preferences</h1>
    <p className="text-sm text-muted-foreground">Profile, notification toggles, quiet hours, and GDPR data export</p>
    <div className="border border-dashed border-border rounded-xl p-12 text-center text-muted-foreground">
      Settings & GDPR (Milestone 9 deliverable)
    </div>
  </div>
);
