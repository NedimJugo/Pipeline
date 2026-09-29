import React, { useState } from 'react';
import {
  DndContext,
  DragEndEvent,
  DragOverlay,
  DragStartEvent,
  KeyboardSensor,
  PointerSensor,
  useSensor,
  useSensors,
} from '@dnd-kit/core';
import { ApplicationListItem, ApplicationStatus } from '../types';
import { KanbanColumn } from './KanbanColumn';
import { ClosedLane } from './ClosedLane';
import { KanbanCard } from './KanbanCard';
import { useUpdateApplicationStatus } from '../useApplications';

interface KanbanBoardProps {
  applications: ApplicationListItem[];
}

const activeColumns: { status: ApplicationStatus; label: string; colorDot: string }[] = [
  { status: 'Wishlist', label: 'Wishlist', colorDot: 'bg-slate-400' },
  { status: 'Applied', label: 'Applied', colorDot: 'bg-blue-500' },
  { status: 'Screening', label: 'Screening', colorDot: 'bg-indigo-500' },
  { status: 'Interview', label: 'Interview', colorDot: 'bg-violet-500' },
  { status: 'Assignment', label: 'Assignment', colorDot: 'bg-amber-500' },
  { status: 'Offer', label: 'Offer', colorDot: 'bg-emerald-500' },
  { status: 'Accepted', label: 'Accepted', colorDot: 'bg-green-600' },
];

export const KanbanBoard: React.FC<KanbanBoardProps> = ({ applications }) => {
  const [activeCard, setActiveCard] = useState<ApplicationListItem | null>(null);
  const updateStatusMutation = useUpdateApplicationStatus();

  const sensors = useSensors(
    useSensor(PointerSensor, {
      activationConstraint: {
        distance: 5,
      },
    }),
    useSensor(KeyboardSensor)
  );

  const handleDragStart = (event: DragStartEvent) => {
    const card = applications.find((a) => a.id === event.active.id);
    if (card) {
      setActiveCard(card);
    }
  };

  const handleDragEnd = (event: DragEndEvent) => {
    const { active, over } = event;
    setActiveCard(null);

    if (!over) return;

    const applicationId = active.id as string;
    const targetStatus = over.id as ApplicationStatus;

    const currentApp = applications.find((a) => a.id === applicationId);
    if (currentApp && currentApp.status !== targetStatus) {
      updateStatusMutation.mutate({
        id: applicationId,
        payload: {
          status: targetStatus,
          note: `Moved to ${targetStatus} via Kanban board`,
        },
      });
    }
  };

  return (
    <DndContext
      sensors={sensors}
      onDragStart={handleDragStart}
      onDragEnd={handleDragEnd}
    >
      <div className="flex gap-4 overflow-x-auto pb-4 pt-1 items-start min-h-[calc(100vh-210px)] select-none">
        {activeColumns.map((col) => {
          const colApps = applications.filter((a) => a.status === col.status);
          return (
            <KanbanColumn
              key={col.status}
              status={col.status}
              label={col.label}
              colorDot={col.colorDot}
              applications={colApps}
            />
          );
        })}

        <ClosedLane applications={applications} />
      </div>

      <DragOverlay>
        {activeCard ? <KanbanCard application={activeCard} isDragging /> : null}
      </DragOverlay>
    </DndContext>
  );
};
