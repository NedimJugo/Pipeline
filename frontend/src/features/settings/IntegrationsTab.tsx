import React, { useState, useEffect } from 'react';
import {
  Mail,
  Server,
  Key,
  HardDrive,
  Calendar,
  CheckCircle2,
  AlertCircle,
  Eye,
  EyeOff,
  Send,
  Loader2,
  Save,
  HelpCircle,
} from 'lucide-react';
import { useIntegrations, useUpdateIntegrations, useTestEmail } from './useSettings';

export const IntegrationsTab: React.FC = () => {
  const { data: integrations, isLoading, error } = useIntegrations();
  const updateMutation = useUpdateIntegrations();
  const testEmailMutation = useTestEmail();

  // SMTP State
  const [useCustomSmtp, setUseCustomSmtp] = useState(false);
  const [smtpHost, setSmtpHost] = useState('');
  const [smtpPort, setSmtpPort] = useState(1025);
  const [smtpUser, setSmtpUser] = useState('');
  const [smtpPassword, setSmtpPassword] = useState('');
  const [smtpFrom, setSmtpFrom] = useState('');
  const [showSmtpPass, setShowSmtpPass] = useState(false);

  // Google State
  const [useCustomGoogle, setUseCustomGoogle] = useState(false);
  const [googleClientId, setGoogleClientId] = useState('');
  const [googleClientSecret, setGoogleClientSecret] = useState('');
  const [showGoogleSecret, setShowGoogleSecret] = useState(false);

  // Storage State
  const [storageProvider, setStorageProvider] = useState('Local');

  // Test Email State
  const [testEmailRecipient, setTestEmailRecipient] = useState('');
  const [testResult, setTestResult] = useState<{ success: boolean; message: string } | null>(null);
  const [saveSuccess, setSaveSuccess] = useState(false);

  useEffect(() => {
    if (integrations) {
      setUseCustomSmtp(integrations.useCustomSmtp);
      setSmtpHost(integrations.smtpHost || '');
      setSmtpPort(integrations.smtpPort || 1025);
      setSmtpUser(integrations.smtpUser || '');
      setSmtpFrom(integrations.smtpFrom || '');

      setUseCustomGoogle(integrations.useCustomGoogle);
      setGoogleClientId(integrations.googleClientId || '');

      setStorageProvider(integrations.storageProvider || 'Local');
    }
  }, [integrations]);

  const handleSave = async (e: React.FormEvent) => {
    e.preventDefault();
    setSaveSuccess(false);

    await updateMutation.mutateAsync({
      useCustomSmtp,
      smtpHost: smtpHost.trim() || null,
      smtpPort: Number(smtpPort),
      smtpUser: smtpUser.trim() || null,
      smtpPassword: smtpPassword.trim() || null,
      smtpFrom: smtpFrom.trim() || null,
      useCustomGoogle,
      googleClientId: googleClientId.trim() || null,
      googleClientSecret: googleClientSecret.trim() || null,
      storageProvider,
    });

    setSaveSuccess(true);
    setSmtpPassword('');
    setGoogleClientSecret('');
    setTimeout(() => setSaveSuccess(false), 4000);
  };

  const handleSendTestEmail = async () => {
    if (!testEmailRecipient.trim()) return;
    setTestResult(null);

    try {
      const res = await testEmailMutation.mutateAsync({
        targetEmail: testEmailRecipient.trim(),
      });
      setTestResult(res);
    } catch (err: any) {
      setTestResult({
        success: false,
        message: err?.response?.data?.message || 'Failed to dispatch test email.',
      });
    }
  };

  if (isLoading) {
    return (
      <div className="flex items-center justify-center p-12">
        <Loader2 className="h-6 w-6 animate-spin text-primary" />
      </div>
    );
  }

  if (error || !integrations) {
    return (
      <div className="p-4 rounded-xl border border-destructive/20 bg-destructive/5 text-destructive text-xs">
        Failed to load integration settings. Please refresh the page.
      </div>
    );
  }

  return (
    <form onSubmit={handleSave} className="space-y-6">
      {saveSuccess && (
        <div className="p-3 rounded-xl border border-emerald-500/20 bg-emerald-500/10 text-emerald-500 text-xs flex items-center gap-2">
          <CheckCircle2 className="h-4 w-4 shrink-0" />
          <span>Integration settings successfully updated!</span>
        </div>
      )}

      {/* 1. SMTP / Email Delivery */}
      <div className="border border-border rounded-xl bg-card p-5 sm:p-6 space-y-5 shadow-2xs">
        <div className="flex flex-col sm:flex-row sm:items-center justify-between gap-3 pb-4 border-b border-border">
          <div>
            <div className="flex items-center gap-2">
              <Mail className="h-4 w-4 text-indigo-500" />
              <h3 className="text-sm font-bold tracking-tight">Email & Notifications (SMTP)</h3>
              <span
                className={`px-2 py-0.5 rounded-full text-[10px] font-semibold border ${
                  useCustomSmtp
                    ? 'bg-emerald-500/10 text-emerald-500 border-emerald-500/20'
                    : 'bg-blue-500/10 text-blue-500 border-blue-500/20'
                }`}
              >
                {useCustomSmtp ? 'Active (Custom)' : 'Active (.env fallback)'}
              </span>
            </div>
            <p className="text-xs text-muted-foreground mt-1">
              Configure SMTP credentials for outgoing follow-up notifications and interview alerts.
            </p>
          </div>

          <label className="flex items-center gap-2 cursor-pointer select-none">
            <input
              type="checkbox"
              aria-label="Enable custom SMTP"
              checked={useCustomSmtp}
              onChange={(e) => setUseCustomSmtp(e.target.checked)}
              className="h-4 w-4 rounded border-border text-primary focus:ring-primary"
            />
            <span className="text-xs font-semibold">Enable custom SMTP</span>
          </label>
        </div>

        <div className="grid grid-cols-1 sm:grid-cols-3 gap-4">
          <div className="sm:col-span-2">
            <label className="block text-xs font-semibold uppercase tracking-wider text-muted-foreground mb-1">
              SMTP Host
            </label>
            <div className="relative">
              <Server className="h-3.5 w-3.5 absolute left-3 top-2.5 text-muted-foreground" />
              <input
                type="text"
                placeholder="e.g. smtp.gmail.com"
                value={smtpHost}
                onChange={(e) => setSmtpHost(e.target.value)}
                className="w-full pl-9 pr-3 py-1.5 text-xs bg-background border border-border rounded-lg focus:outline-none focus:ring-2 focus:ring-primary"
              />
            </div>
          </div>

          <div>
            <label className="block text-xs font-semibold uppercase tracking-wider text-muted-foreground mb-1">
              Port
            </label>
            <input
              type="number"
              placeholder="587"
              value={smtpPort}
              onChange={(e) => setSmtpPort(Number(e.target.value))}
              className="w-full px-3 py-1.5 text-xs bg-background border border-border rounded-lg focus:outline-none focus:ring-2 focus:ring-primary"
            />
          </div>

          <div>
            <label className="block text-xs font-semibold uppercase tracking-wider text-muted-foreground mb-1">
              Username / Account
            </label>
            <input
              type="text"
              placeholder="e.g. your-email@gmail.com"
              value={smtpUser}
              onChange={(e) => setSmtpUser(e.target.value)}
              className="w-full px-3 py-1.5 text-xs bg-background border border-border rounded-lg focus:outline-none focus:ring-2 focus:ring-primary"
            />
          </div>

          <div>
            <label className="block text-xs font-semibold uppercase tracking-wider text-muted-foreground mb-1">
              Password {integrations.hasSmtpPassword && '(configured)'}
            </label>
            <div className="relative">
              <input
                type={showSmtpPass ? 'text' : 'password'}
                placeholder={integrations.hasSmtpPassword ? '••••••••••••' : 'App password'}
                value={smtpPassword}
                onChange={(e) => setSmtpPassword(e.target.value)}
                className="w-full px-3 pr-8 py-1.5 text-xs bg-background border border-border rounded-lg focus:outline-none focus:ring-2 focus:ring-primary"
              />
              <button
                type="button"
                onClick={() => setShowSmtpPass(!showSmtpPass)}
                className="absolute right-2.5 top-2 text-muted-foreground hover:text-foreground"
              >
                {showSmtpPass ? <EyeOff className="h-3.5 w-3.5" /> : <Eye className="h-3.5 w-3.5" />}
              </button>
            </div>
          </div>

          <div>
            <label className="block text-xs font-semibold uppercase tracking-wider text-muted-foreground mb-1">
              Sender Email (From)
            </label>
            <input
              type="email"
              placeholder="no-reply@pipeline.local"
              value={smtpFrom}
              onChange={(e) => setSmtpFrom(e.target.value)}
              className="w-full px-3 py-1.5 text-xs bg-background border border-border rounded-lg focus:outline-none focus:ring-2 focus:ring-primary"
            />
          </div>
        </div>

        {/* Test Email Section */}
        <div className="pt-3 border-t border-border/70 flex flex-col sm:flex-row items-stretch sm:items-center justify-between gap-3 bg-muted/20 p-3.5 rounded-lg">
          <div className="flex-1 max-w-sm">
            <span className="block text-xs font-semibold text-foreground">Verify SMTP Configuration</span>
            <span className="block text-[11px] text-muted-foreground">
              Send a test notification to verify delivery credentials.
            </span>
          </div>

          <div className="flex items-center gap-2 flex-1 max-w-md">
            <input
              type="email"
              placeholder="Recipient address..."
              value={testEmailRecipient}
              onChange={(e) => setTestEmailRecipient(e.target.value)}
              className="flex-1 px-3 py-1.5 text-xs bg-background border border-border rounded-lg focus:outline-none focus:ring-2 focus:ring-primary"
            />
            <button
              type="button"
              disabled={testEmailMutation.isPending || !testEmailRecipient.trim()}
              onClick={handleSendTestEmail}
              className="px-3.5 py-1.5 rounded-lg bg-secondary text-secondary-foreground text-xs font-semibold hover:bg-secondary/80 disabled:opacity-50 transition-colors flex items-center gap-1.5 shrink-0"
            >
              {testEmailMutation.isPending ? (
                <Loader2 className="h-3.5 w-3.5 animate-spin" />
              ) : (
                <Send className="h-3.5 w-3.5" />
              )}
              <span>Send Test</span>
            </button>
          </div>
        </div>

        {testResult && (
          <div
            className={`p-3 rounded-lg text-xs flex items-center gap-2 border ${
              testResult.success
                ? 'bg-emerald-500/10 text-emerald-500 border-emerald-500/20'
                : 'bg-destructive/10 text-destructive border-destructive/20'
            }`}
          >
            {testResult.success ? (
              <CheckCircle2 className="h-4 w-4 shrink-0" />
            ) : (
              <AlertCircle className="h-4 w-4 shrink-0" />
            )}
            <span>{testResult.message}</span>
          </div>
        )}
      </div>

      {/* 2. Google Calendar & OAuth */}
      <div className="border border-border rounded-xl bg-card p-5 sm:p-6 space-y-4 shadow-2xs">
        <div className="flex flex-col sm:flex-row sm:items-center justify-between gap-3 pb-3 border-b border-border">
          <div>
            <div className="flex items-center gap-2">
              <Calendar className="h-4 w-4 text-emerald-500" />
              <h3 className="text-sm font-bold tracking-tight">Google Calendar & OAuth</h3>
              <span
                className={`px-2 py-0.5 rounded-full text-[10px] font-semibold border ${
                  useCustomGoogle
                    ? 'bg-emerald-500/10 text-emerald-500 border-emerald-500/20'
                    : 'bg-muted text-muted-foreground border-border'
                }`}
              >
                {useCustomGoogle ? 'Custom OAuth' : 'Standard'}
              </span>
            </div>
            <p className="text-xs text-muted-foreground mt-1">
              Sync scheduled interviews and deadlines directly with your Google Workspace calendar.
            </p>
          </div>

          <label className="flex items-center gap-2 cursor-pointer select-none">
            <input
              type="checkbox"
              aria-label="Enable custom Google OAuth"
              checked={useCustomGoogle}
              onChange={(e) => setUseCustomGoogle(e.target.checked)}
              className="h-4 w-4 rounded border-border text-primary focus:ring-primary"
            />
            <span className="text-xs font-semibold">Enable custom Google OAuth</span>
          </label>
        </div>

        <div className="grid grid-cols-1 sm:grid-cols-2 gap-4">
          <div>
            <label className="block text-xs font-semibold uppercase tracking-wider text-muted-foreground mb-1">
              Google Client ID
            </label>
            <input
              type="text"
              placeholder="e.g. 123456789-abc.apps.googleusercontent.com"
              value={googleClientId}
              onChange={(e) => setGoogleClientId(e.target.value)}
              className="w-full px-3 py-1.5 text-xs bg-background border border-border rounded-lg focus:outline-none focus:ring-2 focus:ring-primary"
            />
          </div>

          <div>
            <label className="block text-xs font-semibold uppercase tracking-wider text-muted-foreground mb-1">
              Google Client Secret {integrations.hasGoogleClientSecret && '(configured)'}
            </label>
            <div className="relative">
              <input
                type={showGoogleSecret ? 'text' : 'password'}
                placeholder={integrations.hasGoogleClientSecret ? '••••••••••••' : 'GOCSPX-secret'}
                value={googleClientSecret}
                onChange={(e) => setGoogleClientSecret(e.target.value)}
                className="w-full px-3 pr-8 py-1.5 text-xs bg-background border border-border rounded-lg focus:outline-none focus:ring-2 focus:ring-primary"
              />
              <button
                type="button"
                onClick={() => setShowGoogleSecret(!showGoogleSecret)}
                className="absolute right-2.5 top-2 text-muted-foreground hover:text-foreground"
              >
                {showGoogleSecret ? <EyeOff className="h-3.5 w-3.5" /> : <Eye className="h-3.5 w-3.5" />}
              </button>
            </div>
          </div>
        </div>

        <div className="flex items-start gap-2 p-3 bg-muted/20 border border-border rounded-lg text-[11px] text-muted-foreground">
          <HelpCircle className="h-4 w-4 text-primary shrink-0 mt-0.5" />
          <p>
            Create an OAuth 2.0 Web Application client in Google Cloud Console. Set Authorized redirect URIs to{' '}
            <code className="px-1 py-0.5 rounded bg-muted font-mono text-[10px]">
              {window.location.origin}/api/calendar/google/callback
            </code>
            .
          </p>
        </div>
      </div>

      {/* 3. Document Storage Preference */}
      <div className="border border-border rounded-xl bg-card p-5 sm:p-6 space-y-4 shadow-2xs">
        <div>
          <div className="flex items-center gap-2">
            <HardDrive className="h-4 w-4 text-sky-500" />
            <h3 className="text-sm font-bold tracking-tight">Document Storage Provider</h3>
          </div>
          <p className="text-xs text-muted-foreground mt-1">
            Choose where your CV resumes, cover letters, and interview debrief notes are securely stored.
          </p>
        </div>

        <div className="grid grid-cols-1 sm:grid-cols-2 gap-3">
          <label
            className={`p-4 rounded-xl border flex items-center justify-between cursor-pointer transition-all ${
              storageProvider === 'Local'
                ? 'border-primary bg-primary/5 shadow-xs'
                : 'border-border bg-card/60 hover:bg-muted/40'
            }`}
          >
            <div>
              <span className="block text-xs font-bold">Local File Storage</span>
              <span className="block text-[11px] text-muted-foreground mt-0.5">
                Saved directly to the server filesystem (zero cloud setup required).
              </span>
            </div>
            <input
              type="radio"
              name="storageProvider"
              value="Local"
              checked={storageProvider === 'Local'}
              onChange={() => setStorageProvider('Local')}
              className="text-primary focus:ring-primary"
            />
          </label>

          <label
            className={`p-4 rounded-xl border flex items-center justify-between cursor-pointer transition-all ${
              storageProvider === 'S3'
                ? 'border-primary bg-primary/5 shadow-xs'
                : 'border-border bg-card/60 hover:bg-muted/40'
            }`}
          >
            <div>
              <span className="block text-xs font-bold">Cloud S3 / MinIO Storage</span>
              <span className="block text-[11px] text-muted-foreground mt-0.5">
                Encrypted object storage via MinIO or AWS S3 compatible buckets.
              </span>
            </div>
            <input
              type="radio"
              name="storageProvider"
              value="S3"
              checked={storageProvider === 'S3'}
              onChange={() => setStorageProvider('S3')}
              className="text-primary focus:ring-primary"
            />
          </label>
        </div>
      </div>

      {/* Save Button */}
      <div className="flex items-center justify-end gap-3 pt-2">
        <button
          type="submit"
          disabled={updateMutation.isPending}
          className="px-6 py-2.5 rounded-xl bg-primary text-primary-foreground text-xs font-bold hover:bg-primary/90 disabled:opacity-50 transition-colors shadow-sm flex items-center gap-2"
        >
          {updateMutation.isPending ? (
            <Loader2 className="h-4 w-4 animate-spin" />
          ) : (
            <Save className="h-4 w-4" />
          )}
          <span>Save Integrations</span>
        </button>
      </div>
    </form>
  );
};
