import React, { useState } from 'react';
import { useNavigate } from 'react-router-dom';
import { ApplicationListItem } from '../types';
import { ChevronUp, ChevronDown, Calendar, Star } from 'lucide-react';
import { clsx } from 'clsx';

interface ApplicationsTableProps {
  applications: ApplicationListItem[];
}

type SortField =
  | 'roleTitle'
  | 'companyName'
  | 'status'
  | 'workMode'
  | 'salaryMax'
  | 'appliedAt'
  | 'priority'
  | 'daysInStage';

export const ApplicationsTable: React.FC<ApplicationsTableProps> = ({ applications }) => {
  const navigate = useNavigate();
  const [sortField, setSortField] = useState<SortField>('daysInStage');
  const [sortAsc, setSortAsc] = useState<boolean>(false);

  const handleSort = (field: SortField) => {
    if (sortField === field) {
      setSortAsc(!sortAsc);
    } else {
      setSortField(field);
      setSortAsc(true);
    }
  };

  const sortedApplications = [...applications].sort((a, b) => {
    let aVal: any = a[sortField];
    let bVal: any = b[sortField];

    if (aVal === undefined || aVal === null) aVal = '';
    if (bVal === undefined || bVal === null) bVal = '';

    if (typeof aVal === 'string') {
      return sortAsc ? aVal.localeCompare(bVal) : bVal.localeCompare(aVal);
    }

    return sortAsc ? (aVal > bVal ? 1 : -1) : (aVal < bVal ? 1 : -1);
  });

  const statusBadge = (status: string) => {
    const isOffer = status === 'Offer' || status === 'Accepted';
    const isInterview = status === 'Interview' || status === 'Screening';
    const isClosed = ['Rejected', 'Withdrawn', 'Ghosted', 'Declined'].includes(status);

    return (
      <span
        className={clsx(
          'inline-flex items-center px-2 py-0.5 rounded-full text-xs font-semibold border',
          isOffer
            ? 'bg-emerald-500/10 text-emerald-500 border-emerald-500/20'
            : isInterview
            ? 'bg-violet-500/10 text-violet-500 border-violet-500/20'
            : isClosed
            ? 'bg-destructive/10 text-destructive border-destructive/20'
            : 'bg-muted text-muted-foreground border-border'
        )}
      >
        {status}
      </span>
    );
  };

  const priorityBadge = (priority: number) => {
    const text = priority === 3 ? 'High' : priority === 2 ? 'Medium' : 'Low';
    const color =
      priority === 3
        ? 'text-rose-500 bg-rose-500/10 border-rose-500/20'
        : priority === 2
        ? 'text-amber-500 bg-amber-500/10 border-amber-500/20'
        : 'text-slate-400 bg-slate-400/10 border-slate-400/20';

    return (
      <span className={clsx('inline-flex items-center px-1.5 py-0.5 rounded text-[11px] font-medium border', color)}>
        {text}
      </span>
    );
  };

  return (
    <div className="border border-border rounded-xl bg-card overflow-hidden shadow-sm">
      <div className="overflow-x-auto">
        <table className="w-full text-left text-sm border-collapse">
          <thead>
            <tr className="border-b border-border bg-muted/40 text-xs font-semibold text-muted-foreground uppercase tracking-wider select-none">
              <th
                className="py-3 px-4 cursor-pointer hover:text-foreground"
                onClick={() => handleSort('companyName')}
              >
                <div className="flex items-center gap-1">
                  <span>Company</span>
                  {sortField === 'companyName' && (sortAsc ? <ChevronUp className="h-3 w-3" /> : <ChevronDown className="h-3 w-3" />)}
                </div>
              </th>
              <th
                className="py-3 px-4 cursor-pointer hover:text-foreground"
                onClick={() => handleSort('roleTitle')}
              >
                <div className="flex items-center gap-1">
                  <span>Role</span>
                  {sortField === 'roleTitle' && (sortAsc ? <ChevronUp className="h-3 w-3" /> : <ChevronDown className="h-3 w-3" />)}
                </div>
              </th>
              <th
                className="py-3 px-4 cursor-pointer hover:text-foreground"
                onClick={() => handleSort('status')}
              >
                <div className="flex items-center gap-1">
                  <span>Status</span>
                  {sortField === 'status' && (sortAsc ? <ChevronUp className="h-3 w-3" /> : <ChevronDown className="h-3 w-3" />)}
                </div>
              </th>
              <th
                className="py-3 px-4 cursor-pointer hover:text-foreground"
                onClick={() => handleSort('workMode')}
              >
                <div className="flex items-center gap-1">
                  <span>Mode</span>
                  {sortField === 'workMode' && (sortAsc ? <ChevronUp className="h-3 w-3" /> : <ChevronDown className="h-3 w-3" />)}
                </div>
              </th>
              <th
                className="py-3 px-4 cursor-pointer hover:text-foreground"
                onClick={() => handleSort('salaryMax')}
              >
                <div className="flex items-center gap-1">
                  <span>Salary Range</span>
                  {sortField === 'salaryMax' && (sortAsc ? <ChevronUp className="h-3 w-3" /> : <ChevronDown className="h-3 w-3" />)}
                </div>
              </th>
              <th
                className="py-3 px-4 cursor-pointer hover:text-foreground"
                onClick={() => handleSort('daysInStage')}
              >
                <div className="flex items-center gap-1">
                  <span>Days In Stage</span>
                  {sortField === 'daysInStage' && (sortAsc ? <ChevronUp className="h-3 w-3" /> : <ChevronDown className="h-3 w-3" />)}
                </div>
              </th>
              <th
                className="py-3 px-4 cursor-pointer hover:text-foreground"
                onClick={() => handleSort('priority')}
              >
                <div className="flex items-center gap-1">
                  <span>Priority</span>
                  {sortField === 'priority' && (sortAsc ? <ChevronUp className="h-3 w-3" /> : <ChevronDown className="h-3 w-3" />)}
                </div>
              </th>
              <th className="py-3 px-4">Excitement</th>
            </tr>
          </thead>
          <tbody className="divide-y divide-border">
            {sortedApplications.length === 0 ? (
              <tr>
                <td colSpan={8} className="py-12 text-center text-muted-foreground text-sm">
                  No applications found matching your criteria.
                </td>
              </tr>
            ) : (
              sortedApplications.map((app) => (
                <tr
                  key={app.id}
                  onClick={() => navigate(`/applications/${app.id}`)}
                  className="hover:bg-muted/50 cursor-pointer transition-colors"
                >
                  <td className="py-3 px-4 font-semibold text-foreground">
                    <div className="flex items-center gap-2">
                      <span className="h-6 w-6 rounded bg-secondary text-secondary-foreground text-[10px] font-bold flex items-center justify-center shrink-0 border border-border">
                        {app.companyName.slice(0, 2).toUpperCase()}
                      </span>
                      <span>{app.companyName}</span>
                    </div>
                  </td>
                  <td className="py-3 px-4">
                    <span className="font-medium text-foreground hover:text-primary transition-colors block">
                      {app.roleTitle}
                    </span>
                    {app.nextInterviewDate && (
                      <span className="text-[11px] text-indigo-500 font-medium flex items-center gap-1 mt-0.5">
                        <Calendar className="h-3 w-3" />
                        Interview {new Date(app.nextInterviewDate).toLocaleDateString([], { month: 'short', day: 'numeric' })}
                      </span>
                    )}
                  </td>
                  <td className="py-3 px-4">{statusBadge(app.status)}</td>
                  <td className="py-3 px-4 text-xs text-muted-foreground">{app.workMode}</td>
                  <td className="py-3 px-4 text-xs font-mono">
                    {app.salaryMin && app.salaryMax
                      ? `${app.currency} ${(app.salaryMin / 1000).toFixed(0)}k - ${(app.salaryMax / 1000).toFixed(0)}k`
                      : '—'}
                  </td>
                  <td className="py-3 px-4 text-xs text-muted-foreground">
                    {app.daysInStage === 0 ? 'Today' : `${app.daysInStage} days`}
                  </td>
                  <td className="py-3 px-4">{priorityBadge(app.priority)}</td>
                  <td className="py-3 px-4">
                    <div className="flex items-center gap-0.5 text-amber-500/90">
                      {Array.from({ length: 5 }).map((_, i) => (
                        <Star
                          key={i}
                          className={clsx('h-2.5 w-2.5', i < app.excitementRating ? 'fill-current' : 'opacity-20')}
                        />
                      ))}
                    </div>
                  </td>
                </tr>
              ))
            )}
          </tbody>
        </table>
      </div>
    </div>
  );
};
