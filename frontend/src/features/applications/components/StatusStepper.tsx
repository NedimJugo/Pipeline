import React from 'react';
import { ApplicationStatus } from '../types';
import { Check } from 'lucide-react';
import { clsx } from 'clsx';

interface StatusStepperProps {
  currentStatus: ApplicationStatus;
  onStatusSelect: (status: ApplicationStatus) => void;
  disabled?: boolean;
}

const pipelineStages: ApplicationStatus[] = [
  'Wishlist',
  'Applied',
  'Screening',
  'Interview',
  'Assignment',
  'Offer',
  'Accepted',
];

const terminalStatuses: ApplicationStatus[] = ['Rejected', 'Withdrawn', 'Ghosted', 'Declined'];

export const StatusStepper: React.FC<StatusStepperProps> = ({
  currentStatus,
  onStatusSelect,
  disabled = false,
}) => {
  const isTerminal = terminalStatuses.includes(currentStatus);
  const currentIndex = pipelineStages.indexOf(currentStatus);

  return (
    <div className="flex flex-col gap-2">
      <div className="flex items-center overflow-x-auto py-2">
        {pipelineStages.map((stage, idx) => {
          const isPassed = !isTerminal && currentIndex > idx;
          const isCurrent = !isTerminal && currentIndex === idx;

          return (
            <React.Fragment key={stage}>
              <button
                type="button"
                disabled={disabled}
                onClick={() => onStatusSelect(stage)}
                className={clsx(
                  'flex items-center gap-1.5 px-3 py-1.5 rounded-lg text-xs font-semibold whitespace-nowrap transition-all select-none',
                  isCurrent
                    ? 'bg-primary text-primary-foreground shadow-sm scale-105'
                    : isPassed
                    ? 'bg-primary/10 text-primary hover:bg-primary/20'
                    : 'bg-muted/60 text-muted-foreground hover:bg-muted hover:text-foreground'
                )}
              >
                <div
                  className={clsx(
                    'h-4 w-4 rounded-full flex items-center justify-center text-[10px]',
                    isCurrent
                      ? 'bg-white text-primary font-bold'
                      : isPassed
                      ? 'bg-primary text-white'
                      : 'border border-current'
                  )}
                >
                  {isPassed ? <Check className="h-2.5 w-2.5 stroke-[3]" /> : idx + 1}
                </div>
                <span>{stage}</span>
              </button>

              {idx < pipelineStages.length - 1 && (
                <div
                  className={clsx(
                    'h-0.5 w-4 sm:w-6 shrink-0 transition-colors mx-1',
                    isPassed ? 'bg-primary' : 'bg-border'
                  )}
                />
              )}
            </React.Fragment>
          );
        })}
      </div>

      {isTerminal && (
        <div className="inline-flex items-center gap-2 px-3 py-1 rounded-md bg-destructive/10 border border-destructive/20 text-destructive text-xs font-semibold w-fit">
          <span>Terminal status: {currentStatus}</span>
        </div>
      )}
    </div>
  );
};
