import React, { useState, useEffect } from 'react';
import {
  ApplicationDetail,
  WorkMode,
  EmploymentType,
  ApplicationSource,
  CreateApplicationPayload,
} from '../types';
import { useUpdateApplication } from '../useApplications';
import { X, Building2, Briefcase, DollarSign, Star, Link as LinkIcon, ThumbsUp, ThumbsDown, FileText } from 'lucide-react';

interface EditApplicationModalProps {
  isOpen: boolean;
  onClose: () => void;
  application: ApplicationDetail;
}

export const EditApplicationModal: React.FC<EditApplicationModalProps> = ({
  isOpen,
  onClose,
  application,
}) => {
  const [roleTitle, setRoleTitle] = useState(application.roleTitle);
  const [companyName, setCompanyName] = useState(application.companyName);
  const [jobUrl, setJobUrl] = useState(application.jobUrl || '');
  const [workMode, setWorkMode] = useState<WorkMode>(application.workMode);
  const [employmentType, setEmploymentType] = useState<EmploymentType>(application.employmentType);
  const [source, setSource] = useState<ApplicationSource>(application.source);
  const [location, setLocation] = useState(application.location || '');
  const [salaryMin, setSalaryMin] = useState<string>(
    application.salaryMin ? application.salaryMin.toString() : ''
  );
  const [salaryMax, setSalaryMax] = useState<string>(
    application.salaryMax ? application.salaryMax.toString() : ''
  );
  const [currency, setCurrency] = useState(application.currency || 'USD');
  const [priority, setPriority] = useState<number>(application.priority);
  const [excitementRating, setExcitementRating] = useState<number>(application.excitementRating);
  const [favorite, setFavorite] = useState<boolean>(application.favorite);
  const [jobDescription, setJobDescription] = useState(application.jobDescription || '');
  const [notes, setNotes] = useState(application.notes || '');
  const [pros, setPros] = useState(application.pros || '');
  const [cons, setCons] = useState(application.cons || '');

  // Reset state when application changes
  useEffect(() => {
    setRoleTitle(application.roleTitle);
    setCompanyName(application.companyName);
    setJobUrl(application.jobUrl || '');
    setWorkMode(application.workMode);
    setEmploymentType(application.employmentType);
    setSource(application.source);
    setLocation(application.location || '');
    setSalaryMin(application.salaryMin ? application.salaryMin.toString() : '');
    setSalaryMax(application.salaryMax ? application.salaryMax.toString() : '');
    setCurrency(application.currency || 'USD');
    setPriority(application.priority);
    setExcitementRating(application.excitementRating);
    setFavorite(application.favorite);
    setJobDescription(application.jobDescription || '');
    setNotes(application.notes || '');
    setPros(application.pros || '');
    setCons(application.cons || '');
  }, [application]);

  const updateMutation = useUpdateApplication();

  if (!isOpen) return null;

  const handleSubmit = async (e: React.FormEvent) => {
    e.preventDefault();
    if (!roleTitle.trim() || !companyName.trim()) return;

    const payload: Partial<CreateApplicationPayload> = {
      roleTitle: roleTitle.trim(),
      companyName: companyName.trim(),
      jobUrl: jobUrl.trim() || null,
      workMode,
      employmentType,
      source,
      location: location.trim() || null,
      salaryMin: salaryMin ? parseFloat(salaryMin) : null,
      salaryMax: salaryMax ? parseFloat(salaryMax) : null,
      currency,
      priority,
      excitementRating,
      favorite,
      jobDescription: jobDescription.trim() || null,
      notes: notes.trim() || null,
      pros: pros.trim() || null,
      cons: cons.trim() || null,
    };

    await updateMutation.mutateAsync({ id: application.id, payload });
    onClose();
  };

  return (
    <div className="fixed inset-0 z-50 flex items-center justify-center p-4 bg-background/80 backdrop-blur-sm animate-in fade-in duration-200">
      <div className="border border-border bg-card max-w-2xl w-full rounded-xl shadow-2xl overflow-hidden flex flex-col max-h-[90vh]">
        {/* Header */}
        <div className="px-6 py-4 border-b border-border flex items-center justify-between">
          <div className="flex items-center gap-2">
            <Briefcase className="h-5 w-5 text-primary" />
            <h2 className="font-bold text-base tracking-tight">Edit Application</h2>
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
                value={roleTitle}
                onChange={(e) => setRoleTitle(e.target.value)}
                className="w-full px-3 py-1.5 text-xs bg-background border border-border rounded-lg focus:outline-none focus:ring-2 focus:ring-primary"
              />
            </div>
          </div>

          <div className="grid grid-cols-1 sm:grid-cols-3 gap-4">
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
                Employment Type
              </label>
              <select
                value={employmentType}
                onChange={(e) => setEmploymentType(e.target.value as EmploymentType)}
                className="w-full px-3 py-1.5 text-xs bg-background border border-border rounded-lg focus:outline-none focus:ring-2 focus:ring-primary"
              >
                <option value="FullTime">Full Time</option>
                <option value="PartTime">Part Time</option>
                <option value="Contract">Contract</option>
                <option value="Internship">Internship</option>
                <option value="Freelance">Freelance</option>
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

          <div className="grid grid-cols-1 sm:grid-cols-2 gap-4">
            <div>
              <label className="block text-xs font-semibold uppercase tracking-wider text-muted-foreground mb-1">
                Job Posting URL
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

            <div>
              <label className="block text-xs font-semibold uppercase tracking-wider text-muted-foreground mb-1">
                Location
              </label>
              <input
                type="text"
                placeholder="e.g. San Francisco, CA or London / Remote"
                value={location}
                onChange={(e) => setLocation(e.target.value)}
                className="w-full px-3 py-1.5 text-xs bg-background border border-border rounded-lg focus:outline-none focus:ring-2 focus:ring-primary"
              />
            </div>
          </div>

          {/* Salary Expectation */}
          <div>
            <label className="block text-xs font-semibold uppercase tracking-wider text-muted-foreground mb-1">
              Salary Range & Currency
            </label>
            <div className="grid grid-cols-3 gap-3">
              <div className="relative">
                <DollarSign className="h-4 w-4 absolute left-3 top-2.5 text-muted-foreground" />
                <input
                  type="number"
                  placeholder="Min (e.g. 120000)"
                  value={salaryMin}
                  onChange={(e) => setSalaryMin(e.target.value)}
                  className="w-full pl-9 pr-3 py-1.5 text-xs bg-background border border-border rounded-lg focus:outline-none focus:ring-2 focus:ring-primary"
                />
              </div>
              <input
                type="number"
                placeholder="Max (e.g. 150000)"
                value={salaryMax}
                onChange={(e) => setSalaryMax(e.target.value)}
                className="w-full px-3 py-1.5 text-xs bg-background border border-border rounded-lg focus:outline-none focus:ring-2 focus:ring-primary"
              />
              <select
                value={currency}
                onChange={(e) => setCurrency(e.target.value)}
                className="w-full px-3 py-1.5 text-xs bg-background border border-border rounded-lg focus:outline-none focus:ring-2 focus:ring-primary"
              >
                <option value="USD">USD ($)</option>
                <option value="EUR">EUR (€)</option>
                <option value="GBP">GBP (£)</option>
                <option value="CAD">CAD ($)</option>
                <option value="AUD">AUD ($)</option>
                <option value="CHF">CHF</option>
              </select>
            </div>
          </div>

          {/* Priority & Excitement */}
          <div className="grid grid-cols-2 gap-4 pt-1">
            <div>
              <label className="block text-xs font-semibold uppercase tracking-wider text-muted-foreground mb-1">
                Priority
              </label>
              <div className="flex gap-2">
                {[
                  { val: 1, label: 'Low', color: 'bg-muted text-muted-foreground' },
                  { val: 2, label: 'Medium', color: 'bg-blue-500/10 text-blue-600' },
                  { val: 3, label: 'High', color: 'bg-rose-500/10 text-rose-600' },
                ].map((p) => (
                  <button
                    key={p.val}
                    type="button"
                    onClick={() => setPriority(p.val)}
                    className={`flex-1 py-1 text-xs font-medium rounded-lg border transition-all ${
                      priority === p.val
                        ? `${p.color} border-current ring-1 ring-current`
                        : 'border-border text-muted-foreground hover:bg-muted'
                    }`}
                  >
                    {p.label}
                  </button>
                ))}
              </div>
            </div>

            <div>
              <label className="block text-xs font-semibold uppercase tracking-wider text-muted-foreground mb-1">
                Excitement Rating (1-5)
              </label>
              <div className="flex items-center gap-1 pt-1">
                {[1, 2, 3, 4, 5].map((star) => (
                  <button
                    key={star}
                    type="button"
                    onClick={() => setExcitementRating(star)}
                    className="p-1 hover:scale-110 transition-transform"
                  >
                    <Star
                      className={`h-5 w-5 ${
                        star <= excitementRating
                          ? 'fill-amber-400 text-amber-400'
                          : 'text-muted-foreground/30'
                      }`}
                    />
                  </button>
                ))}
              </div>
            </div>
          </div>

          {/* Pros & Cons */}
          <div className="grid grid-cols-1 sm:grid-cols-2 gap-4">
            <div>
              <div className="flex items-center gap-1.5 mb-1">
                <ThumbsUp className="h-3.5 w-3.5 text-emerald-500" />
                <label className="block text-xs font-semibold uppercase tracking-wider text-muted-foreground">
                  Pros
                </label>
              </div>
              <textarea
                rows={2}
                value={pros}
                onChange={(e) => setPros(e.target.value)}
                placeholder="High autonomy, modern stack, competitive compensation"
                className="w-full px-3 py-1.5 text-xs bg-background border border-border rounded-lg focus:outline-none focus:ring-2 focus:ring-primary resize-none"
              />
            </div>

            <div>
              <div className="flex items-center gap-1.5 mb-1">
                <ThumbsDown className="h-3.5 w-3.5 text-rose-500" />
                <label className="block text-xs font-semibold uppercase tracking-wider text-muted-foreground">
                  Cons
                </label>
              </div>
              <textarea
                rows={2}
                value={cons}
                onChange={(e) => setCons(e.target.value)}
                placeholder="Timezone overlap challenges, small team"
                className="w-full px-3 py-1.5 text-xs bg-background border border-border rounded-lg focus:outline-none focus:ring-2 focus:ring-primary resize-none"
              />
            </div>
          </div>

          {/* Notes */}
          <div>
            <label className="block text-xs font-semibold uppercase tracking-wider text-muted-foreground mb-1">
              Personal Notes
            </label>
            <textarea
              rows={2}
              value={notes}
              onChange={(e) => setNotes(e.target.value)}
              placeholder="Questions for hiring manager, recruiter contact details, interview strategy..."
              className="w-full px-3 py-1.5 text-xs bg-background border border-border rounded-lg focus:outline-none focus:ring-2 focus:ring-primary resize-none"
            />
          </div>

          {/* Job Description */}
          <div>
            <div className="flex items-center gap-1.5 mb-1">
              <FileText className="h-3.5 w-3.5 text-primary" />
              <label className="block text-xs font-semibold uppercase tracking-wider text-muted-foreground">
                Job Description
              </label>
            </div>
            <textarea
              rows={4}
              value={jobDescription}
              onChange={(e) => setJobDescription(e.target.value)}
              placeholder="Paste full job description here to enable automatic keyword and skill extraction..."
              className="w-full px-3 py-1.5 text-xs bg-background border border-border rounded-lg focus:outline-none focus:ring-2 focus:ring-primary resize-none font-mono"
            />
          </div>

          {/* Footer Actions */}
          <div className="flex items-center justify-end gap-2 pt-4 border-t border-border">
            <button
              type="button"
              onClick={onClose}
              disabled={updateMutation.isPending}
              className="px-4 py-2 text-xs font-medium text-muted-foreground hover:text-foreground hover:bg-muted rounded-lg transition-colors"
            >
              Cancel
            </button>
            <button
              type="submit"
              disabled={updateMutation.isPending}
              className="px-5 py-2 text-xs font-semibold bg-primary text-primary-foreground rounded-lg hover:opacity-90 transition-opacity shadow-sm disabled:opacity-50"
            >
              {updateMutation.isPending ? 'Saving...' : 'Save Changes'}
            </button>
          </div>
        </form>
      </div>
    </div>
  );
};
