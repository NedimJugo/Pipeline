import React, { useState } from 'react';
import { CreateContactPayload, ContactType } from '../types';
import { useCreateContact } from '../useContacts';
import { useApplications } from '@/features/applications/useApplications';
import { X, User, Building2, Briefcase, Mail, Phone, Linkedin, FileText } from 'lucide-react';

interface CreateContactModalProps {
  isOpen: boolean;
  onClose: () => void;
  preselectedApplicationId?: string;
  preselectedCompanyName?: string;
}

export const CreateContactModal: React.FC<CreateContactModalProps> = ({
  isOpen,
  onClose,
  preselectedApplicationId,
  preselectedCompanyName,
}) => {
  const [fullName, setFullName] = useState('');
  const [companyName, setCompanyName] = useState(preselectedCompanyName || '');
  const [role, setRole] = useState('');
  const [type, setType] = useState<ContactType>('Recruiter');
  const [email, setEmail] = useState('');
  const [phone, setPhone] = useState('');
  const [linkedInUrl, setLinkedInUrl] = useState('');
  const [applicationId, setApplicationId] = useState(preselectedApplicationId || '');
  const [roleInProcess, setRoleInProcess] = useState('');
  const [notes, setNotes] = useState('');

  const createMutation = useCreateContact();
  const { data: applications } = useApplications();

  if (!isOpen) return null;

  const handleSubmit = async (e: React.FormEvent) => {
    e.preventDefault();
    if (!fullName.trim()) return;

    const payload: CreateContactPayload = {
      fullName: fullName.trim(),
      companyName: companyName.trim() || undefined,
      role: role.trim() || undefined,
      type,
      email: email.trim() || undefined,
      phone: phone.trim() || undefined,
      linkedInUrl: linkedInUrl.trim() || undefined,
      applicationId: applicationId || undefined,
      roleInProcess: roleInProcess.trim() || undefined,
      notes: notes.trim() || undefined,
    };

    await createMutation.mutateAsync(payload);
    onClose();
  };

  return (
    <div className="fixed inset-0 z-50 flex items-center justify-center p-4 bg-background/80 backdrop-blur-sm animate-in fade-in duration-200">
      <div className="border border-border bg-card max-w-lg w-full rounded-xl shadow-2xl overflow-hidden flex flex-col max-h-[90vh]">
        {/* Header */}
        <div className="px-6 py-4 border-b border-border flex items-center justify-between">
          <div className="flex items-center gap-2">
            <User className="h-5 w-5 text-primary" />
            <h2 className="font-bold text-base tracking-tight text-foreground">Add New Contact</h2>
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
          <div>
            <label className="block text-xs font-semibold uppercase tracking-wider text-muted-foreground mb-1">
              Full Name *
            </label>
            <input
              type="text"
              required
              placeholder="e.g. Sarah Connor"
              value={fullName}
              onChange={(e) => setFullName(e.target.value)}
              className="w-full px-3 py-1.5 text-xs bg-background border border-border rounded-lg focus:outline-none focus:ring-2 focus:ring-primary"
            />
          </div>

          <div className="grid grid-cols-1 sm:grid-cols-2 gap-4">
            <div>
              <label className="block text-xs font-semibold uppercase tracking-wider text-muted-foreground mb-1">
                Company
              </label>
              <div className="relative">
                <Building2 className="h-4 w-4 absolute left-3 top-2.5 text-muted-foreground" />
                <input
                  type="text"
                  placeholder="e.g. Stripe"
                  value={companyName}
                  onChange={(e) => setCompanyName(e.target.value)}
                  className="w-full pl-9 pr-3 py-1.5 text-xs bg-background border border-border rounded-lg focus:outline-none focus:ring-2 focus:ring-primary"
                />
              </div>
            </div>

            <div>
              <label className="block text-xs font-semibold uppercase tracking-wider text-muted-foreground mb-1">
                Role / Title
              </label>
              <input
                type="text"
                placeholder="e.g. Senior Tech Recruiter"
                value={role}
                onChange={(e) => setRole(e.target.value)}
                className="w-full px-3 py-1.5 text-xs bg-background border border-border rounded-lg focus:outline-none focus:ring-2 focus:ring-primary"
              />
            </div>
          </div>

          <div>
            <label className="block text-xs font-semibold uppercase tracking-wider text-muted-foreground mb-1">
              Contact Type
            </label>
            <select
              value={type}
              onChange={(e) => setType(e.target.value as ContactType)}
              className="w-full px-3 py-1.5 text-xs bg-background border border-border rounded-lg focus:outline-none focus:ring-2 focus:ring-primary"
            >
              <option value="Recruiter">Recruiter</option>
              <option value="HiringManager">Hiring Manager</option>
              <option value="Interviewer">Interviewer</option>
              <option value="Referrer">Referrer</option>
              <option value="Peer">Peer / Colleague</option>
              <option value="Other">Other</option>
            </select>
          </div>

          <div className="grid grid-cols-1 sm:grid-cols-2 gap-4">
            <div>
              <label className="block text-xs font-semibold uppercase tracking-wider text-muted-foreground mb-1">
                Email
              </label>
              <div className="relative">
                <Mail className="h-4 w-4 absolute left-3 top-2.5 text-muted-foreground" />
                <input
                  type="email"
                  placeholder="sarah@example.com"
                  value={email}
                  onChange={(e) => setEmail(e.target.value)}
                  className="w-full pl-9 pr-3 py-1.5 text-xs bg-background border border-border rounded-lg focus:outline-none focus:ring-2 focus:ring-primary"
                />
              </div>
            </div>

            <div>
              <label className="block text-xs font-semibold uppercase tracking-wider text-muted-foreground mb-1">
                Phone
              </label>
              <div className="relative">
                <Phone className="h-4 w-4 absolute left-3 top-2.5 text-muted-foreground" />
                <input
                  type="tel"
                  placeholder="+1 (555) 000-0000"
                  value={phone}
                  onChange={(e) => setPhone(e.target.value)}
                  className="w-full pl-9 pr-3 py-1.5 text-xs bg-background border border-border rounded-lg focus:outline-none focus:ring-2 focus:ring-primary"
                />
              </div>
            </div>
          </div>

          <div>
            <label className="block text-xs font-semibold uppercase tracking-wider text-muted-foreground mb-1">
              LinkedIn Profile URL
            </label>
            <div className="relative">
              <Linkedin className="h-4 w-4 absolute left-3 top-2.5 text-muted-foreground" />
              <input
                type="url"
                placeholder="https://linkedin.com/in/username"
                value={linkedInUrl}
                onChange={(e) => setLinkedInUrl(e.target.value)}
                className="w-full pl-9 pr-3 py-1.5 text-xs bg-background border border-border rounded-lg focus:outline-none focus:ring-2 focus:ring-primary"
              />
            </div>
          </div>

          {/* Optional Application Association */}
          <div className="pt-2 border-t border-border space-y-3">
            <span className="text-[11px] font-bold uppercase tracking-wider text-muted-foreground block">
              Application Association (Optional)
            </span>

            <div className="grid grid-cols-1 sm:grid-cols-2 gap-4">
              <div>
                <label className="block text-xs font-semibold uppercase tracking-wider text-muted-foreground mb-1">
                  Link to Application
                </label>
                <div className="relative">
                  <Briefcase className="h-4 w-4 absolute left-3 top-2.5 text-muted-foreground" />
                  <select
                    value={applicationId}
                    onChange={(e) => setApplicationId(e.target.value)}
                    className="w-full pl-9 pr-3 py-1.5 text-xs bg-background border border-border rounded-lg focus:outline-none focus:ring-2 focus:ring-primary"
                  >
                    <option value="">None / General Contact</option>
                    {applications?.map((app) => (
                      <option key={app.id} value={app.id}>
                        {app.companyName} — {app.roleTitle}
                      </option>
                    ))}
                  </select>
                </div>
              </div>

              <div>
                <label className="block text-xs font-semibold uppercase tracking-wider text-muted-foreground mb-1">
                  Role in Process
                </label>
                <input
                  type="text"
                  placeholder="e.g. Primary Recruiter, Tech Screener"
                  value={roleInProcess}
                  onChange={(e) => setRoleInProcess(e.target.value)}
                  className="w-full px-3 py-1.5 text-xs bg-background border border-border rounded-lg focus:outline-none focus:ring-2 focus:ring-primary"
                />
              </div>
            </div>
          </div>

          <div>
            <label className="block text-xs font-semibold uppercase tracking-wider text-muted-foreground mb-1">
              Notes
            </label>
            <textarea
              rows={2}
              placeholder="Background notes, referral relationship, preferred communication style..."
              value={notes}
              onChange={(e) => setNotes(e.target.value)}
              className="w-full px-3 py-1.5 text-xs bg-background border border-border rounded-lg focus:outline-none focus:ring-2 focus:ring-primary resize-none"
            />
          </div>

          {/* Footer Actions */}
          <div className="flex items-center justify-end gap-2 pt-3 border-t border-border">
            <button
              type="button"
              onClick={onClose}
              disabled={createMutation.isPending}
              className="px-3.5 py-1.5 text-xs font-medium text-muted-foreground hover:text-foreground hover:bg-muted rounded-lg transition-colors"
            >
              Cancel
            </button>
            <button
              type="submit"
              disabled={createMutation.isPending}
              className="px-4 py-1.5 text-xs font-semibold bg-primary text-primary-foreground rounded-lg hover:opacity-90 shadow-sm transition-opacity disabled:opacity-50"
            >
              {createMutation.isPending ? 'Adding...' : 'Add Contact'}
            </button>
          </div>
        </form>
      </div>
    </div>
  );
};
