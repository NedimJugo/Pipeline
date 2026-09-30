import React, { useState } from 'react';
import {
  CheckCircle2,
  Plus,
  Search,
  Filter,
  RefreshCw,
  Zap,
  Calendar,
  Clock,
  Sparkles,
} from 'lucide-react';
import { useTasks } from './useTasks';
import { useEvaluateAutomationRules } from '@/features/dashboard/useDashboard';
import { TaskItem, TaskSource } from './types';
import { TaskCard } from './TaskCard';
import { TaskModal } from './TaskModal';

export const TasksPage: React.FC = () => {
  const [activeView, setActiveView] = useState<'today' | 'upcoming' | 'overdue' | 'done' | 'all'>('today');
  const [sourceFilter, setSourceFilter] = useState<TaskSource | 'all'>('all');
  const [search, setSearch] = useState('');
  const [isTaskModalOpen, setIsTaskModalOpen] = useState(false);
  const [taskToEdit, setTaskToEdit] = useState<TaskItem | null>(null);

  const { data: tasksData, isLoading, refetch } = useTasks({
    view: activeView,
    source: sourceFilter === 'all' ? undefined : sourceFilter,
    search: search.trim() || undefined,
  });

  const evaluateMutation = useEvaluateAutomationRules();

  const handleEvaluate = async () => {
    await evaluateMutation.mutateAsync();
    refetch();
  };

  const tasks = tasksData?.items || [];

  return (
    <div className="max-w-6xl mx-auto space-y-8">
      {/* Header Banner */}
      <div className="flex flex-col sm:flex-row sm:items-center justify-between gap-4 pb-6 border-b border-border">
        <div>
          <h1 className="text-2xl font-bold tracking-tight">Tasks & Next Actions</h1>
          <p className="text-sm text-muted-foreground mt-1">
            Prioritized follow-ups, interview prep reminders, and system-generated next actions
          </p>
        </div>

        <div className="flex items-center gap-3">
          <button
            onClick={handleEvaluate}
            disabled={evaluateMutation.isPending}
            className="inline-flex items-center gap-1.5 px-3 py-2 bg-secondary text-secondary-foreground text-xs font-semibold rounded-lg hover:bg-secondary/80 transition-colors shadow-2xs"
            title="Scan active applications and contacts for next actions"
          >
            <RefreshCw
              className={`h-3.5 w-3.5 ${evaluateMutation.isPending ? 'animate-spin text-primary' : ''}`}
            />
            <span>Check Rules</span>
          </button>

          <button
            onClick={() => {
              setTaskToEdit(null);
              setIsTaskModalOpen(true);
            }}
            className="inline-flex items-center gap-2 px-4 py-2 bg-primary text-primary-foreground text-sm font-semibold rounded-lg hover:bg-primary/90 shadow-sm transition-colors"
          >
            <Plus className="h-4 w-4" />
            <span>New Task</span>
          </button>
        </div>
      </div>

      {/* Filter and View Bar */}
      <div className="flex flex-col md:flex-row md:items-center justify-between gap-4">
        {/* View Tabs */}
        <div className="flex items-center gap-1.5 overflow-x-auto pb-1 text-xs">
          {[
            { key: 'today', label: 'Today', icon: CheckCircle2 },
            { key: 'upcoming', label: 'Upcoming', icon: Calendar },
            { key: 'overdue', label: 'Overdue', icon: Clock },
            { key: 'done', label: 'Completed', icon: CheckCircle2 },
            { key: 'all', label: 'All Tasks', icon: Filter },
          ].map(({ key, label, icon: Icon }) => (
            <button
              key={key}
              onClick={() => setActiveView(key as typeof activeView)}
              className={`inline-flex items-center gap-1.5 px-3 py-1.5 rounded-full font-medium transition-colors whitespace-nowrap ${
                activeView === key
                  ? 'bg-primary text-primary-foreground shadow-sm'
                  : 'bg-muted/50 text-muted-foreground hover:bg-muted hover:text-foreground'
              }`}
            >
              <Icon className="h-3.5 w-3.5" />
              <span>{label}</span>
            </button>
          ))}
        </div>

        {/* Source & Search Filters */}
        <div className="flex items-center gap-3">
          <select
            value={sourceFilter}
            onChange={(e) => setSourceFilter(e.target.value as typeof sourceFilter)}
            className="px-3 py-1.5 text-xs bg-card border border-border rounded-lg focus:outline-none focus:ring-2 focus:ring-primary/20"
          >
            <option value="all">All Sources</option>
            <option value="Auto">Automated Rules</option>
            <option value="Manual">Manual Tasks</option>
          </select>

          <div className="relative w-full sm:w-56">
            <Search className="absolute left-2.5 top-1/2 -translate-y-1/2 h-3.5 w-3.5 text-muted-foreground" />
            <input
              type="text"
              value={search}
              onChange={(e) => setSearch(e.target.value)}
              placeholder="Search tasks..."
              className="w-full pl-8 pr-3 py-1.5 text-xs bg-card border border-border rounded-lg focus:outline-none focus:ring-2 focus:ring-primary/20"
            />
          </div>
        </div>
      </div>

      {/* Task List */}
      {isLoading ? (
        <div className="space-y-3">
          {[1, 2, 3, 4].map((i) => (
            <div key={i} className="h-20 rounded-xl border border-border bg-card/40 animate-pulse" />
          ))}
        </div>
      ) : tasks.length === 0 ? (
        <div className="border border-dashed border-border rounded-xl p-12 text-center">
          <CheckCircle2 className="h-8 w-8 text-muted-foreground mx-auto mb-2 opacity-50" />
          <h3 className="font-semibold text-sm">No tasks in this view</h3>
          <p className="text-xs text-muted-foreground mt-1 max-w-sm mx-auto">
            {activeView === 'today'
              ? 'You are all caught up for today! Click "Check Rules" or create a new task.'
              : 'No matching tasks found. Change filters or add a new action.'}
          </p>
        </div>
      ) : (
        <div className="space-y-3">
          {tasks.map((task) => (
            <TaskCard
              key={task.id}
              task={task}
              onEdit={(t) => {
                setTaskToEdit(t);
                setIsTaskModalOpen(true);
              }}
            />
          ))}
        </div>
      )}

      {/* Task Create / Edit Modal */}
      <TaskModal
        isOpen={isTaskModalOpen}
        onClose={() => setIsTaskModalOpen(false)}
        taskToEdit={taskToEdit}
      />
    </div>
  );
};
