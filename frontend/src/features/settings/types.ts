export type SearchStatus = 'Active' | 'Passive' | 'Paused';

export interface UserSettingsProfile {
  id: string;
  email: string;
  displayName: string | null;
  targetRole: string | null;
  seniority: string | null;
  location: string | null;
  salaryExpectationMin: number | null;
  salaryExpectationMax: number | null;
  currency: string;
  searchStatus: SearchStatus;
  timezone: string;
  staleAfterDays: number;
  onboardingCompleted: boolean;
  notificationPrefs: string;
  createdAt: string;
}

export interface UpdateProfileRequest {
  displayName?: string | null;
  targetRole?: string | null;
  seniority?: string | null;
  location?: string | null;
  salaryExpectationMin?: number | null;
  salaryExpectationMax?: number | null;
  currency?: string;
  searchStatus?: SearchStatus;
  timezone?: string;
}

export interface UpdatePreferencesRequest {
  staleAfterDays?: number | null;
  notificationPrefs?: string | null;
}

export interface CsvImportError {
  row: number;
  field: string;
  message: string;
}

export interface CsvImportResult {
  totalProcessed: number;
  createdCount: number;
  updatedCount: number;
  failedCount: number;
  errors: CsvImportError[];
}

export interface IntegrationSettings {
  useCustomSmtp: boolean;
  smtpHost: string | null;
  smtpPort: number;
  smtpUser: string | null;
  hasSmtpPassword: boolean;
  smtpFrom: string | null;
  isEnvFallbackSmtp: boolean;
  useCustomGoogle: boolean;
  googleClientId: string | null;
  hasGoogleClientSecret: boolean;
  storageProvider: string;
}

export interface UpdateIntegrationSettingsRequest {
  useCustomSmtp?: boolean;
  smtpHost?: string | null;
  smtpPort?: number | null;
  smtpUser?: string | null;
  smtpPassword?: string | null;
  smtpFrom?: string | null;
  useCustomGoogle?: boolean;
  googleClientId?: string | null;
  googleClientSecret?: string | null;
  storageProvider?: string | null;
}

export interface TestEmailRequest {
  targetEmail: string;
}

export interface TestEmailResult {
  success: boolean;
  message: string;
}

