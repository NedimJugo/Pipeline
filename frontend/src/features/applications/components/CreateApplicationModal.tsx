import React, { useState } from 'react';
import { CreateApplicationPayload, ApplicationStatus, WorkMode, EmploymentType, ApplicationSource } from '../types';
import { useCreateApplication } from '../useApplications';
import { X, Building2, Briefcase, DollarSign, Star, Link as LinkIcon, Calendar } from 'lucide-react';

interface CreateApplicationModalProps {
  isOpen: boolean;
  onClose: () => void;
}

export const CreateApplicationModal: React.FC<CreateApplicationModalProps> = ({ isOpen, onClose }) => {
  const todayStr = new Date().toISOString().split('T')[0];
  const [roleTitle, setRoleTitle] = useState('');
  const [companyName, setCompanyName] = useState('');
  const [jobUrl, setJobUrl] = useState('');
  const [status, setStatus] = useState<ApplicationStatus>('Applied');
  const [appliedAt, setAppliedAt] = useState<string>(todayStr);
  const [dateError, setDateError] = useState<string | null>(null);
  const [workMode, setWorkMode] = useState<WorkMode>('Remote');
  const [employmentType, setEmploymentType] = useState<EmploymentType>('FullTime');
  const [source, setSource] = useState<ApplicationSource>('LinkedIn');
  const [salaryMin, setSalaryMin] = useState<string>('');
  const [salaryMax, setSalaryMax] = useState<string>('');
  const [currency, setCurrency] = useState('USD');
  const [priority, setPriority] = useState<number>(2);
  const [excitementRating, setExcitementRating] = useState<number>(3);
  const [jobDescription, setJobDescription] = useState('');
  const [notes, setNotes] = useState('');
  const [pros, setPros] = useState('');
  const [cons, setCons] = useState('');

  const createMutation = useCreateApplication();

  if (!isOpen) return null;

  const handleSubmit = async (e: React.FormEvent) => {
    e.preventDefault();
    if (!roleTitle.trim() || !companyName.trim()) return;

    if (appliedAt && appliedAt > todayStr) {
      setDateError('Application date cannot be in the future.');
      return;
    }

    const payload: CreateApplicationPayload = {
      roleTitle: roleTitle.trim(),
      companyName: companyName.trim(),
      jobUrl: jobUrl.trim() || undefined,
      status,
      appliedAt: appliedAt ? new Date(appliedAt).toISOString() : undefined,
      workMode,
      employmentType,
      source,
      salaryMin: salaryMin ? parseFloat(salaryMin) : undefined,
      salaryMax: salaryMax ? parseFloat(salaryMax) : undefined,
      currency,
      priority,
      excitementRating,
      jobDescription: jobDescription.trim() || undefined,
      notes: notes.trim() || undefined,
      pros: pros.trim() || undefined,
      cons: cons.trim() || undefined,
    };

    await createMutation.mutateAsync(payload);
    onClose();
  };

  return (
    <div className="fixed inset-0 z-50 flex items-center justify-center p-4 bg-background/80 backdrop-blur-sm">
      <div className="border border-border bg-card max-w-2xl w-full rounded-xl shadow-2xl overflow-hidden flex flex-col max-h-[90vh]">
        {/* Header */}
        <div className="px-6 py-4 border-b border-border flex items-center justify-between">
          <div className="flex items-center gap-2">
            <Briefcase className="h-5 w-5 text-primary" />
            <h2 className="font-bold text-base tracking-tight">New Job Application</h2>
          </div>
          <button
            type="button"
            onClick={onClose}
            className="p-1 rounded-lg text-muted-foreground hover:text-foreground hover:bg-muted"
          >
            <X className="h-4 w-4" />
          </button>
        </div>

        {/* Form Body */}
        <form onSubmit={handleSubmit} className="flex-1 overflow-y-auto p-6 space-y-4">
          <div className="grid grid-cols-1 sm:grid-cols-2 gap-4">
            <div>
              <label className="block text-xs font-semibold uppercase tracking-wider text-muted-foreground mb-1">
                Company Name *
              </label>
              <div className="relative">
                <Building2 className="h-4 w-4 absolute left-3 top-2.5 text-muted-foreground" />
                <input
                  type="text"
                  required
                  placeholder="e.g. Stripe, OpenAI, Figma"
                  value={companyName}
                  onChange={(e) => setCompanyName(e.target.value)}
                  className="w-full pl-9 pr-3 py-1.5 text-xs bg-background border border-border rounded-lg focus:outline-none focus:ring-2 focus:ring-primary"
                />
              </div>
            </div>

            <div>
              <label className="block text-xs font-semibold uppercase tracking-wider text-muted-foreground mb-1">
                Role Title *
              </label>
              <input
                type="text"
                required
                placeholder="e.g. Senior Software Engineer"
                value={roleTitle}
                onChange={(e) => setRoleTitle(e.target.value)}
                className="w-full px-3 py-1.5 text-xs bg-background border border-border rounded-lg focus:outline-none focus:ring-2 focus:ring-primary"
              />
            </div>
          </div>

          <div className="grid grid-cols-1 sm:grid-cols-2 lg:grid-cols-4 gap-4">
            <div>
              <label className="block text-xs font-semibold uppercase tracking-wider text-muted-foreground mb-1">
                Status
              </label>
              <select
                value={status}
                onChange={(e) => setStatus(e.target.value as ApplicationStatus)}
                className="w-full px-3 py-1.5 text-xs bg-background border border-border rounded-lg focus:outline-none focus:ring-2 focus:ring-primary"
              >
                <option value="Wishlist">Wishlist</option>
                <option value="Applied">Applied</option>
                <option value="Screening">Screening</option>
                <option value="Interview">Interview</option>
                <option value="Assignment">Assignment</option>
                <option value="Offer">Offer</option>
                <option value="Accepted">Accepted</option>
              </select>
            </div>

            <div>
              <label htmlFor="create-app-date" className="block text-xs font-semibold uppercase tracking-wider text-muted-foreground mb-1">
                Application Date
              </label>
              <div className="relative">
                <Calendar className="h-4 w-4 absolute left-3 top-2.5 text-muted-foreground pointer-events-none" />
                <input
                  id="create-app-date"
                  type="date"
                  max={todayStr}
                  value={appliedAt}
                  onChange={(e) => {
                    setAppliedAt(e.target.value);
                    if (e.target.value > todayStr) {
                      setDateError('Application date cannot be in the future.');
                    } else {
                      setDateError(null);
                    }
                  }}
                  className={`w-full pl-9 pr-3 py-1.5 text-xs bg-background border rounded-lg focus:outline-none focus:ring-2 focus:ring-primary ${
                    dateError ? 'border-destructive' : 'border-border'
                  }`}
                />
              </div>
              {dateError && <p className="text-[10px] text-destructive mt-1">{dateError}</p>}
            </div>

            <div>
              <label className="block text-xs font-semibold uppercase tracking-wider text-muted-foreground mb-1">
                Work Mode
              </label>
              <select
                value={workMode}
                onChange={(e) => setWorkMode(e.target.value as WorkMode)}
                className="w-full px-3 py-1.5 text-xs bg-background border border-border rounded-lg focus:outline-none focus:ring-2 focus:ring-primary"
              >
                <option value="Remote">Remote</option>
                <option value="Hybrid">Hybrid</option>
                <option value="Onsite">Onsite</option>
              </select>
            </div>

            <div>
              <label className="block text-xs font-semibold uppercase tracking-wider text-muted-foreground mb-1">
                Source
              </label>
              <select
                value={source}
                onChange={(e) => setSource(e.target.value as ApplicationSource)}
                className="w-full px-3 py-1.5 text-xs bg-background border border-border rounded-lg focus:outline-none focus:ring-2 focus:ring-primary"
              >
                <option value="LinkedIn">LinkedIn</option>
                <option value="CompanyWebsite">Company Website</option>
                <option value="Referral">Referral</option>
                <option value="Recruiter">Recruiter</option>
                <option value="JobBoard">Job Board</option>
                <option value="Event">Event</option>
                <option value="Other">Other</option>
              </select>
            </div>
          </div>

          <div>
            <label className="block text-xs font-semibold uppercase tracking-wider text-muted-foreground mb-1">
              Job URL
            </label>
            <div className="relative">
              <LinkIcon className="h-4 w-4 absolute left-3 top-2.5 text-muted-foreground" />
              <input
                type="url"
                placeholder="https://..."
                value={jobUrl}
                onChange={(e) => setJobUrl(e.target.value)}
                className="w-full pl-9 pr-3 py-1.5 text-xs bg-background border border-border rounded-lg focus:outline-none focus:ring-2 focus:ring-primary"
              />
            </div>
          </div>

          {/* Salary & Priority */}
          <div className="grid grid-cols-1 sm:grid-cols-4 gap-4">
            <div>
              <label className="block text-xs font-semibold uppercase tracking-wider text-muted-foreground mb-1">
                Min Salary
              </label>
              <input
                type="number"
                placeholder="120000"
                value={salaryMin}
                onChange={(e) => setSalaryMin(e.target.value)}
                className="w-full px-3 py-1.5 text-xs bg-background border border-border rounded-lg focus:outline-none focus:ring-2 focus:ring-primary"
              />
            </div>
            <div>
              <label className="block text-xs font-semibold uppercase tracking-wider text-muted-foreground mb-1">
                Max Salary
              </label>
              <input
                type="number"
                placeholder="150000"
                value={salaryMax}
                onChange={(e) => setSalaryMax(e.target.value)}
                className="w-full px-3 py-1.5 text-xs bg-background border border-border rounded-lg focus:outline-none focus:ring-2 focus:ring-primary"
              />
            </div>
            <div>
              <label className="block text-xs font-semibold uppercase tracking-wider text-muted-foreground mb-1">
                Currency
              </label>
              <input
                type="text"
                value={currency}
                onChange={(e) => setCurrency(e.target.value)}
                className="w-full px-3 py-1.5 text-xs bg-background border border-border rounded-lg focus:outline-none focus:ring-2 focus:ring-primary"
              />
            </div>
            <div>
              <label className="block text-xs font-semibold uppercase tracking-wider text-muted-foreground mb-1">
                Priority
              </label>
              <select
                value={priority}
                onChange={(e) => setPriority(parseInt(e.target.value))}
                className="w-full px-3 py-1.5 text-xs bg-background border border-border rounded-lg focus:outline-none focus:ring-2 focus:ring-primary"
              >
                <option value={3}>High</option>
                <option value={2}>Medium</option>
                <option value={1}>Low</option>
              </select>
            </div>
          </div>

          {/* Excitement Rating */}
          <div>
            <label className="block text-xs font-semibold uppercase tracking-wider text-muted-foreground mb-1">
              Excitement Rating
            </label>
            <div className="flex items-center gap-1.5">
              {[1, 2, 3, 4, 5].map((star) => (
                <button
                  type="button"
                  key={star}
                  onClick={() => setExcitementRating(star)}
                  className="p-1 text-amber-500 hover:scale-110 transition-transform"
                >
                  <Star className={`h-5 w-5 ${star <= excitementRating ? 'fill-current' : 'opacity-30'}`} />
                </button>
              ))}
              <span className="text-xs text-muted-foreground ml-2 font-medium">
                {excitementRating === 5 ? 'Dream Role' : excitementRating >= 4 ? 'Very Excited' : 'Good Opportunity'}
              </span>
            </div>
          </div>

          {/* Job Description (Preserved permanently) */}
          <div>
            <label className="block text-xs font-semibold uppercase tracking-wider text-muted-foreground mb-1">
              Job Description (paste text for permanent archive)
            </label>
            <textarea
              rows={3}
              placeholder="Paste job posting details here..."
              value={jobDescription}
              onChange={(e) => setJobDescription(e.target.value)}
              className="w-full px-3 py-2 text-xs bg-background border border-border rounded-lg focus:outline-none focus:ring-2 focus:ring-primary"
            />
          </div>

          <div className="flex items-center justify-end gap-3 pt-4 border-t border-border">
            <button
              type="button"
              onClick={onClose}
              className="px-4 py-2 border border-border rounded-lg text-xs font-semibold hover:bg-muted text-foreground transition-colors"
            >
              Cancel
            </button>
            <button
              type="submit"
              disabled={createMutation.isPending}
              className="px-4 py-2 bg-primary text-primary-foreground rounded-lg text-xs font-semibold hover:bg-primary/90 transition-colors disabled:opacity-50"
            >
              {createMutation.isPending ? 'Saving...' : 'Add to Pipeline'}
            </button>
          </div>
        </form>
      </div>
    </div>
  );
};
