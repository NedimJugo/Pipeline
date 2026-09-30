import React, { useState } from 'react';
import {
  CheckCircle2,
  Circle,
  Calendar,
  Clock,
  Briefcase,
  User,
  Zap,
  MoreVertical,
  Edit2,
  Trash2,
  ArrowRight,
  ExternalLink,
} from 'lucide-react';
import { Link } from 'react-router-dom';
import { TaskItem } from './types';
import { useCompleteTask, useSnoozeTask, useDeleteTask } from './useTasks';

interface TaskCardProps {
  task: TaskItem;
  onEdit?: (task: TaskItem) => void;
  showEntityLinks?: boolean;
}

export const TaskCard: React.FC<TaskCardProps> = ({
  task,
  onEdit,
  showEntityLinks = true,
}) => {
  const [isSnoozeOpen, setIsSnoozeOpen] = useState(false);
  const completeMutation = useCompleteTask();
  const snoozeMutation = useSnoozeTask();
  const deleteMutation = useDeleteTask();

  const isCompleted = !!task.completedAt;

  const handleToggleComplete = async () => {
    await completeMutation.mutateAsync({ id: task.id, isCompleted: !isCompleted });
  };

  const handleSnooze = async (days: number) => {
    setIsSnoozeOpen(false);
    await snoozeMutation.mutateAsync({ id: task.id, days });
  };

  const handleDelete = async () => {
    if (window.confirm(`Delete task "${task.title}"?`)) {
      await deleteMutation.mutateAsync(task.id);
    }
  };

  // Due date status styling
  const getDueStatus = () => {
    if (isCompleted) {
      return {
        label: `Completed`,
        badgeClass: 'bg-emerald-500/10 text-emerald-600 dark:text-emerald-400 border-emerald-500/20',
      };
    }
    if (!task.dueAt) {
      return { label: 'No due date', badgeClass: 'bg-muted text-muted-foreground border-border' };
    }

    const today = new Date();
    today.setHours(0, 0, 0, 0);
    const dueDate = new Date(task.dueAt);
    dueDate.setHours(0, 0, 0, 0);

    const diffDays = Math.round((dueDate.getTime() - today.getTime()) / (1000 * 60 * 60 * 24));

    if (diffDays < 0) {
      return {
        label: `Overdue (${Math.abs(diffDays)}d ago)`,
        badgeClass: 'bg-rose-500/10 text-rose-600 dark:text-rose-400 border-rose-500/20 font-semibold',
      };
    } else if (diffDays === 0) {
      return {
        label: 'Due Today',
        badgeClass: 'bg-amber-500/10 text-amber-600 dark:text-amber-400 border-amber-500/20 font-semibold',
      };
    } else if (diffDays === 1) {
      return {
        label: 'Due Tomorrow',
        badgeClass: 'bg-blue-500/10 text-blue-600 dark:text-blue-400 border-blue-500/20',
      };
    } else {
      return {
        label: `Due in ${diffDays}d`,
        badgeClass: 'bg-muted text-muted-foreground border-border',
      };
    }
  };

  const dueStatus = getDueStatus();

  return (
    <div
      className={`p-4 rounded-xl border transition-all ${
        isCompleted
          ? 'bg-card/40 border-border/50 opacity-75'
          : 'bg-card border-border shadow-xs hover:border-border/80'
      }`}
    >
      <div className="flex items-start justify-between gap-3">
        {/* Checkbox & Title */}
        <div className="flex items-start gap-3 min-w-0 flex-1">
          <button
            type="button"
            onClick={handleToggleComplete}
            disabled={completeMutation.isPending}
            className="mt-0.5 text-muted-foreground hover:text-primary transition-colors shrink-0"
            title={isCompleted ? 'Mark incomplete' : 'Mark complete'}
          >
            {isCompleted ? (
              <CheckCircle2 className="h-5 w-5 text-emerald-500 fill-emerald-500/10" />
            ) : (
              <Circle className="h-5 w-5 hover:stroke-primary" />
            )}
          </button>

          <div className="min-w-0 space-y-1">
            <h4
              className={`text-sm font-semibold tracking-tight transition-colors ${
                isCompleted ? 'line-through text-muted-foreground' : 'text-foreground'
              }`}
            >
              {task.title}
            </h4>

            {task.notes && (
              <p className="text-xs text-muted-foreground line-clamp-2 leading-relaxed">
                {task.notes}
              </p>
            )}

            {/* Linked Entity Chips */}
            {showEntityLinks && (
              <div className="flex flex-wrap items-center gap-2 pt-1">
                {task.applicationId && (
                  <Link
                    to={`/applications/${task.applicationId}`}
                    className="inline-flex items-center gap-1 px-2 py-0.5 rounded text-[11px] font-medium bg-muted/60 hover:bg-muted text-foreground/80 hover:text-primary transition-colors border border-border/50"
                  >
                    <Briefcase className="h-3 w-3 text-indigo-500" />
                    <span>
                      {task.applicationTitle || 'Application'}
                      {task.companyName ? ` @ ${task.companyName}` : ''}
                    </span>
                  </Link>
                )}

                {task.contactId && (
                  <Link
                    to={`/contacts/${task.contactId}`}
                    className="inline-flex items-center gap-1 px-2 py-0.5 rounded text-[11px] font-medium bg-muted/60 hover:bg-muted text-foreground/80 hover:text-primary transition-colors border border-border/50"
                  >
                    <User className="h-3 w-3 text-emerald-500" />
                    <span>{task.contactName || 'Contact'}</span>
                  </Link>
                )}

                {task.interviewId && (
                  <Link
                    to={`/interviews/${task.interviewId}`}
                    className="inline-flex items-center gap-1 px-2 py-0.5 rounded text-[11px] font-medium bg-muted/60 hover:bg-muted text-foreground/80 hover:text-primary transition-colors border border-border/50"
                  >
                    <Clock className="h-3 w-3 text-amber-500" />
                    <span>{task.interviewTitle || 'Interview'}</span>
                  </Link>
                )}
              </div>
            )}
          </div>
        </div>

        {/* Right side: Due Status & Action Menu */}
        <div className="flex items-center gap-2 shrink-0">
          {/* Auto vs Manual chip */}
          {task.source === 'Auto' ? (
            <span
              className="inline-flex items-center gap-1 px-2 py-0.5 rounded-full text-[10px] font-semibold bg-violet-500/10 text-violet-600 dark:text-violet-400 border border-violet-500/20"
              title="Generated automatically by the Next-Action Engine"
            >
              <Zap className="h-2.5 w-2.5" />
              <span>Auto</span>
            </span>
          ) : (
            <span className="text-[10px] text-muted-foreground px-1">Manual</span>
          )}

          {/* Due status badge */}
          <span
            className={`inline-flex items-center px-2 py-0.5 rounded-md text-[11px] border ${dueStatus.badgeClass}`}
          >
            {dueStatus.label}
          </span>

          {/* Snooze & Actions Menu */}
          <div className="relative">
            <button
              type="button"
              onClick={() => setIsSnoozeOpen(!isSnoozeOpen)}
              className="p-1 text-muted-foreground hover:text-foreground rounded hover:bg-muted transition-colors"
              title="More actions & snooze"
            >
              <MoreVertical className="h-4 w-4" />
            </button>

            {isSnoozeOpen && (
              <>
                <div className="fixed inset-0 z-20" onClick={() => setIsSnoozeOpen(false)} />
                <div className="absolute right-0 mt-1 w-36 bg-popover border border-border rounded-lg shadow-lg py-1 z-30 text-xs">
                  <div className="px-2 py-1 text-[10px] font-bold uppercase tracking-wider text-muted-foreground">
                    Snooze
                  </div>
                  <button
                    onClick={() => handleSnooze(1)}
                    className="w-full text-left px-3 py-1.5 hover:bg-muted transition-colors"
                  >
                    1 Day
                  </button>
                  <button
                    onClick={() => handleSnooze(3)}
                    className="w-full text-left px-3 py-1.5 hover:bg-muted transition-colors"
                  >
                    3 Days
                  </button>
                  <button
                    onClick={() => handleSnooze(7)}
                    className="w-full text-left px-3 py-1.5 hover:bg-muted transition-colors"
                  >
                    1 Week
                  </button>

                  <div className="border-t border-border my-1" />

                  {onEdit && (
                    <button
                      onClick={() => {
                        setIsSnoozeOpen(false);
                        onEdit(task);
                      }}
                      className="w-full text-left px-3 py-1.5 hover:bg-muted flex items-center gap-1.5 transition-colors"
                    >
                      <Edit2 className="h-3 w-3" />
                      <span>Edit</span>
                    </button>
                  )}

                  <button
                    onClick={() => {
                      setIsSnoozeOpen(false);
                      handleDelete();
                    }}
                    className="w-full text-left px-3 py-1.5 hover:bg-destructive/10 text-destructive flex items-center gap-1.5 transition-colors"
                  >
                    <Trash2 className="h-3 w-3" />
                    <span>Delete</span>
                  </button>
                </div>
              </>
            )}
          </div>
        </div>
      </div>
    </div>
  );
};
