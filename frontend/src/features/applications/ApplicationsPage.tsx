import React, { useState } from 'react';
import { useApplications } from './useApplications';
import { ApplicationFilter } from './types';
import { ApplicationsFilterBar } from './components/ApplicationsFilterBar';
import { KanbanBoard } from './components/KanbanBoard';
import { ApplicationsTable } from './components/ApplicationsTable';
import { CreateApplicationModal } from './components/CreateApplicationModal';

export const ApplicationsPage: React.FC = () => {
  const [viewMode, setViewMode] = useState<'kanban' | 'table'>('kanban');
  const [filters, setFilters] = useState<ApplicationFilter>({});
  const [isCreateModalOpen, setIsCreateModalOpen] = useState(false);

  const { data: applications = [], isLoading, isError } = useApplications(filters);

  return (
    <div className="space-y-6">
      {/* Header and Filter Controls */}
      <ApplicationsFilterBar
        filters={filters}
        onFilterChange={setFilters}
        viewMode={viewMode}
        onViewModeChange={setViewMode}
        onNewApplication={() => setIsCreateModalOpen(true)}
      />

      {/* Content Area */}
      {isLoading ? (
        <div className="h-64 flex items-center justify-center">
          <div className="flex flex-col items-center gap-2 text-muted-foreground text-xs font-medium">
            <div className="h-6 w-6 rounded-md bg-primary animate-pulse" />
            <span>Loading applications...</span>
          </div>
        </div>
      ) : isError ? (
        <div className="p-8 border border-destructive/20 rounded-xl bg-destructive/10 text-destructive text-center text-sm">
          Failed to load applications. Please try again.
        </div>
      ) : viewMode === 'kanban' ? (
        <KanbanBoard applications={applications} />
      ) : (
        <ApplicationsTable applications={applications} />
      )}

      {/* Creation Modal */}
      <CreateApplicationModal
        isOpen={isCreateModalOpen}
        onClose={() => setIsCreateModalOpen(false)}
      />
    </div>
  );
};
