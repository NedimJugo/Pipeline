export type ApplicationStatus =
  | 'Wishlist'
  | 'Applied'
  | 'Screening'
  | 'Interview'
  | 'Assignment'
  | 'Offer'
  | 'Accepted'
  | 'Rejected'
  | 'Withdrawn'
  | 'Ghosted'
  | 'Declined';

export type WorkMode = 'Onsite' | 'Hybrid' | 'Remote';
export type EmploymentType = 'FullTime' | 'PartTime' | 'Contract' | 'Internship' | 'Freelance';
export type ApplicationSource =
  | 'LinkedIn'
  | 'CompanyWebsite'
  | 'Referral'
  | 'Recruiter'
  | 'JobBoard'
  | 'Event'
  | 'Other';

export interface ApplicationListItem {
  id: string;
  companyId: string;
  companyName: string;
  roleTitle: string;
  jobUrl?: string | null;
  source: ApplicationSource;
  sourceDetail?: string | null;
  status: ApplicationStatus;
  statusChangedAt: string;
  appliedAt?: string | null;
  workMode: WorkMode;
  employmentType: EmploymentType;
  location?: string | null;
  salaryMin?: number | null;
  salaryMax?: number | null;
  currency: string;
  priority: number; // 1-3
  favorite: boolean;
  excitementRating: number; // 1-5
  daysInStage: number;
  nextInterviewDate?: string | null;
  closedReason?: string | null;
  rejectionStage?: string | null;
}

export interface ApplicationDetail {
  id: string;
  companyId: string;
  companyName: string;
  companyWebsite?: string | null;
  roleTitle: string;
  jobUrl?: string | null;
  source: ApplicationSource;
  sourceDetail?: string | null;
  status: ApplicationStatus;
  statusChangedAt: string;
  appliedAt?: string | null;
  workMode: WorkMode;
  employmentType: EmploymentType;
  location?: string | null;
  salaryMin?: number | null;
  salaryMax?: number | null;
  currency: string;
  jobDescription?: string | null;
  notes?: string | null;
  pros?: string | null;
  cons?: string | null;
  priority: number;
  favorite: boolean;
  excitementRating: number;
  daysInStage: number;
  documentVersionCvId?: string | null;
  documentVersionCvLabel?: string | null;
  documentVersionCoverId?: string | null;
  documentVersionCoverLabel?: string | null;
  closedReason?: string | null;
  rejectionStage?: string | null;
  lessonsLearned?: string | null;
  offerSalary?: number | null;
  offerBenefits?: string | null;
  offerDeadline?: string | null;
  createdAt: string;
  updatedAt: string;
}

export interface ApplicationTimelineItem {
  id: string;
  type: 'StatusChange' | 'Interaction' | 'Interview' | 'Task';
  title: string;
  description?: string | null;
  timestamp: string;
  metadata?: Record<string, string | null> | null;
}

export interface CreateApplicationPayload {
  roleTitle: string;
  companyId?: string | null;
  companyName?: string | null;
  jobUrl?: string | null;
  source?: ApplicationSource;
  sourceDetail?: string | null;
  status?: ApplicationStatus;
  appliedAt?: string | null;
  workMode?: WorkMode;
  employmentType?: EmploymentType;
  location?: string | null;
  salaryMin?: number | null;
  salaryMax?: number | null;
  currency?: string;
  jobDescription?: string | null;
  notes?: string | null;
  pros?: string | null;
  cons?: string | null;
  priority?: number;
  favorite?: boolean;
  excitementRating?: number;
}

export interface UpdateStatusPayload {
  status: ApplicationStatus;
  note?: string | null;
  closedReason?: string | null;
  rejectionStage?: string | null;
  lessonsLearned?: string | null;
}

export interface ApplicationFilter {
  status?: ApplicationStatus;
  search?: string;
  source?: ApplicationSource;
  workMode?: WorkMode;
  priority?: number;
  favorite?: boolean;
}

export interface Company {
  id: string;
  name: string;
  website?: string | null;
  industry?: string | null;
  size?: string | null;
  location?: string | null;
  notes?: string | null;
  linkedInUrl?: string | null;
}
