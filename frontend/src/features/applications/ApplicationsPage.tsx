import React from 'react';

export const ApplicationsPage: React.FC = () => {
  return (
    <div className="space-y-4">
      <div className="flex items-center justify-between">
        <div>
          <h1 className="text-2xl font-bold tracking-tight">Applications</h1>
          <p className="text-sm text-muted-foreground">Manage your job pipeline across Kanban and Table views</p>
        </div>
      </div>
      <div className="border border-dashed border-border rounded-xl p-12 text-center text-muted-foreground">
        Applications Kanban and Table Views (Milestone 2 deliverable)
      </div>
    </div>
  );
};
