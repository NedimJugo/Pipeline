import React from 'react';
import { ContactFilter, ContactType, ContactWarmth } from '../types';
import { Search, UserPlus, Filter, X } from 'lucide-react';
import { clsx } from 'clsx';

interface ContactsFilterBarProps {
  filter: ContactFilter;
  onFilterChange: (filter: ContactFilter) => void;
  onNewContactClick: () => void;
  totalCount: number;
}

export const ContactsFilterBar: React.FC<ContactsFilterBarProps> = ({
  filter,
  onFilterChange,
  onNewContactClick,
  totalCount,
}) => {
  const warmthOptions: { val: ContactWarmth | undefined; label: string }[] = [
    { val: undefined, label: 'All Warmth' },
    { val: 'Hot', label: '🔥 Hot (<14d)' },
    { val: 'Warm', label: '⚡ Warm (14-30d)' },
    { val: 'Cooling', label: '❄️ Cooling (31-60d)' },
    { val: 'Cold', label: '🧊 Cold (>60d)' },
  ];

  const handleSearchChange = (e: React.ChangeEvent<HTMLInputElement>) => {
    onFilterChange({ ...filter, search: e.target.value });
  };

  const handleTypeChange = (e: React.ChangeEvent<HTMLSelectElement>) => {
    const val = e.target.value as ContactType | '';
    onFilterChange({ ...filter, type: val ? val : undefined });
  };

  const handleWarmthSelect = (warmth: ContactWarmth | undefined) => {
    onFilterChange({ ...filter, warmth });
  };

  const hasActiveFilters = Boolean(filter.search || filter.type || filter.warmth);

  const resetFilters = () => {
    onFilterChange({});
  };

  return (
    <div className="bg-card border border-border rounded-xl p-4 shadow-2xs space-y-3">
      <div className="flex flex-col md:flex-row items-center justify-between gap-3">
        {/* Search input */}
        <div className="relative w-full md:w-80">
          <Search className="h-4 w-4 absolute left-3 top-2.5 text-muted-foreground" />
          <input
            type="text"
            placeholder="Search contacts by name, company, role, email..."
            value={filter.search || ''}
            onChange={handleSearchChange}
            className="w-full pl-9 pr-3 py-1.5 text-xs bg-background border border-border rounded-lg focus:outline-none focus:ring-2 focus:ring-primary transition-all"
          />
        </div>

        {/* Right action controls */}
        <div className="flex items-center gap-2.5 w-full md:w-auto justify-end">
          {/* Contact type select */}
          <div className="flex items-center gap-1.5">
            <Filter className="h-3.5 w-3.5 text-muted-foreground shrink-0 hidden sm:block" />
            <select
              value={filter.type || ''}
              onChange={handleTypeChange}
              className="px-2.5 py-1.5 text-xs bg-background border border-border rounded-lg focus:outline-none focus:ring-2 focus:ring-primary text-foreground"
            >
              <option value="">All Contact Types</option>
              <option value="Recruiter">Recruiter</option>
              <option value="HiringManager">Hiring Manager</option>
              <option value="Interviewer">Interviewer</option>
              <option value="Referrer">Referrer</option>
              <option value="Peer">Peer</option>
              <option value="Other">Other</option>
            </select>
          </div>

          {hasActiveFilters && (
            <button
              type="button"
              onClick={resetFilters}
              className="flex items-center gap-1 px-2.5 py-1.5 text-xs text-muted-foreground hover:text-foreground bg-muted/60 hover:bg-muted rounded-lg transition-colors"
              title="Reset all filters"
            >
              <X className="h-3.5 w-3.5" />
              <span className="hidden sm:inline">Reset</span>
            </button>
          )}

          <button
            type="button"
            onClick={onNewContactClick}
            className="inline-flex items-center gap-1.5 px-3.5 py-1.5 text-xs font-semibold bg-primary text-primary-foreground rounded-lg hover:opacity-90 shadow-sm transition-opacity"
          >
            <UserPlus className="h-4 w-4" />
            <span>Add Contact</span>
          </button>
        </div>
      </div>

      {/* Warmth pills filter */}
      <div className="flex items-center gap-1.5 overflow-x-auto pt-1 pb-0.5">
        <span className="text-[11px] font-bold uppercase tracking-wider text-muted-foreground shrink-0 mr-1">
          Warmth:
        </span>
        {warmthOptions.map((opt) => (
          <button
            key={opt.label}
            type="button"
            onClick={() => handleWarmthSelect(opt.val)}
            className={clsx(
              'px-2.5 py-1 rounded-full text-xs font-medium border whitespace-nowrap transition-colors',
              filter.warmth === opt.val
                ? 'bg-primary text-primary-foreground border-primary shadow-2xs'
                : 'bg-muted/40 text-muted-foreground border-border hover:bg-muted hover:text-foreground'
            )}
          >
            {opt.label}
          </button>
        ))}
      </div>
    </div>
  );
};
