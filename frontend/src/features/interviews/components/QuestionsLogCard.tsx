import React, { useState } from 'react';
import {
  InterviewQuestion,
  InterviewQuestionCategory,
  AddQuestionPayload,
} from '../types';
import { useAddQuestion, useDeleteQuestion } from '../useInterviews';
import {
  HelpCircle,
  Plus,
  Star,
  CheckCircle,
  XCircle,
  Trash2,
  Tag,
  MessageSquare,
} from 'lucide-react';
import { clsx } from 'clsx';

interface QuestionsLogCardProps {
  interviewId: string;
  questions: InterviewQuestion[];
}

export const QuestionsLogCard: React.FC<QuestionsLogCardProps> = ({
  interviewId,
  questions,
}) => {
  const [isAdding, setIsAdding] = useState(false);
  const [questionText, setQuestionText] = useState('');
  const [myAnswer, setMyAnswer] = useState('');
  const [category, setCategory] = useState<InterviewQuestionCategory>('Behavioral');
  const [difficulty, setDifficulty] = useState(3);
  const [wasPrepared, setWasPrepared] = useState(true);

  const addMutation = useAddQuestion();
  const deleteMutation = useDeleteQuestion();

  const handleAddSubmit = async (e: React.FormEvent) => {
    e.preventDefault();
    if (!questionText.trim()) return;

    const payload: AddQuestionPayload = {
      question: questionText.trim(),
      myAnswer: myAnswer.trim() || undefined,
      category,
      difficulty,
      wasPrepared,
    };

    await addMutation.mutateAsync({ interviewId, payload });
    setQuestionText('');
    setMyAnswer('');
    setIsAdding(false);
  };

  const handleDelete = async (questionId: string) => {
    await deleteMutation.mutateAsync({ interviewId, questionId });
  };

  const getCategoryColor = (cat: InterviewQuestionCategory) => {
    switch (cat) {
      case 'Technical':
        return 'bg-blue-500/10 text-blue-600 dark:text-blue-400 border-blue-500/20';
      case 'Behavioral':
        return 'bg-purple-500/10 text-purple-600 dark:text-purple-400 border-purple-500/20';
      case 'Situational':
        return 'bg-amber-500/10 text-amber-600 dark:text-amber-400 border-amber-500/20';
      case 'Salary':
        return 'bg-emerald-500/10 text-emerald-600 dark:text-emerald-400 border-emerald-500/20';
      default:
        return 'bg-muted text-muted-foreground border-border';
    }
  };

  return (
    <div className="bg-card border border-border rounded-xl p-5 shadow-2xs space-y-5">
      {/* Header */}
      <div className="flex items-center justify-between border-b border-border pb-4">
        <div>
          <h3 className="font-bold text-sm text-foreground flex items-center gap-2">
            <HelpCircle className="h-4 w-4 text-primary" />
            <span>Questions Log & Talking Points</span>
          </h3>
          <p className="text-xs text-muted-foreground mt-0.5">
            Record questions asked during the round, answers given, and unexpected topics.
          </p>
        </div>

        {!isAdding && (
          <button
            type="button"
            onClick={() => setIsAdding(true)}
            className="inline-flex items-center gap-1.5 px-3 py-1.5 text-xs font-semibold bg-primary text-primary-foreground rounded-lg hover:opacity-90 shadow-2xs transition-opacity"
          >
            <Plus className="h-3.5 w-3.5" />
            <span>Add Question</span>
          </button>
        )}
      </div>

      {/* Add question form */}
      {isAdding && (
        <form
          onSubmit={handleAddSubmit}
          className="bg-muted/20 border border-primary/20 rounded-xl p-4 space-y-4 animate-in fade-in duration-150"
        >
          <div className="flex items-center justify-between">
            <span className="text-xs font-bold text-foreground">Log New Interview Question</span>
            <button
              type="button"
              onClick={() => setIsAdding(false)}
              className="text-muted-foreground hover:text-foreground text-xs"
            >
              Cancel
            </button>
          </div>

          <div>
            <label className="block text-xs font-semibold uppercase tracking-wider text-muted-foreground mb-1">
              Question Asked *
            </label>
            <input
              type="text"
              required
              placeholder="e.g. Tell me about a time you had a technical disagreement with a team lead..."
              value={questionText}
              onChange={(e) => setQuestionText(e.target.value)}
              className="w-full px-3 py-1.5 text-xs bg-background border border-border rounded-lg focus:outline-none focus:ring-2 focus:ring-primary"
            />
          </div>

          <div className="grid grid-cols-1 sm:grid-cols-3 gap-3">
            <div>
              <label className="block text-xs font-semibold uppercase tracking-wider text-muted-foreground mb-1">
                Category
              </label>
              <select
                value={category}
                onChange={(e) => setCategory(e.target.value as InterviewQuestionCategory)}
                className="w-full px-3 py-1.5 text-xs bg-background border border-border rounded-lg focus:outline-none focus:ring-2 focus:ring-primary"
              >
                <option value="Behavioral">Behavioral (STAR)</option>
                <option value="Technical">Technical / Coding</option>
                <option value="Situational">Situational Scenario</option>
                <option value="Salary">Compensation / Expectations</option>
                <option value="Other">General / Other</option>
              </select>
            </div>

            <div>
              <label className="block text-xs font-semibold uppercase tracking-wider text-muted-foreground mb-1">
                Difficulty (1 - 5)
              </label>
              <div className="flex items-center gap-1 mt-1">
                {[1, 2, 3, 4, 5].map((star) => (
                  <button
                    key={star}
                    type="button"
                    onClick={() => setDifficulty(star)}
                    className="p-1 hover:scale-110 transition-transform"
                  >
                    <Star
                      className={clsx(
                        'h-4 w-4',
                        star <= difficulty
                          ? 'fill-amber-400 text-amber-400'
                          : 'text-muted-foreground/30'
                      )}
                    />
                  </button>
                ))}
              </div>
            </div>

            <div className="flex items-center pt-4">
              <label className="flex items-center gap-2 cursor-pointer text-xs">
                <input
                  type="checkbox"
                  checked={wasPrepared}
                  onChange={(e) => setWasPrepared(e.target.checked)}
                  className="rounded border-border text-primary focus:ring-primary h-4 w-4"
                />
                <span className="font-semibold text-foreground">Felt Prepared</span>
              </label>
            </div>
          </div>

          <div>
            <label className="block text-xs font-semibold uppercase tracking-wider text-muted-foreground mb-1">
              Your Answer & Key Talking Points (Optional)
            </label>
            <textarea
              rows={2}
              placeholder="Summary of your response, examples cited, or interviewer's reaction..."
              value={myAnswer}
              onChange={(e) => setMyAnswer(e.target.value)}
              className="w-full px-3 py-1.5 text-xs bg-background border border-border rounded-lg focus:outline-none focus:ring-2 focus:ring-primary resize-none"
            />
          </div>

          <div className="flex items-center justify-end gap-2 pt-1">
            <button
              type="button"
              onClick={() => setIsAdding(false)}
              className="px-3 py-1.5 text-xs font-medium text-muted-foreground hover:bg-muted rounded-lg"
            >
              Cancel
            </button>
            <button
              type="submit"
              disabled={!questionText.trim() || addMutation.isPending}
              className="px-4 py-1.5 text-xs font-semibold bg-primary text-primary-foreground rounded-lg hover:opacity-90 shadow-2xs disabled:opacity-50"
            >
              {addMutation.isPending ? 'Saving...' : 'Save Question'}
            </button>
          </div>
        </form>
      )}

      {/* Questions List */}
      <div className="space-y-3">
        {questions.length === 0 ? (
          <div className="p-8 text-center bg-muted/20 border border-dashed border-border rounded-xl space-y-2">
            <HelpCircle className="h-6 w-6 text-muted-foreground/40 mx-auto" />
            <h4 className="text-xs font-bold text-foreground">No questions recorded yet</h4>
            <p className="text-xs text-muted-foreground max-w-sm mx-auto">
              During or after your interview, log the questions asked to build your personal question bank for future rounds.
            </p>
          </div>
        ) : (
          questions.map((q) => (
            <div
              key={q.id}
              className="bg-background border border-border rounded-xl p-4 shadow-2xs space-y-2.5 hover:border-primary/30 transition-colors"
            >
              <div className="flex items-start justify-between gap-3">
                <div className="space-y-1 flex-1">
                  <div className="flex items-center gap-2 flex-wrap">
                    <span
                      className={clsx(
                        'px-2 py-0.5 rounded-full text-[10px] font-bold border',
                        getCategoryColor(q.category)
                      )}
                    >
                      {q.category}
                    </span>

                    <div className="flex items-center gap-0.5">
                      {[1, 2, 3, 4, 5].map((star) => (
                        <Star
                          key={star}
                          className={clsx(
                            'h-3 w-3',
                            star <= q.difficulty
                              ? 'fill-amber-400 text-amber-400'
                              : 'text-muted-foreground/20'
                          )}
                        />
                      ))}
                    </div>

                    <span
                      className={clsx(
                        'px-2 py-0.5 rounded text-[10px] font-semibold flex items-center gap-1',
                        q.wasPrepared
                          ? 'bg-emerald-500/10 text-emerald-600 dark:text-emerald-400'
                          : 'bg-rose-500/10 text-rose-600 dark:text-rose-400'
                      )}
                    >
                      {q.wasPrepared ? (
                        <>
                          <CheckCircle className="h-3 w-3" />
                          <span>Prepared</span>
                        </>
                      ) : (
                        <>
                          <XCircle className="h-3 w-3" />
                          <span>Unprepared</span>
                        </>
                      )}
                    </span>
                  </div>

                  <h4 className="text-xs font-bold text-foreground leading-snug">
                    {q.question}
                  </h4>
                </div>

                <button
                  type="button"
                  onClick={() => handleDelete(q.id)}
                  disabled={deleteMutation.isPending}
                  className="p-1 text-muted-foreground hover:text-destructive hover:bg-destructive/10 rounded transition-colors"
                  title="Delete question"
                >
                  <Trash2 className="h-3.5 w-3.5" />
                </button>
              </div>

              {q.myAnswer && (
                <div className="bg-muted/40 p-2.5 rounded-lg text-xs text-foreground/90 border border-border/60">
                  <span className="text-[10px] font-bold uppercase tracking-wider text-muted-foreground block mb-0.5">
                    Your Response
                  </span>
                  <p className="whitespace-pre-wrap">{q.myAnswer}</p>
                </div>
              )}
            </div>
          ))
        )}
      </div>
    </div>
  );
};
