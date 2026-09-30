import React, { useState } from 'react';
import {
  Mail,
  Plus,
  Search,
  Copy,
  Edit2,
  Trash2,
  Sparkles,
  ExternalLink,
  ShieldCheck,
  Send,
} from 'lucide-react';
import { useTemplates, useDeleteTemplate } from './useTemplates';
import { EmailTemplate, EmailTemplateCategory } from './types';
import { TemplateModal } from './TemplateModal';
import { EmailTemplatePickerModal } from './EmailTemplatePickerModal';

export const TemplatesPage: React.FC = () => {
  const [selectedCategory, setSelectedCategory] = useState<EmailTemplateCategory | 'All'>('All');
  const [search, setSearch] = useState('');
  const [templateToEdit, setTemplateToEdit] = useState<EmailTemplate | null>(null);
  const [isEditModalOpen, setIsEditModalOpen] = useState(false);
  const [selectedTemplateForPicker, setSelectedTemplateForPicker] = useState<EmailTemplate | null>(null);
  const [isPickerOpen, setIsPickerOpen] = useState(false);

  const { data: templates = [], isLoading } = useTemplates(
    selectedCategory === 'All' ? undefined : selectedCategory
  );
  const deleteMutation = useDeleteTemplate();

  const filteredTemplates = templates.filter((t) => {
    if (!search.trim()) return true;
    const q = search.toLowerCase();
    return (
      t.name.toLowerCase().includes(q) ||
      t.subject.toLowerCase().includes(q) ||
      t.body.toLowerCase().includes(q)
    );
  });

  const handleDelete = async (id: string, name: string) => {
    if (window.confirm(`Are you sure you want to delete template "${name}"?`)) {
      await deleteMutation.mutateAsync(id);
    }
  };

  return (
    <div className="max-w-6xl mx-auto space-y-8">
      {/* Header Banner */}
      <div className="flex flex-col sm:flex-row sm:items-center justify-between gap-4 pb-6 border-b border-border">
        <div>
          <h1 className="text-2xl font-bold tracking-tight">Email Templates</h1>
          <p className="text-sm text-muted-foreground mt-1">
            Pre-written messages with auto-filled placeholders ready to copy or open in your email app
          </p>
        </div>

        <button
          onClick={() => {
            setTemplateToEdit(null);
            setIsEditModalOpen(true);
          }}
          className="inline-flex items-center gap-2 px-4 py-2 bg-primary text-primary-foreground text-sm font-semibold rounded-lg hover:bg-primary/90 shadow-sm transition-colors self-start sm:self-auto"
        >
          <Plus className="h-4 w-4" />
          <span>New Template</span>
        </button>
      </div>

      {/* Filter Toolbar */}
      <div className="flex flex-col sm:flex-row items-center justify-between gap-4">
        {/* Category Tabs */}
        <div className="flex items-center gap-1.5 overflow-x-auto w-full sm:w-auto pb-1 text-xs">
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

        {/* Search */}
        <div className="relative w-full sm:w-64">
          <Search className="absolute left-3 top-1/2 -translate-y-1/2 h-4 w-4 text-muted-foreground" />
          <input
            type="text"
            value={search}
            onChange={(e) => setSearch(e.target.value)}
            placeholder="Search templates..."
            className="w-full pl-9 pr-4 py-1.5 text-xs bg-card border border-border rounded-lg focus:outline-none focus:ring-2 focus:ring-primary/20"
          />
        </div>
      </div>

      {/* Grid of Templates */}
      {isLoading ? (
        <div className="grid grid-cols-1 md:grid-cols-2 lg:grid-cols-3 gap-4">
          {[1, 2, 3, 4, 5, 6].map((i) => (
            <div key={i} className="h-48 rounded-xl border border-border bg-card/40 animate-pulse" />
          ))}
        </div>
      ) : filteredTemplates.length === 0 ? (
        <div className="border border-dashed border-border rounded-xl p-12 text-center">
          <Mail className="h-8 w-8 text-muted-foreground mx-auto mb-2 opacity-50" />
          <h3 className="font-semibold text-sm">No templates found</h3>
          <p className="text-xs text-muted-foreground mt-1 max-w-sm mx-auto">
            {search
              ? 'Try modifying your search query or switching categories.'
              : 'Create your first custom email template to speed up your communications.'}
          </p>
        </div>
      ) : (
        <div className="grid grid-cols-1 md:grid-cols-2 lg:grid-cols-3 gap-5">
          {filteredTemplates.map((template) => (
            <div
              key={template.id}
              className="flex flex-col justify-between p-5 rounded-xl border border-border bg-card/60 shadow-sm hover:border-border/80 transition-all group"
            >
              <div className="space-y-3">
                <div className="flex items-start justify-between gap-2">
                  <div>
                    <h3 className="font-bold text-sm tracking-tight group-hover:text-primary transition-colors">
                      {template.name}
                    </h3>
                    <div className="flex items-center gap-2 mt-1">
                      <span className="inline-flex items-center px-2 py-0.5 rounded-full text-[10px] font-semibold bg-primary/10 text-primary">
                        {template.category}
                      </span>
                      {template.isSystem ? (
                        <span className="inline-flex items-center gap-1 text-[10px] text-muted-foreground font-medium">
                          <ShieldCheck className="h-3 w-3 text-indigo-500" /> Default
                        </span>
                      ) : (
                        <span className="text-[10px] text-muted-foreground">Custom</span>
                      )}
                    </div>
                  </div>

                  <div className="flex items-center gap-1 opacity-80 group-hover:opacity-100 transition-opacity">
                    {!template.isSystem && (
                      <>
                        <button
                          onClick={() => {
                            setTemplateToEdit(template);
                            setIsEditModalOpen(true);
                          }}
                          className="p-1.5 text-muted-foreground hover:text-foreground rounded-lg hover:bg-muted transition-colors"
                          title="Edit template"
                        >
                          <Edit2 className="h-3.5 w-3.5" />
                        </button>
                        <button
                          onClick={() => handleDelete(template.id, template.name)}
                          className="p-1.5 text-muted-foreground hover:text-destructive rounded-lg hover:bg-destructive/10 transition-colors"
                          title="Delete template"
                        >
                          <Trash2 className="h-3.5 w-3.5" />
                        </button>
                      </>
                    )}
                  </div>
                </div>

                <div className="space-y-1">
                  <span className="text-[11px] font-semibold text-muted-foreground uppercase tracking-wider block">
                    Subject
                  </span>
                  <p className="text-xs font-medium line-clamp-1 bg-muted/40 p-1.5 rounded border border-border/50">
                    {template.subject}
                  </p>
                </div>

                <div className="space-y-1">
                  <span className="text-[11px] font-semibold text-muted-foreground uppercase tracking-wider block">
                    Body Snippet
                  </span>
                  <p className="text-xs text-muted-foreground line-clamp-3 leading-relaxed">
                    {template.body}
                  </p>
                </div>
              </div>

              <div className="pt-4 mt-4 border-t border-border flex items-center justify-end">
                <button
                  onClick={() => {
                    setSelectedTemplateForPicker(template);
                    setIsPickerOpen(true);
                  }}
                  className="w-full inline-flex items-center justify-center gap-1.5 px-3 py-2 bg-secondary text-secondary-foreground text-xs font-semibold rounded-lg hover:bg-secondary/80 transition-colors"
                >
                  <Send className="h-3.5 w-3.5" />
                  <span>Preview & Use</span>
                </button>
              </div>
            </div>
          ))}
        </div>
      )}

      {/* Template Modals */}
      <TemplateModal
        isOpen={isEditModalOpen}
        onClose={() => setIsEditModalOpen(false)}
        templateToEdit={templateToEdit}
      />

      <EmailTemplatePickerModal
        isOpen={isPickerOpen}
        onClose={() => setIsPickerOpen(false)}
        defaultCategory={selectedTemplateForPicker?.category}
      />
    </div>
  );
};
