import React from 'react';
import { useDraggable } from '@dnd-kit/core';
import { useNavigate } from 'react-router-dom';
import { ApplicationListItem } from '../types';
import { Calendar, Star, MapPin } from 'lucide-react';
import { clsx } from 'clsx';

interface KanbanCardProps {
  application: ApplicationListItem;
  isDragging?: boolean;
}

export const KanbanCard: React.FC<KanbanCardProps> = ({ application, isDragging = false }) => {
  const navigate = useNavigate();
  const { attributes, listeners, setNodeRef, transform } = useDraggable({
    id: application.id,
    data: { application },
  });

  const style = transform
    ? {
        transform: `translate3d(${transform.x}px, ${transform.y}px, 0)`,
        zIndex: 50,
      }
    : undefined;

  const companyInitials = application.companyName
    .split(' ')
    .map((n) => n[0])
    .join('')
    .toUpperCase()
    .slice(0, 2);

  const priorityColor =
    application.priority === 3
      ? 'bg-rose-500'
      : application.priority === 2
      ? 'bg-amber-500'
      : 'bg-slate-400';

  return (
    <div
      ref={setNodeRef}
      style={style}
      {...listeners}
      {...attributes}
      onClick={(e) => {
        // Prevent navigation when dragging
        if (!transform) {
          navigate(`/applications/${application.id}`);
        }
      }}
      className={clsx(
        'group p-3.5 rounded-xl border border-border/80 bg-card hover:border-primary/50 shadow-sm transition-all cursor-grab active:cursor-grabbing select-none',
        isDragging && 'opacity-50 ring-2 ring-primary shadow-lg scale-105'
      )}
    >
      <div className="flex items-start justify-between gap-2 mb-2">
        <div className="flex items-center gap-2">
          <div className="h-6 w-6 rounded-md bg-secondary text-secondary-foreground font-bold text-[10px] flex items-center justify-center shrink-0 border border-border">
            {companyInitials}
          </div>
          <span className="font-semibold text-xs text-muted-foreground truncate max-w-[130px]">
            {application.companyName}
          </span>
        </div>
        <div className="flex items-center gap-1.5">
          <span
            className={clsx('h-2 w-2 rounded-full shrink-0', priorityColor)}
            title={`Priority: ${application.priority === 3 ? 'High' : application.priority === 2 ? 'Medium' : 'Low'}`}
          />
        </div>
      </div>

      <h4 className="font-semibold text-sm leading-snug line-clamp-1 group-hover:text-primary transition-colors">
        {application.roleTitle}
      </h4>

      <div className="flex items-center gap-2 mt-2 text-[11px] text-muted-foreground">
        <span className="inline-flex items-center gap-1 px-1.5 py-0.5 rounded bg-muted/60 border border-border/50">
          <MapPin className="h-3 w-3" />
          {application.workMode}
        </span>
        {application.daysInStage > 0 && (
          <span>{application.daysInStage}d in stage</span>
        )}
      </div>

      {application.nextInterviewDate && (
        <div className="mt-2.5 pt-2 border-t border-border/50 flex items-center gap-1.5 text-[11px] text-indigo-500 font-medium">
          <Calendar className="h-3 w-3 shrink-0" />
          <span>Interview {new Date(application.nextInterviewDate).toLocaleDateString([], { month: 'short', day: 'numeric' })}</span>
        </div>
      )}

      <div className="flex items-center justify-between mt-2 pt-1.5 text-[10px] text-muted-foreground">
        <div className="flex items-center gap-0.5 text-amber-500/80">
          {Array.from({ length: 5 }).map((_, i) => (
            <Star
              key={i}
              className={clsx('h-2.5 w-2.5', i < application.excitementRating ? 'fill-current' : 'opacity-20')}
            />
          ))}
        </div>
        {application.salaryMin && application.salaryMax ? (
          <span className="font-medium">
            {application.currency} {(application.salaryMin / 1000).toFixed(0)}k-{(application.salaryMax / 1000).toFixed(0)}k
          </span>
        ) : null}
      </div>
    </div>
  );
};
