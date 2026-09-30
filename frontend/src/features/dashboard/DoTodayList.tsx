import React, { useState } from 'react';
import {
  CheckCircle2,
  Circle,
  Calendar,
  Clock,
  Briefcase,
  User,
  Zap,
  ArrowUpRight,
  MoreVertical,
  Check,
} from 'lucide-react';
import { Link } from 'react-router-dom';
import { TaskItem } from '@/features/tasks/types';
import { useCompleteTask, useSnoozeTask } from '@/features/tasks/useTasks';

interface DoTodayListProps {
  tasks: TaskItem[];
}

export const DoTodayList: React.FC<DoTodayListProps> = ({ tasks }) => {
  const [activeSnoozeId, setActiveSnoozeId] = useState<string | null>(null);
  const completeMutation = useCompleteTask();
  const snoozeMutation = useSnoozeTask();

  const handleToggle = async (task: TaskItem) => {
    await completeMutation.mutateAsync({
      id: task.id,
      isCompleted: !task.completedAt,
    });
  };

  const handleSnooze = async (taskId: string, days: number) => {
    setActiveSnoozeId(null);
    await snoozeMutation.mutateAsync({ id: taskId, days });
  };

  if (tasks.length === 0) {
    return (
      <div className="border border-dashed border-border rounded-xl p-8 text-center bg-card/20">
        <CheckCircle2 className="h-8 w-8 text-emerald-500 mx-auto mb-2 opacity-80" />
        <h3 className="font-semibold text-sm">All caught up for today!</h3>
        <p className="text-xs text-muted-foreground mt-1 max-w-sm mx-auto">
          No urgent follow-ups, overdue actions, or pending deadlines. The Next-Action Engine will automatically populate tasks as deadlines approach.
        </p>
      </div>
    );
  }

  return (
    <div className="space-y-2.5">
      {tasks.map((task) => {
        const isCompleted = !!task.completedAt;
        const targetUrl = task.interviewId
          ? `/interviews/${task.interviewId}`
          : task.contactId
          ? `/contacts/${task.contactId}`
          : task.applicationId
          ? `/applications/${task.applicationId}`
          : null;

        return (
          <div
            key={task.id}
            className={`p-3.5 rounded-xl border transition-all ${
              isCompleted
                ? 'bg-card/40 border-border/50 opacity-60'
                : 'bg-card border-border shadow-2xs hover:border-border/80'
            }`}
          >
            <div className="flex items-start justify-between gap-3">
              {/* Checkbox and info */}
              <div className="flex items-start gap-3 min-w-0 flex-1">
                <button
                  type="button"
                  onClick={() => handleToggle(task)}
                  disabled={completeMutation.isPending}
                  className="mt-0.5 text-muted-foreground hover:text-primary transition-colors shrink-0"
                  title={isCompleted ? 'Mark incomplete' : 'Mark done'}
                >
                  {isCompleted ? (
                    <CheckCircle2 className="h-5 w-5 text-emerald-500 fill-emerald-500/10" />
                  ) : (
                    <Circle className="h-5 w-5 hover:stroke-primary" />
                  )}
                </button>

                <div className="min-w-0 space-y-1">
                  <div className="flex items-center gap-2 flex-wrap">
                    <span
                      className={`text-sm font-semibold tracking-tight ${
                        isCompleted ? 'line-through text-muted-foreground' : 'text-foreground'
                      }`}
                    >
                      {task.title}
                    </span>

                    {task.source === 'Auto' && (
                      <span className="inline-flex items-center gap-0.5 px-1.5 py-0.5 rounded text-[10px] font-semibold bg-violet-500/10 text-violet-600 dark:text-violet-400 border border-violet-500/20">
                        <Zap className="h-2.5 w-2.5" />
                        <span>Auto</span>
                      </span>
                    )}
                  </div>

                  {task.notes && (
                    <p className="text-xs text-muted-foreground line-clamp-1">{task.notes}</p>
                  )}

                  <div className="flex items-center gap-3 pt-0.5 text-[11px] text-muted-foreground">
                    {task.companyName && (
                      <span className="inline-flex items-center gap-1 font-medium text-foreground/80">
                        <Briefcase className="h-3 w-3 text-indigo-500" />
                        <span>{task.companyName}</span>
                      </span>
                    )}
                    {task.contactName && (
                      <span className="inline-flex items-center gap-1 font-medium text-foreground/80">
                        <User className="h-3 w-3 text-emerald-500" />
                        <span>{task.contactName}</span>
                      </span>
                    )}
                  </div>
                </div>
              </div>

              {/* One-tap actions: Done, Snooze, Open */}
              <div className="flex items-center gap-1.5 shrink-0">
                {targetUrl && (
                  <Link
                    to={targetUrl}
                    className="inline-flex items-center gap-1 px-2.5 py-1 text-xs font-semibold bg-secondary text-secondary-foreground rounded-lg hover:bg-secondary/80 transition-colors"
                    title="Open linked application or contact"
                  >
                    <span>Open</span>
                    <ArrowUpRight className="h-3 w-3" />
                  </Link>
                )}

                {/* Snooze dropdown */}
                <div className="relative">
                  <button
                    type="button"
                    onClick={() =>
                      setActiveSnoozeId(activeSnoozeId === task.id ? null : task.id)
                    }
                    className="px-2 py-1 text-xs font-medium text-muted-foreground hover:text-foreground hover:bg-muted border border-border rounded-lg transition-colors"
                  >
                    Snooze
                  </button>

                  {activeSnoozeId === task.id && (
                    <>
                      <div
                        className="fixed inset-0 z-20"
                        onClick={() => setActiveSnoozeId(null)}
                      />
                      <div className="absolute right-0 mt-1 w-32 bg-popover border border-border rounded-lg shadow-lg py-1 z-30 text-xs">
                        <button
                          onClick={() => handleSnooze(task.id, 1)}
                          className="w-full text-left px-3 py-1.5 hover:bg-muted transition-colors"
                        >
                          1 Day
                        </button>
                        <button
                          onClick={() => handleSnooze(task.id, 3)}
                          className="w-full text-left px-3 py-1.5 hover:bg-muted transition-colors"
                        >
                          3 Days
                        </button>
                        <button
                          onClick={() => handleSnooze(task.id, 7)}
                          className="w-full text-left px-3 py-1.5 hover:bg-muted transition-colors"
                        >
                          1 Week
                        </button>
                      </div>
                    </>
                  )}
                </div>
              </div>
            </div>
          </div>
        );
      })}
    </div>
  );
};
