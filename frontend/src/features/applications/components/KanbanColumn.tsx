import React from 'react';
import { useDroppable } from '@dnd-kit/core';
import { ApplicationListItem, ApplicationStatus } from '../types';
import { KanbanCard } from './KanbanCard';
import { clsx } from 'clsx';

interface KanbanColumnProps {
  status: ApplicationStatus;
  label: string;
  applications: ApplicationListItem[];
  colorDot?: string;
}

export const KanbanColumn: React.FC<KanbanColumnProps> = ({
  status,
  label,
  applications,
  colorDot = 'bg-primary',
}) => {
  const { isOver, setNodeRef } = useDroppable({
    id: status,
    data: { status },
  });

  return (
    <div
      ref={setNodeRef}
      className={clsx(
        'w-72 shrink-0 flex flex-col rounded-xl border border-border/80 bg-muted/30 transition-colors max-h-[calc(100vh-210px)]',
        isOver && 'ring-2 ring-primary/60 bg-primary/5'
      )}
    >
      {/* Column Header */}
      <div className="p-3.5 border-b border-border/70 flex items-center justify-between">
        <div className="flex items-center gap-2">
          <span className={clsx('h-2 w-2 rounded-full', colorDot)} />
          <h3 className="font-semibold text-xs tracking-tight uppercase text-foreground/90">
            {label}
          </h3>
        </div>
        <span className="text-xs font-semibold px-2 py-0.5 rounded-full bg-background border border-border text-muted-foreground">
          {applications.length}
        </span>
      </div>

      {/* Cards List */}
      <div className="flex-1 overflow-y-auto p-2.5 space-y-2.5">
        {applications.length === 0 ? (
          <div className="h-24 border border-dashed border-border/60 rounded-lg flex items-center justify-center text-xs text-muted-foreground/60 select-none">
            Drop here
          </div>
        ) : (
          applications.map((app) => <KanbanCard key={app.id} application={app} />)
        )}
      </div>
    </div>
  );
};
