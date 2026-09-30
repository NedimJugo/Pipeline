import React, { useState, useEffect } from 'react';
import { X, CheckCircle2, Calendar, Briefcase, User, Save } from 'lucide-react';
import { TaskItem, CreateTaskRequest, UpdateTaskRequest } from './types';
import { useCreateTask, useUpdateTask } from './useTasks';
import { useApplications } from '@/features/applications/useApplications';
import { useContacts } from '@/features/contacts/useContacts';

interface TaskModalProps {
  isOpen: boolean;
  onClose: () => void;
  taskToEdit?: TaskItem | null;
  initialApplicationId?: string | null;
  initialContactId?: string | null;
}

export const TaskModal: React.FC<TaskModalProps> = ({
  isOpen,
  onClose,
  taskToEdit,
  initialApplicationId,
  initialContactId,
}) => {
  const [title, setTitle] = useState('');
  const [notes, setNotes] = useState('');
  const [dueDate, setDueDate] = useState('');
  const [applicationId, setApplicationId] = useState<string>('');
  const [contactId, setContactId] = useState<string>('');

  const { data: applications = [] } = useApplications();
  const { data: contacts = [] } = useContacts();

  const createMutation = useCreateTask();
  const updateMutation = useUpdateTask();

  useEffect(() => {
    if (taskToEdit) {
      setTitle(taskToEdit.title);
      setNotes(taskToEdit.notes || '');
      setDueDate(taskToEdit.dueAt ? taskToEdit.dueAt.substring(0, 10) : '');
      setApplicationId(taskToEdit.applicationId || '');
      setContactId(taskToEdit.contactId || '');
    } else {
      setTitle('');
      setNotes('');
      // Default due today
      const today = new Date();
      setDueDate(today.toISOString().substring(0, 10));
      setApplicationId(initialApplicationId || '');
      setContactId(initialContactId || '');
    }
  }, [taskToEdit, isOpen, initialApplicationId, initialContactId]);

  useEffect(() => {
    const handleKeyDown = (e: KeyboardEvent) => {
      if (e.key === 'Escape' && isOpen) {
        onClose();
      }
    };
    window.addEventListener('keydown', handleKeyDown);
    return () => window.removeEventListener('keydown', handleKeyDown);
  }, [isOpen, onClose]);

  if (!isOpen) return null;

  const handleSubmit = async (e: React.FormEvent) => {
    e.preventDefault();
    if (!title.trim()) return;

    const dueAtIso = dueDate ? new Date(`${dueDate}T12:00:00Z`).toISOString() : null;

    if (taskToEdit) {
      const payload: UpdateTaskRequest = {
        title: title.trim(),
        notes: notes.trim() || null,
        dueAt: dueAtIso,
        applicationId: applicationId || null,
        contactId: contactId || null,
        interviewId: taskToEdit.interviewId || null,
      };
      await updateMutation.mutateAsync({ id: taskToEdit.id, request: payload });
    } else {
      const payload: CreateTaskRequest = {
        title: title.trim(),
        notes: notes.trim() || null,
        dueAt: dueAtIso,
        applicationId: applicationId || null,
        contactId: contactId || null,
      };
      await createMutation.mutateAsync(payload);
    }

    onClose();
  };

  return (
    <div
      role="dialog"
      aria-modal="true"
      aria-labelledby="task-modal-title"
      className="fixed inset-0 z-50 flex items-center justify-center p-4 bg-black/60 backdrop-blur-sm animate-in fade-in duration-200"
      onClick={onClose}
    >
      <div
        className="relative w-full max-w-lg bg-card border border-border rounded-xl shadow-2xl overflow-hidden flex flex-col max-h-[90vh]"
        onClick={(e) => e.stopPropagation()}
      >
        <div className="flex items-center justify-between px-6 py-4 border-b border-border bg-muted/30">
          <div className="flex items-center gap-2.5">
            <div className="p-2 rounded-lg bg-primary/10 text-primary">
              <CheckCircle2 className="h-5 w-5" />
            </div>
            <div>
              <h2 id="task-modal-title" className="text-base font-bold tracking-tight">
                {taskToEdit ? 'Edit Task' : 'Create New Task'}
              </h2>
              <p className="text-xs text-muted-foreground">
                Set a proactive action with deadline and linked context
              </p>
            </div>
          </div>

          <button
            onClick={onClose}
            aria-label="Close modal"
            className="p-2 text-muted-foreground hover:text-foreground rounded-lg hover:bg-muted/80 transition-colors"
          >
            <X className="h-5 w-5" />
          </button>
        </div>

        <form onSubmit={handleSubmit} className="p-6 overflow-y-auto space-y-4 flex-1">
          <div className="space-y-1.5">
            <label className="text-xs font-semibold text-muted-foreground">
              Task Title <span className="text-destructive">*</span>
            </label>
            <input
              type="text"
              required
              value={title}
              onChange={(e) => setTitle(e.target.value)}
              className="w-full px-3 py-2 text-sm bg-background border border-border rounded-lg focus:outline-none focus:ring-2 focus:ring-primary/20 font-medium"
              placeholder="e.g., Follow up with recruiter regarding next steps"
            />
          </div>

          <div className="space-y-1.5">
            <label className="text-xs font-semibold text-muted-foreground flex items-center gap-1.5">
              <Calendar className="h-3.5 w-3.5" />
              <span>Due Date</span>
            </label>
            <input
              type="date"
              value={dueDate}
              onChange={(e) => setDueDate(e.target.value)}
              className="w-full px-3 py-2 text-sm bg-background border border-border rounded-lg focus:outline-none focus:ring-2 focus:ring-primary/20"
            />
          </div>

          <div className="grid grid-cols-1 sm:grid-cols-2 gap-4">
            <div className="space-y-1.5">
              <label className="text-xs font-semibold text-muted-foreground flex items-center gap-1.5">
                <Briefcase className="h-3.5 w-3.5" />
                <span>Link Application</span>
              </label>
              <select
                value={applicationId}
                onChange={(e) => setApplicationId(e.target.value)}
                className="w-full px-3 py-2 text-sm bg-background border border-border rounded-lg focus:outline-none focus:ring-2 focus:ring-primary/20"
              >
                <option value="">None (General task)</option>
                {applications.map((app) => (
                  <option key={app.id} value={app.id}>
                    {app.roleTitle} @ {app.companyName}
                  </option>
                ))}
              </select>
            </div>

            <div className="space-y-1.5">
              <label className="text-xs font-semibold text-muted-foreground flex items-center gap-1.5">
                <User className="h-3.5 w-3.5" />
                <span>Link Contact</span>
              </label>
              <select
                value={contactId}
                onChange={(e) => setContactId(e.target.value)}
                className="w-full px-3 py-2 text-sm bg-background border border-border rounded-lg focus:outline-none focus:ring-2 focus:ring-primary/20"
              >
                <option value="">None</option>
                {contacts.map((contact) => (
                  <option key={contact.id} value={contact.id}>
                    {contact.fullName} {contact.role ? `(${contact.role})` : ''}
                  </option>
                ))}
              </select>
            </div>
          </div>

          <div className="space-y-1.5">
            <label className="text-xs font-semibold text-muted-foreground">Notes & Context</label>
            <textarea
              rows={4}
              value={notes}
              onChange={(e) => setNotes(e.target.value)}
              className="w-full p-3 text-sm bg-background border border-border rounded-lg focus:outline-none focus:ring-2 focus:ring-primary/20 leading-relaxed resize-none"
              placeholder="Any key details, links, or specific questions to ask..."
            />
          </div>

          <div className="flex items-center justify-end gap-3 pt-3 border-t border-border">
            <button
              type="button"
              onClick={onClose}
              className="px-4 py-2 text-xs font-semibold border border-border rounded-lg hover:bg-muted transition-colors"
            >
              Cancel
            </button>
            <button
              type="submit"
              disabled={createMutation.isPending || updateMutation.isPending}
              className="inline-flex items-center gap-2 px-4 py-2 bg-primary text-primary-foreground text-xs font-bold rounded-lg hover:bg-primary/90 transition-colors shadow-sm"
            >
              <Save className="h-4 w-4" />
              <span>{taskToEdit ? 'Save Changes' : 'Create Task'}</span>
            </button>
          </div>
        </form>
      </div>
    </div>
  );
};
