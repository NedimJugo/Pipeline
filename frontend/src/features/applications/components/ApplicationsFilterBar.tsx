import React from 'react';
import { ApplicationFilter, ApplicationStatus, WorkMode } from '../types';
import { Search, LayoutGrid, List, Plus } from 'lucide-react';
import { clsx } from 'clsx';

interface ApplicationsFilterBarProps {
  filters: ApplicationFilter;
  onFilterChange: (filters: ApplicationFilter) => void;
  viewMode: 'kanban' | 'table';
  onViewModeChange: (mode: 'kanban' | 'table') => void;
  onNewApplication: () => void;
}

export const ApplicationsFilterBar: React.FC<ApplicationsFilterBarProps> = ({
  filters,
  onFilterChange,
  viewMode,
  onViewModeChange,
  onNewApplication,
}) => {
  return (
    <div className="flex flex-col md:flex-row md:items-center justify-between gap-4 pb-4 border-b border-border/80">
      {/* Search & Filters */}
      <div className="flex flex-wrap items-center gap-2.5 flex-1">
        <div className="relative min-w-[200px] flex-1 max-w-sm">
          <Search className="h-4 w-4 absolute left-3 top-2.5 text-muted-foreground" />
          <input
            type="text"
            placeholder="Search roles, companies..."
            value={filters.search || ''}
            onChange={(e) => onFilterChange({ ...filters, search: e.target.value })}
            className="w-full pl-9 pr-3 py-1.5 text-xs bg-background border border-border rounded-lg focus:outline-none focus:ring-2 focus:ring-primary"
          />
        </div>

        {/* Status Filter */}
        <select
          value={filters.status || ''}
          onChange={(e) =>
            onFilterChange({
              ...filters,
              status: e.target.value ? (e.target.value as ApplicationStatus) : undefined,
            })
          }
          className="text-xs bg-background border border-border rounded-lg px-2.5 py-1.5 focus:outline-none focus:ring-2 focus:ring-primary text-foreground"
        >
          <option value="">All Statuses</option>
          <option value="Wishlist">Wishlist</option>
          <option value="Applied">Applied</option>
          <option value="Screening">Screening</option>
          <option value="Interview">Interview</option>
          <option value="Assignment">Assignment</option>
          <option value="Offer">Offer</option>
          <option value="Accepted">Accepted</option>
          <option value="Rejected">Rejected</option>
          <option value="Withdrawn">Withdrawn</option>
          <option value="Ghosted">Ghosted</option>
        </select>

        {/* WorkMode Filter */}
        <select
          value={filters.workMode || ''}
          onChange={(e) =>
            onFilterChange({
              ...filters,
              workMode: e.target.value ? (e.target.value as WorkMode) : undefined,
            })
          }
          className="text-xs bg-background border border-border rounded-lg px-2.5 py-1.5 focus:outline-none focus:ring-2 focus:ring-primary text-foreground"
        >
          <option value="">All Modes</option>
          <option value="Remote">Remote</option>
          <option value="Hybrid">Hybrid</option>
          <option value="Onsite">Onsite</option>
        </select>

        {/* Priority Filter */}
        <select
          value={filters.priority?.toString() || ''}
          onChange={(e) =>
            onFilterChange({
              ...filters,
              priority: e.target.value ? parseInt(e.target.value) : undefined,
            })
          }
          className="text-xs bg-background border border-border rounded-lg px-2.5 py-1.5 focus:outline-none focus:ring-2 focus:ring-primary text-foreground"
        >
          <option value="">All Priorities</option>
          <option value="3">High Priority</option>
          <option value="2">Medium Priority</option>
          <option value="1">Low Priority</option>
        </select>
      </div>

      {/* View Switcher & Action CTA */}
      <div className="flex items-center gap-3 self-end md:self-auto">
        <div className="flex items-center p-0.5 rounded-lg border border-border bg-muted/60">
          <button
            type="button"
            onClick={() => onViewModeChange('kanban')}
            className={clsx(
              'p-1.5 rounded-md text-xs font-medium flex items-center gap-1.5 transition-colors',
              viewMode === 'kanban'
                ? 'bg-background text-foreground shadow-sm'
                : 'text-muted-foreground hover:text-foreground'
            )}
            title="Kanban Board View"
          >
            <LayoutGrid className="h-3.5 w-3.5" />
            <span className="hidden sm:inline">Board</span>
          </button>
          <button
            type="button"
            onClick={() => onViewModeChange('table')}
            className={clsx(
              'p-1.5 rounded-md text-xs font-medium flex items-center gap-1.5 transition-colors',
              viewMode === 'table'
                ? 'bg-background text-foreground shadow-sm'
                : 'text-muted-foreground hover:text-foreground'
            )}
            title="Table View"
          >
            <List className="h-3.5 w-3.5" />
            <span className="hidden sm:inline">Table</span>
          </button>
        </div>

        <button
          type="button"
          onClick={onNewApplication}
          className="px-3.5 py-1.5 bg-primary text-primary-foreground text-xs font-semibold rounded-lg hover:bg-primary/90 flex items-center gap-1.5 shadow-sm transition-colors"
        >
          <Plus className="h-4 w-4" />
          <span>New Application</span>
        </button>
      </div>
    </div>
  );
};
