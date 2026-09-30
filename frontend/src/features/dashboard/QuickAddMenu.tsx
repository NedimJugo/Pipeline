import React, { useState } from 'react';
import { Plus, Briefcase, User, MessageSquare, CheckCircle2, ChevronDown } from 'lucide-react';
import { useNavigate } from 'react-router-dom';
import { TaskModal } from '@/features/tasks/TaskModal';

export const QuickAddMenu: React.FC = () => {
  const [isOpen, setIsOpen] = useState(false);
  const [isTaskModalOpen, setIsTaskModalOpen] = useState(false);
  const navigate = useNavigate();

  return (
    <>
      <div className="relative">
        <button
          onClick={() => setIsOpen(!isOpen)}
          className="inline-flex items-center gap-2 px-4 py-2 bg-primary text-primary-foreground text-sm font-semibold rounded-lg hover:bg-primary/90 shadow-sm transition-colors"
        >
          <Plus className="h-4 w-4" />
          <span>Quick Add</span>
          <ChevronDown className={`h-3.5 w-3.5 transition-transform ${isOpen ? 'rotate-180' : ''}`} />
        </button>

        {isOpen && (
          <>
            <div className="fixed inset-0 z-20" onClick={() => setIsOpen(false)} />
            <div className="absolute right-0 mt-2 w-48 bg-card border border-border rounded-xl shadow-xl py-1.5 z-30 text-xs animate-in fade-in zoom-in-95 duration-100">
              <button
                onClick={() => {
                  setIsOpen(false);
                  navigate('/applications');
                }}
                className="w-full text-left px-3.5 py-2 hover:bg-muted flex items-center gap-2.5 font-medium transition-colors"
              >
                <Briefcase className="h-4 w-4 text-indigo-500" />
                <span>New Application</span>
              </button>

              <button
                onClick={() => {
                  setIsOpen(false);
                  navigate('/contacts');
                }}
                className="w-full text-left px-3.5 py-2 hover:bg-muted flex items-center gap-2.5 font-medium transition-colors"
              >
                <User className="h-4 w-4 text-emerald-500" />
                <span>New Contact</span>
              </button>

              <button
                onClick={() => {
                  setIsOpen(false);
                  setIsTaskModalOpen(true);
                }}
                className="w-full text-left px-3.5 py-2 hover:bg-muted flex items-center gap-2.5 font-medium transition-colors"
              >
                <CheckCircle2 className="h-4 w-4 text-amber-500" />
                <span>New Task</span>
              </button>

              <button
                onClick={() => {
                  setIsOpen(false);
                  navigate('/contacts');
                }}
                className="w-full text-left px-3.5 py-2 hover:bg-muted flex items-center gap-2.5 font-medium transition-colors"
              >
                <MessageSquare className="h-4 w-4 text-violet-500" />
                <span>Log Interaction</span>
              </button>
            </div>
          </>
        )}
      </div>

      <TaskModal
        isOpen={isTaskModalOpen}
        onClose={() => setIsTaskModalOpen(false)}
      />
    </>
  );
};
