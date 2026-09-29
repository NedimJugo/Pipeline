import React, { useState } from 'react';
import { useDroppable } from '@dnd-kit/core';
import { ApplicationListItem, ApplicationStatus } from '../types';
import { KanbanCard } from './KanbanCard';
import { ChevronRight, ChevronDown, Archive } from 'lucide-react';
import { clsx } from 'clsx';

interface ClosedLaneProps {
  applications: ApplicationListItem[];
}

export const ClosedLane: React.FC<ClosedLaneProps> = ({ applications }) => {
  const [isExpanded, setIsExpanded] = useState(false);
  const { isOver, setNodeRef } = useDroppable({
    id: 'Rejected', // Dropping onto Closed defaults to Rejected
    data: { status: 'Rejected' },
  });

  const terminalStatuses: ApplicationStatus[] = ['Rejected', 'Withdrawn', 'Ghosted', 'Declined'];
  const closedApps = applications.filter((a) => terminalStatuses.includes(a.status));

  return (
    <div
      ref={setNodeRef}
      className={clsx(
        'shrink-0 flex flex-col rounded-xl border border-border/80 bg-muted/20 transition-all max-h-[calc(100vh-210px)]',
        isExpanded ? 'w-72' : 'w-12',
        isOver && 'ring-2 ring-destructive/60 bg-destructive/5'
      )}
    >
      {/* Header / Toggle Button */}
      <button
        type="button"
        onClick={() => setIsExpanded(!isExpanded)}
        className="p-3.5 flex items-center justify-between text-muted-foreground hover:text-foreground transition-colors border-b border-border/60"
        title="Toggle Closed Applications"
      >
        <div className="flex items-center gap-2 overflow-hidden">
          <Archive className="h-4 w-4 shrink-0 text-muted-foreground" />
          {isExpanded && (
            <span className="font-semibold text-xs tracking-tight uppercase truncate">
              Closed ({closedApps.length})
            </span>
          )}
        </div>
        {isExpanded ? (
          <ChevronDown className="h-4 w-4 shrink-0" />
        ) : (
          <ChevronRight className="h-4 w-4 shrink-0" />
        )}
      </button>

      {/* Expanded List */}
      {isExpanded ? (
        <div className="flex-1 overflow-y-auto p-2.5 space-y-2.5">
          {closedApps.length === 0 ? (
            <div className="h-24 border border-dashed border-border/60 rounded-lg flex items-center justify-center text-xs text-muted-foreground/60 select-none">
              No closed applications
            </div>
          ) : (
            closedApps.map((app) => <KanbanCard key={app.id} application={app} />)
          )}
        </div>
      ) : (
        <div className="flex-1 py-4 flex flex-col items-center gap-3">
          <span className="text-[11px] font-bold text-muted-foreground [writing-mode:vertical-lr] tracking-wider uppercase">
            Closed Lane ({closedApps.length})
          </span>
        </div>
      )}
    </div>
  );
};
