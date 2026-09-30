export interface OfferComparisonItem {
  applicationId: string;
  roleTitle: string;
  companyId: string;
  companyName: string;
  companyWebsite?: string | null;
  status: string;
  workMode: string;
  employmentType: string;
  location?: string | null;
  offerSalary?: number | null;
  offerBonus?: number | null;
  totalCompensation: number;
  currency: string;
  offerBenefits?: string | null;
  offerDeadline?: string | null;
  daysUntilDeadline?: number | null;
  offerNegotiationNotes?: string | null;
  pros?: string | null;
  cons?: string | null;
  excitementRating: number;
  priority: number;
}

export interface OfferComparisonView {
  offers: OfferComparisonItem[];
  availableCriteria: string[];
}

export interface UpdateOfferPayload {
  offerSalary?: number | null;
  offerBonus?: number | null;
  offerBenefits?: string | null;
  offerDeadline?: string | null;
  offerNegotiationNotes?: string | null;
}

export interface CriterionWeight {
  id: string;
  name: string;
  weight: number; // Percentage e.g. 20 for 20%
}
