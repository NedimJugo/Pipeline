import React, { useState, useEffect } from 'react';
import { X, Copy, Check, Mail, Sparkles, Send, RefreshCw } from 'lucide-react';
import { useTemplates, useRenderTemplate } from './useTemplates';
import { EmailTemplate, EmailTemplateCategory } from './types';

interface EmailTemplatePickerModalProps {
  isOpen: boolean;
  onClose: () => void;
  applicationId?: string | null;
  contactId?: string | null;
  recipientEmail?: string | null;
  recipientName?: string | null;
  defaultCategory?: EmailTemplateCategory;
}

export const EmailTemplatePickerModal: React.FC<EmailTemplatePickerModalProps> = ({
  isOpen,
  onClose,
  applicationId,
  contactId,
  recipientEmail,
  recipientName,
  defaultCategory,
}) => {
  const [selectedCategory, setSelectedCategory] = useState<EmailTemplateCategory | 'All'>(
    defaultCategory || 'All'
  );
  const [selectedTemplateId, setSelectedTemplateId] = useState<string>('');
  const [subject, setSubject] = useState<string>('');
  const [body, setBody] = useState<string>('');
  const [copiedType, setCopiedType] = useState<'all' | 'body' | null>(null);

  const { data: templates = [], isLoading: isLoadingTemplates } = useTemplates(
    selectedCategory === 'All' ? undefined : selectedCategory
  );
  const renderMutation = useRenderTemplate();

  useEffect(() => {
    if (defaultCategory) {
      setSelectedCategory(defaultCategory);
    }
  }, [defaultCategory]);

  useEffect(() => {
    if (templates.length > 0 && (!selectedTemplateId || !templates.some((t) => t.id === selectedTemplateId))) {
      setSelectedTemplateId(templates[0].id);
    }
  }, [templates, selectedTemplateId]);

  useEffect(() => {
    if (isOpen && selectedTemplateId) {
      renderMutation
        .mutateAsync({
          id: selectedTemplateId,
          request: {
            applicationId: applicationId || null,
            contactId: contactId || null,
          },
        })
        .then((res) => {
          setSubject(res.subject);
          setBody(res.body);
        })
        .catch((err) => {
          console.error('Failed to render template:', err);
          const t = templates.find((tpl) => tpl.id === selectedTemplateId);
          if (t) {
            setSubject(t.subject);
            setBody(t.body);
          }
        });
    }
  }, [isOpen, selectedTemplateId, applicationId, contactId]);

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

  const handleCopy = async (type: 'all' | 'body') => {
    try {
      const textToCopy = type === 'all' ? `Subject: ${subject}\n\n${body}` : body;
      await navigator.clipboard.writeText(textToCopy);
      setCopiedType(type);
      setTimeout(() => setCopiedType(null), 2000);
    } catch (err) {
      console.error('Failed to copy to clipboard', err);
    }
  };

  const handleMailto = () => {
    const to = recipientEmail ? encodeURIComponent(recipientEmail) : '';
    const sub = encodeURIComponent(subject);
    const bod = encodeURIComponent(body);
    window.open(`mailto:${to}?subject=${sub}&body=${bod}`, '_blank');
  };

  return (
    <div
      role="dialog"
      aria-modal="true"
      aria-labelledby="template-modal-title"
      className="fixed inset-0 z-50 flex items-center justify-center p-4 bg-black/60 backdrop-blur-sm animate-in fade-in duration-200"
      onClick={onClose}
    >
      <div
        className="relative w-full max-w-2xl bg-card border border-border rounded-xl shadow-2xl overflow-hidden flex flex-col max-h-[90vh]"
        onClick={(e) => e.stopPropagation()}
      >
        {/* Header */}
        <div className="flex items-center justify-between px-6 py-4 border-b border-border bg-muted/30">
          <div className="flex items-center gap-2.5">
            <div className="p-2 rounded-lg bg-indigo-500/10 text-indigo-500">
              <Mail className="h-5 w-5" />
            </div>
            <div>
              <h2 id="template-modal-title" className="text-lg font-bold tracking-tight">
                Email Templates
              </h2>
              <p className="text-xs text-muted-foreground">
                Personalized email ready to copy or open in your default email client
                {recipientName ? ` for ${recipientName}` : ''}
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

        {/* Content */}
        <div className="p-6 overflow-y-auto space-y-4 flex-1">
          {/* Category Tabs */}
          <div className="flex items-center gap-1.5 overflow-x-auto pb-1 text-xs">
            {(['All', 'FollowUp', 'ThankYou', 'Negotiation', 'Withdraw', 'Other'] as const).map(
              (cat) => (
                <button
                  key={cat}
                  onClick={() => setSelectedCategory(cat)}
                  className={`px-3 py-1.5 rounded-full font-medium transition-colors whitespace-nowrap ${
                    selectedCategory === cat
                      ? 'bg-primary text-primary-foreground shadow-sm'
                      : 'bg-muted/50 text-muted-foreground hover:bg-muted hover:text-foreground'
                  }`}
                >
                  {cat === 'All' ? 'All Templates' : cat}
                </button>
              )
            )}
          </div>

          {/* Template Selector */}
          <div className="space-y-1.5">
            <label className="text-xs font-semibold uppercase tracking-wider text-muted-foreground">
              Select Template
            </label>
            <select
              value={selectedTemplateId}
              onChange={(e) => setSelectedTemplateId(e.target.value)}
              className="w-full px-3 py-2 text-sm bg-background border border-border rounded-lg focus:outline-none focus:ring-2 focus:ring-primary/20"
              disabled={isLoadingTemplates}
            >
              {templates.map((tpl) => (
                <option key={tpl.id} value={tpl.id}>
                  {tpl.name} {tpl.isSystem ? '(Default)' : ''}
                </option>
              ))}
            </select>
          </div>

          {/* Subject Field */}
          <div className="space-y-1.5">
            <label className="text-xs font-semibold text-muted-foreground flex items-center justify-between">
              <span>Subject Line</span>
              {renderMutation.isPending && (
                <span className="flex items-center gap-1 text-[11px] text-indigo-500 font-normal">
                  <RefreshCw className="h-3 w-3 animate-spin" /> Rendering variables...
                </span>
              )}
            </label>
            <input
              type="text"
              value={subject}
              onChange={(e) => setSubject(e.target.value)}
              className="w-full px-3 py-2 text-sm bg-background border border-border rounded-lg focus:outline-none focus:ring-2 focus:ring-primary/20 font-medium"
              placeholder="Email subject line"
            />
          </div>

          {/* Body Field */}
          <div className="space-y-1.5">
            <label className="text-xs font-semibold text-muted-foreground flex items-center justify-between">
              <span>Message Body</span>
              <span className="text-[11px] text-muted-foreground">You can customize the text before sending</span>
            </label>
            <textarea
              rows={8}
              value={body}
              onChange={(e) => setBody(e.target.value)}
              className="w-full p-3 text-sm bg-background border border-border rounded-lg focus:outline-none focus:ring-2 focus:ring-primary/20 font-normal leading-relaxed resize-none"
              placeholder="Email body text"
            />
          </div>
        </div>

        {/* Footer Actions */}
        <div className="flex flex-col sm:flex-row items-center justify-between gap-3 px-6 py-4 border-t border-border bg-muted/20">
          <div className="flex items-center gap-2 w-full sm:w-auto">
            <button
              onClick={() => handleCopy('body')}
              className="flex-1 sm:flex-initial inline-flex items-center justify-center gap-1.5 px-3 py-2 border border-border rounded-lg text-xs font-semibold hover:bg-muted transition-colors"
            >
              {copiedType === 'body' ? (
                <>
                  <Check className="h-3.5 w-3.5 text-emerald-500" />
                  <span className="text-emerald-500">Body Copied!</span>
                </>
              ) : (
                <>
                  <Copy className="h-3.5 w-3.5 text-muted-foreground" />
                  <span>Copy Body</span>
                </>
              )}
            </button>

            <button
              onClick={() => handleCopy('all')}
              className="flex-1 sm:flex-initial inline-flex items-center justify-center gap-1.5 px-3 py-2 border border-border rounded-lg text-xs font-semibold hover:bg-muted transition-colors"
            >
              {copiedType === 'all' ? (
                <>
                  <Check className="h-3.5 w-3.5 text-emerald-500" />
                  <span className="text-emerald-500">Copied!</span>
                </>
              ) : (
                <>
                  <Copy className="h-3.5 w-3.5 text-muted-foreground" />
                  <span>Copy Subject & Body</span>
                </>
              )}
            </button>
          </div>

          <div className="flex items-center gap-2 w-full sm:w-auto">
            <button
              onClick={handleMailto}
              className="w-full sm:w-auto inline-flex items-center justify-center gap-2 px-4 py-2 bg-primary text-primary-foreground text-xs font-bold rounded-lg hover:bg-primary/90 shadow-sm transition-colors"
            >
              <Send className="h-3.5 w-3.5" />
              <span>Open in Mail App</span>
            </button>
          </div>
        </div>
      </div>
    </div>
  );
};
