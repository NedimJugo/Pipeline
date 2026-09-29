import React, { useState } from 'react';
import { PrepChecklistItem } from '../types';
import { useUpdatePrepChecklist } from '../useInterviews';
import { CheckSquare, Square, Plus, Sparkles, CheckCircle2 } from 'lucide-react';
import { clsx } from 'clsx';

interface PrepChecklistCardProps {
  interviewId: string;
  checklist: PrepChecklistItem[];
}

export const PrepChecklistCard: React.FC<PrepChecklistCardProps> = ({
  interviewId,
  checklist,
}) => {
  const [newText, setNewText] = useState('');
  const updateMutation = useUpdatePrepChecklist();

  const totalCount = checklist.length;
  const completedCount = checklist.filter((item) => item.done).length;
  const progressPercent = totalCount > 0 ? Math.round((completedCount / totalCount) * 100) : 0;

  const handleToggle = async (index: number) => {
    const updated = checklist.map((item, idx) =>
      idx === index ? { ...item, done: !item.done } : item
    );
    await updateMutation.mutateAsync({
      id: interviewId,
      payload: { checklist: updated },
    });
  };

  const handleAddItem = async (e: React.FormEvent) => {
    e.preventDefault();
    if (!newText.trim()) return;

    const updated = [...checklist, { text: newText.trim(), done: false }];
    setNewText('');
    await updateMutation.mutateAsync({
      id: interviewId,
      payload: { checklist: updated },
    });
  };

  const handleDeleteItem = async (index: number) => {
    const updated = checklist.filter((_, idx) => idx !== index);
    await updateMutation.mutateAsync({
      id: interviewId,
      payload: { checklist: updated },
    });
  };

  return (
    <div className="bg-card border border-border rounded-xl p-5 shadow-2xs space-y-5">
      {/* Header and Progress */}
      <div className="flex flex-col sm:flex-row sm:items-center justify-between gap-3 border-b border-border pb-4">
        <div>
          <h3 className="font-bold text-sm text-foreground flex items-center gap-2">
            <CheckCircle2 className="h-4 w-4 text-primary" />
            <span>Interview Preparation Checklist</span>
          </h3>
          <p className="text-xs text-muted-foreground mt-0.5">
            Round-specific checklist tailored to help you perform at your peak.
          </p>
        </div>

        <div className="flex items-center gap-3 shrink-0">
          <div className="text-right">
            <span className="text-xs font-bold text-foreground font-mono">
              {completedCount} / {totalCount}
            </span>
            <span className="text-[11px] text-muted-foreground ml-1.5">({progressPercent}%)</span>
          </div>

          <div className="w-24 h-2 bg-muted rounded-full overflow-hidden border border-border">
            <div
              className={clsx(
                'h-full transition-all duration-300',
                progressPercent === 100 ? 'bg-emerald-500' : 'bg-primary'
              )}
              style={{ width: `${progressPercent}%` }}
            />
          </div>
        </div>
      </div>

      {/* Checklist items */}
      <div className="space-y-2">
        {checklist.length === 0 ? (
          <div className="p-6 text-center text-xs text-muted-foreground bg-muted/20 border border-dashed border-border rounded-lg">
            No checklist items yet. Add custom tasks below.
          </div>
        ) : (
          checklist.map((item, index) => {
            return (
              <div
                key={index}
                className={clsx(
                  'flex items-start justify-between gap-3 p-3 rounded-lg border transition-all group',
                  item.done
                    ? 'bg-muted/30 border-border/50 text-muted-foreground'
                    : 'bg-background border-border text-foreground hover:border-primary/40'
                )}
              >
                <label className="flex items-start gap-3 cursor-pointer flex-1 min-w-0">
                  <button
                    type="button"
                    onClick={() => handleToggle(index)}
                    disabled={updateMutation.isPending}
                    className="mt-0.5 shrink-0 text-primary hover:opacity-80 transition-opacity"
                  >
                    {item.done ? (
                      <CheckSquare className="h-4 w-4 text-emerald-500" />
                    ) : (
                      <Square className="h-4 w-4 text-muted-foreground" />
                    )}
                  </button>

                  <span
                    className={clsx(
                      'text-xs leading-relaxed select-none break-words',
                      item.done && 'line-through text-muted-foreground/80'
                    )}
                  >
                    {item.text}
                  </span>
                </label>

                <button
                  type="button"
                  onClick={() => handleDeleteItem(index)}
                  className="opacity-0 group-hover:opacity-100 text-muted-foreground hover:text-destructive text-[11px] transition-opacity shrink-0 px-1"
                  title="Remove item"
                >
                  ✕
                </button>
              </div>
            );
          })
        )}
      </div>

      {/* Add custom item form */}
      <form onSubmit={handleAddItem} className="flex items-center gap-2 pt-2">
        <input
          type="text"
          placeholder="Add custom prep task (e.g. review Q3 financial filings, test LeetCode 206)..."
          value={newText}
          onChange={(e) => setNewText(e.target.value)}
          className="flex-1 px-3 py-1.5 text-xs bg-background border border-border rounded-lg focus:outline-none focus:ring-2 focus:ring-primary"
        />
        <button
          type="submit"
          disabled={!newText.trim() || updateMutation.isPending}
          className="inline-flex items-center gap-1 px-3 py-1.5 text-xs font-semibold bg-primary text-primary-foreground rounded-lg hover:opacity-90 transition-opacity shadow-2xs disabled:opacity-50"
        >
          <Plus className="h-3.5 w-3.5" />
          <span>Add</span>
        </button>
      </form>
    </div>
  );
};
