import React, { useState, useEffect } from 'react';
import { X, Save, Sparkles } from 'lucide-react';
import { EmailTemplate, EmailTemplateCategory, CreateEmailTemplateRequest } from './types';
import { useCreateTemplate, useUpdateTemplate } from './useTemplates';

interface TemplateModalProps {
  isOpen: boolean;
  onClose: () => void;
  templateToEdit?: EmailTemplate | null;
}

export const TemplateModal: React.FC<TemplateModalProps> = ({
  isOpen,
  onClose,
  templateToEdit,
}) => {
  const [name, setName] = useState('');
  const [category, setCategory] = useState<EmailTemplateCategory>('FollowUp');
  const [subject, setSubject] = useState('');
  const [body, setBody] = useState('');

  const createMutation = useCreateTemplate();
  const updateMutation = useUpdateTemplate();

  useEffect(() => {
    if (templateToEdit) {
      setName(templateToEdit.name);
      setCategory(templateToEdit.category);
      setSubject(templateToEdit.subject);
      setBody(templateToEdit.body);
    } else {
      setName('');
      setCategory('FollowUp');
      setSubject('');
      setBody('');
    }
  }, [templateToEdit, isOpen]);

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

  const insertVariable = (variable: string) => {
    setBody((prev) => `${prev}${variable}`);
  };

  const handleSubmit = async (e: React.FormEvent) => {
    e.preventDefault();
    if (!name.trim() || !subject.trim() || !body.trim()) return;

    const payload: CreateEmailTemplateRequest = {
      name: name.trim(),
      category,
      subject: subject.trim(),
      body: body.trim(),
    };

    if (templateToEdit) {
      await updateMutation.mutateAsync({ id: templateToEdit.id, request: payload });
    } else {
      await createMutation.mutateAsync(payload);
    }

    onClose();
  };

  return (
    <div
      role="dialog"
      aria-modal="true"
      aria-labelledby="template-edit-modal-title"
      className="fixed inset-0 z-50 flex items-center justify-center p-4 bg-black/60 backdrop-blur-sm animate-in fade-in duration-200"
      onClick={onClose}
    >
      <div
        className="relative w-full max-w-xl bg-card border border-border rounded-xl shadow-2xl overflow-hidden flex flex-col max-h-[90vh]"
        onClick={(e) => e.stopPropagation()}
      >
        <div className="flex items-center justify-between px-6 py-4 border-b border-border bg-muted/30">
          <h2 id="template-edit-modal-title" className="text-lg font-bold tracking-tight">
            {templateToEdit ? 'Edit Email Template' : 'Create Email Template'}
          </h2>
          <button
            onClick={onClose}
            aria-label="Close modal"
            className="p-2 text-muted-foreground hover:text-foreground rounded-lg hover:bg-muted/80 transition-colors"
          >
            <X className="h-5 w-5" />
          </button>
        </div>

        <form onSubmit={handleSubmit} className="p-6 overflow-y-auto space-y-4 flex-1">
          <div className="grid grid-cols-1 sm:grid-cols-2 gap-4">
            <div className="space-y-1.5">
              <label className="text-xs font-semibold text-muted-foreground">Template Name</label>
              <input
                type="text"
                required
                value={name}
                onChange={(e) => setName(e.target.value)}
                className="w-full px-3 py-2 text-sm bg-background border border-border rounded-lg focus:outline-none focus:ring-2 focus:ring-primary/20"
                placeholder="e.g., Senior Role Follow-up"
              />
            </div>

            <div className="space-y-1.5">
              <label className="text-xs font-semibold text-muted-foreground">Category</label>
              <select
                value={category}
                onChange={(e) => setCategory(e.target.value as EmailTemplateCategory)}
                className="w-full px-3 py-2 text-sm bg-background border border-border rounded-lg focus:outline-none focus:ring-2 focus:ring-primary/20"
              >
                <option value="FollowUp">Follow-up</option>
                <option value="ThankYou">Thank You</option>
                <option value="Negotiation">Negotiation</option>
                <option value="Withdraw">Withdraw</option>
                <option value="Other">Other</option>
              </select>
            </div>
          </div>

          <div className="space-y-1.5">
            <label className="text-xs font-semibold text-muted-foreground">Subject Line</label>
            <input
              type="text"
              required
              value={subject}
              onChange={(e) => setSubject(e.target.value)}
              className="w-full px-3 py-2 text-sm bg-background border border-border rounded-lg focus:outline-none focus:ring-2 focus:ring-primary/20"
              placeholder="e.g., Following up on {{role}} at {{company}}"
            />
          </div>

          <div className="space-y-1.5">
            <div className="flex items-center justify-between">
              <label className="text-xs font-semibold text-muted-foreground">Body</label>
              <div className="flex items-center gap-1 text-[11px] text-muted-foreground">
                <span>Insert:</span>
                {['{{contactName}}', '{{company}}', '{{role}}', '{{myName}}'].map((v) => (
                  <button
                    type="button"
                    key={v}
                    onClick={() => insertVariable(v)}
                    className="px-1.5 py-0.5 rounded bg-muted hover:bg-muted-foreground/20 text-foreground font-mono text-[10px] transition-colors"
                  >
                    {v}
                  </button>
                ))}
              </div>
            </div>
            <textarea
              rows={8}
              required
              value={body}
              onChange={(e) => setBody(e.target.value)}
              className="w-full p-3 text-sm bg-background border border-border rounded-lg focus:outline-none focus:ring-2 focus:ring-primary/20 leading-relaxed resize-none"
              placeholder="Write your email template body here. Use variables like {{contactName}}, {{company}}, {{role}}, {{myName}}..."
            />
          </div>

          <div className="flex items-center justify-end gap-3 pt-2">
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
              <span>{templateToEdit ? 'Save Changes' : 'Create Template'}</span>
            </button>
          </div>
        </form>
      </div>
    </div>
  );
};
