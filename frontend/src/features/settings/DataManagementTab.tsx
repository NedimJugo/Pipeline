import React, { useState, useRef } from 'react';
import {
  Download,
  Upload,
  Database,
  FileSpreadsheet,
  AlertTriangle,
  CheckCircle2,
  Trash2,
  Sparkles,
  Loader2,
  X,
  FileText,
} from 'lucide-react';
import { settingsApi } from './settings-api';
import { useImportCsv, useSeedDemo, useSeedNedim, useDeleteAccount } from './useSettings';
import { CsvImportResult } from './types';

export const DataManagementTab: React.FC = () => {
  const [isExportingCsv, setIsExportingCsv] = useState(false);
  const [isExportingGdpr, setIsExportingGdpr] = useState(false);
  const [isImportModalOpen, setIsImportModalOpen] = useState(false);
  const [isDeleteModalOpen, setIsDeleteModalOpen] = useState(false);
  const [deleteConfirmText, setDeleteConfirmText] = useState('');
  const [demoNotice, setDemoNotice] = useState<string | null>(null);
  const [nedimNotice, setNedimNotice] = useState<string | null>(null);

  // CSV Import State
  const [selectedFile, setSelectedFile] = useState<File | null>(null);
  const [importResult, setImportResult] = useState<CsvImportResult | null>(null);
  const fileInputRef = useRef<HTMLInputElement>(null);

  const importMutation = useImportCsv();
  const seedMutation = useSeedDemo();
  const seedNedimMutation = useSeedNedim();
  const deleteMutation = useDeleteAccount();

  const handleExportCsv = async () => {
    setIsExportingCsv(true);
    try {
      await settingsApi.exportApplicationsCsv();
    } catch (err) {
      console.error('Failed to export CSV:', err);
    } finally {
      setIsExportingCsv(false);
    }
  };

  const handleExportGdpr = async () => {
    setIsExportingGdpr(true);
    try {
      await settingsApi.exportGdprData();
    } catch (err) {
      console.error('Failed to export GDPR data:', err);
    } finally {
      setIsExportingGdpr(false);
    }
  };

  const handleSeedDemo = async () => {
    if (confirm('Are you sure you want to seed demo job search data? This will add ~15 applications across stages, contacts, interviews, and email templates.')) {
      setDemoNotice(null);
      const res = await seedMutation.mutateAsync();
      setDemoNotice(res.message);
      setTimeout(() => setDemoNotice(null), 5000);
    }
  };

  const handleSeedNedim = async () => {
    if (confirm('Import / Sync Nedim Jugo job search records? This will populate 24 real job applications, 27 contacts, and 62 communication interactions.')) {
      setNedimNotice(null);
      const res = await seedNedimMutation.mutateAsync();
      setNedimNotice(res.message);
      setTimeout(() => setNedimNotice(null), 6000);
    }
  };

  const handleImportSubmit = async (e: React.FormEvent) => {
    e.preventDefault();
    if (!selectedFile) return;

    try {
      const res = await importMutation.mutateAsync(selectedFile);
      setImportResult(res);
      setSelectedFile(null);
    } catch (err: any) {
      alert(err.response?.data?.detail || 'Failed to import CSV.');
    }
  };

  const handleDeleteAccount = async () => {
    if (deleteConfirmText !== 'DELETE') return;
    await deleteMutation.mutateAsync();
  };

  return (
    <div className="space-y-6">
      {/* CSV Portability Card */}
      <div className="bg-card border border-border rounded-xl p-6 shadow-xs space-y-4">
        <div className="flex items-center gap-2.5">
          <div className="p-2 rounded-lg bg-blue-500/10 text-blue-500">
            <FileSpreadsheet className="h-5 w-5" />
          </div>
          <div>
            <h2 className="text-base font-bold tracking-tight">Applications CSV Data Operations</h2>
            <p className="text-xs text-muted-foreground mt-0.5">
              Export and import your application pipeline in RFC 4180 standard spreadsheet format
            </p>
          </div>
        </div>

        <div className="grid grid-cols-1 sm:grid-cols-2 gap-4 pt-2">
          <div className="p-4 rounded-xl border border-border bg-muted/20 flex flex-col justify-between">
            <div className="space-y-1">
              <div className="text-xs font-bold text-foreground">Export CSV Spreadsheet</div>
              <p className="text-xs text-muted-foreground">
                Download all your tracked jobs with company name, title, stage, salary, and notes into Excel/Sheets.
              </p>
            </div>
            <button
              type="button"
              onClick={handleExportCsv}
              disabled={isExportingCsv}
              className="mt-4 inline-flex items-center justify-center gap-2 px-4 py-2 bg-secondary text-secondary-foreground text-xs font-bold rounded-lg hover:bg-secondary/80 transition-colors shadow-2xs disabled:opacity-50"
            >
              {isExportingCsv ? (
                <>
                  <Loader2 className="h-4 w-4 animate-spin" />
                  <span>Preparing CSV...</span>
                </>
              ) : (
                <>
                  <Download className="h-4 w-4" />
                  <span>Download Applications (.csv)</span>
                </>
              )}
            </button>
          </div>

          <div className="p-4 rounded-xl border border-border bg-muted/20 flex flex-col justify-between">
            <div className="space-y-1">
              <div className="text-xs font-bold text-foreground">Import Applications</div>
              <p className="text-xs text-muted-foreground">
                Bulk upload job records from another tracker or spreadsheet with automatic column mapping and deduplication.
              </p>
            </div>
            <button
              type="button"
              onClick={() => {
                setImportResult(null);
                setSelectedFile(null);
                setIsImportModalOpen(true);
              }}
              className="mt-4 inline-flex items-center justify-center gap-2 px-4 py-2 bg-primary text-primary-foreground text-xs font-bold rounded-lg hover:bg-primary/90 transition-colors shadow-2xs"
            >
              <Upload className="h-4 w-4" />
              <span>Import from CSV</span>
            </button>
          </div>
        </div>
      </div>

      {/* GDPR Complete Archive Card */}
      <div className="bg-card border border-border rounded-xl p-6 shadow-xs space-y-4">
        <div className="flex items-center gap-2.5">
          <div className="p-2 rounded-lg bg-indigo-500/10 text-indigo-500">
            <Database className="h-5 w-5" />
          </div>
          <div>
            <h2 className="text-base font-bold tracking-tight">Full GDPR Data Archive</h2>
            <p className="text-xs text-muted-foreground mt-0.5">
              Article 20 Right to Data Portability — Download your entire job search database in structured JSON format
            </p>
          </div>
        </div>

        <p className="text-xs text-muted-foreground">
          The archive includes your user profile, all applications and status transition logs, companies, contacts with full interaction logs, scheduled interviews and questions, tasks, document metadata, references, and email templates.
        </p>

        <button
          type="button"
          onClick={handleExportGdpr}
          disabled={isExportingGdpr}
          className="inline-flex items-center gap-2 px-4 py-2.5 bg-secondary text-secondary-foreground text-xs font-bold rounded-lg hover:bg-secondary/80 transition-colors shadow-2xs disabled:opacity-50"
        >
          {isExportingGdpr ? (
            <>
              <Loader2 className="h-4 w-4 animate-spin" />
              <span>Generating Archive...</span>
            </>
          ) : (
            <>
              <Download className="h-4 w-4" />
              <span>Download Complete Archive (.json)</span>
            </>
          )}
        </button>
      </div>

      {/* Nedim Jugo Real Career Data Card */}
      <div className="bg-card border border-primary/30 bg-primary/5 rounded-xl p-6 shadow-xs space-y-4">
        <div className="flex items-center gap-2.5">
          <div className="p-2 rounded-lg bg-primary/20 text-primary">
            <Sparkles className="h-5 w-5" />
          </div>
          <div>
            <h2 className="text-base font-bold tracking-tight text-foreground">Nedim Jugo — Real Job Search History</h2>
            <p className="text-xs text-muted-foreground mt-0.5">
              Populate workspace with 24 verified applications, 27 recruiter/referrer contacts, and 62 verbatim interaction records
            </p>
          </div>
        </div>

        {nedimNotice && (
          <div className="p-3 bg-emerald-500/10 border border-emerald-500/20 text-emerald-600 dark:text-emerald-400 rounded-lg text-xs font-semibold flex items-center gap-2">
            <CheckCircle2 className="h-4 w-4 shrink-0" />
            <span>{nedimNotice}</span>
          </div>
        )}

        <div className="flex flex-col sm:flex-row sm:items-center justify-between gap-4">
          <p className="text-xs text-muted-foreground max-w-xl">
            Loads Nedim's complete real-life application portfolio across 24 companies (Raiffeisen, UniCredit, ITO, Galeyo, HTEC, SaaS Solutions, Softray, ZIRA, Manpower, BH Telecom, Endava, Popcorn Recruiters, Port8, etc.), full verbatim email/LinkedIn/call logs, salary offer, and follow-up tasks.
          </p>
          <button
            type="button"
            onClick={handleSeedNedim}
            disabled={seedNedimMutation.isPending}
            className="inline-flex items-center justify-center gap-2 px-4 py-2 bg-primary hover:bg-primary/90 text-primary-foreground text-xs font-bold rounded-lg transition-colors shadow-2xs shrink-0 disabled:opacity-50"
          >
            {seedNedimMutation.isPending ? (
              <>
                <Loader2 className="h-4 w-4 animate-spin" />
                <span>Importing Real Career Data...</span>
              </>
            ) : (
              <>
                <Sparkles className="h-4 w-4" />
                <span>Import / Sync Nedim's Data</span>
              </>
            )}
          </button>
        </div>
      </div>

      {/* Demo Sandbox Data Card */}
      <div className="bg-card border border-border rounded-xl p-6 shadow-xs space-y-4">
        <div className="flex items-center gap-2.5">
          <div className="p-2 rounded-lg bg-amber-500/10 text-amber-500">
            <Sparkles className="h-5 w-5" />
          </div>
          <div>
            <h2 className="text-base font-bold tracking-tight">Demo Sandbox Environment</h2>
            <p className="text-xs text-muted-foreground mt-0.5">
              Populate your workspace with a realistic sample portfolio for testing and presentations
            </p>
          </div>
        </div>

        {demoNotice && (
          <div className="p-3 bg-emerald-500/10 border border-emerald-500/20 text-emerald-600 dark:text-emerald-400 rounded-lg text-xs font-semibold flex items-center gap-2">
            <CheckCircle2 className="h-4 w-4 shrink-0" />
            <span>{demoNotice}</span>
          </div>
        )}

        <div className="flex flex-col sm:flex-row sm:items-center justify-between gap-4">
          <p className="text-xs text-muted-foreground max-w-xl">
            Generates 10 realistic tech applications across all stages (`Applied`, `Screening`, `Interview`, `Offer`, `Accepted`, `Rejected`), complete with interview questions, contacts, tasks, offers, and CV versions so your analytics dashboard is populated.
          </p>
          <button
            type="button"
            onClick={handleSeedDemo}
            disabled={seedMutation.isPending}
            className="inline-flex items-center justify-center gap-2 px-4 py-2 bg-amber-600 hover:bg-amber-700 text-white text-xs font-bold rounded-lg transition-colors shadow-2xs shrink-0 disabled:opacity-50"
          >
            {seedMutation.isPending ? (
              <>
                <Loader2 className="h-4 w-4 animate-spin" />
                <span>Seeding Demo Data...</span>
              </>
            ) : (
              <>
                <Sparkles className="h-4 w-4" />
                <span>Seed Demo Data</span>
              </>
            )}
          </button>
        </div>
      </div>

      {/* Danger Zone */}
      <div className="bg-destructive/5 border border-destructive/20 rounded-xl p-6 shadow-xs space-y-4">
        <div className="flex items-center gap-2.5">
          <div className="p-2 rounded-lg bg-destructive/10 text-destructive">
            <AlertTriangle className="h-5 w-5" />
          </div>
          <div>
            <h2 className="text-base font-bold tracking-tight text-destructive">Danger Zone</h2>
            <p className="text-xs text-muted-foreground mt-0.5">
              Permanently delete your account and all associated pipeline data
            </p>
          </div>
        </div>

        <p className="text-xs text-muted-foreground">
          Deleting your account is permanent. All your applications, interviews, notes, contacts, documents, and credentials will be purged immediately in accordance with GDPR right-to-be-forgotten rules.
        </p>

        <button
          type="button"
          onClick={() => {
            setDeleteConfirmText('');
            setIsDeleteModalOpen(true);
          }}
          className="inline-flex items-center gap-2 px-4 py-2 bg-destructive text-destructive-foreground text-xs font-bold rounded-lg hover:bg-destructive/90 transition-colors shadow-2xs"
        >
          <Trash2 className="h-4 w-4" />
          <span>Delete Account</span>
        </button>
      </div>

      {/* CSV Import Modal */}
      {isImportModalOpen && (
        <div
          role="dialog"
          aria-modal="true"
          className="fixed inset-0 z-50 flex items-center justify-center p-4 bg-black/60 backdrop-blur-sm animate-in fade-in duration-150"
          onClick={() => setIsImportModalOpen(false)}
        >
          <div
            className="w-full max-w-lg bg-card border border-border rounded-xl shadow-2xl p-6 space-y-4 max-h-[90vh] overflow-y-auto"
            onClick={(e) => e.stopPropagation()}
          >
            <div className="flex items-center justify-between border-b border-border pb-3">
              <h3 className="text-base font-bold tracking-tight">Import Applications from CSV</h3>
              <button
                onClick={() => setIsImportModalOpen(false)}
                className="p-1 rounded-lg text-muted-foreground hover:text-foreground hover:bg-muted"
              >
                <X className="h-5 w-5" />
              </button>
            </div>

            <form onSubmit={handleImportSubmit} className="space-y-4">
              <div
                onClick={() => fileInputRef.current?.click()}
                className="border-2 border-dashed border-border hover:border-primary rounded-xl p-8 text-center cursor-pointer bg-muted/20 hover:bg-muted/40 transition-colors"
              >
                <input
                  ref={fileInputRef}
                  type="file"
                  accept=".csv,text/csv"
                  className="hidden"
                  onChange={(e) => {
                    if (e.target.files && e.target.files[0]) {
                      setSelectedFile(e.target.files[0]);
                      setImportResult(null);
                    }
                  }}
                />
                <Upload className="h-8 w-8 text-muted-foreground mx-auto mb-2" />
                {selectedFile ? (
                  <div className="text-xs font-bold text-primary">{selectedFile.name}</div>
                ) : (
                  <>
                    <div className="text-xs font-bold text-foreground">Click to select CSV file</div>
                    <div className="text-[11px] text-muted-foreground mt-1">Supported headers: CompanyName, RoleTitle, Status, Source, WorkMode, SalaryMin, SalaryMax, Location</div>
                  </>
                )}
              </div>

              {importResult && (
                <div className="p-4 rounded-xl border border-border bg-card space-y-2 text-xs">
                  <div className="font-bold flex items-center gap-1.5 text-foreground">
                    <CheckCircle2 className="h-4 w-4 text-emerald-500" />
                    <span>Import Completed</span>
                  </div>
                  <div className="grid grid-cols-3 gap-2 text-center pt-2">
                    <div className="p-2 rounded bg-muted/40">
                      <div className="font-bold text-foreground">{importResult.createdCount}</div>
                      <div className="text-[10px] text-muted-foreground">Created</div>
                    </div>
                    <div className="p-2 rounded bg-muted/40">
                      <div className="font-bold text-foreground">{importResult.updatedCount}</div>
                      <div className="text-[10px] text-muted-foreground">Updated</div>
                    </div>
                    <div className="p-2 rounded bg-muted/40">
                      <div className="font-bold text-destructive">{importResult.failedCount}</div>
                      <div className="text-[10px] text-muted-foreground">Failed</div>
                    </div>
                  </div>
                  {importResult.errors.length > 0 && (
                    <div className="mt-2 space-y-1 text-[11px] text-destructive bg-destructive/10 p-2 rounded">
                      {importResult.errors.map((err, i) => (
                        <div key={i}>Row {err.row}: {err.message}</div>
                      ))}
                    </div>
                  )}
                </div>
              )}

              <div className="flex justify-end gap-2 pt-2 border-t border-border">
                <button
                  type="button"
                  onClick={() => setIsImportModalOpen(false)}
                  className="px-4 py-2 border border-border text-xs font-semibold rounded-lg hover:bg-muted text-foreground"
                >
                  Close
                </button>
                <button
                  type="submit"
                  disabled={!selectedFile || importMutation.isPending}
                  className="px-4 py-2 bg-primary text-primary-foreground text-xs font-bold rounded-lg hover:bg-primary/90 disabled:opacity-50 inline-flex items-center gap-2"
                >
                  {importMutation.isPending ? (
                    <>
                      <Loader2 className="h-4 w-4 animate-spin" />
                      <span>Importing...</span>
                    </>
                  ) : (
                    <span>Start Import</span>
                  )}
                </button>
              </div>
            </form>
          </div>
        </div>
      )}

      {/* Delete Account Modal */}
      {isDeleteModalOpen && (
        <div
          role="dialog"
          aria-modal="true"
          className="fixed inset-0 z-50 flex items-center justify-center p-4 bg-black/60 backdrop-blur-sm animate-in fade-in duration-150"
          onClick={() => setIsDeleteModalOpen(false)}
        >
          <div
            className="w-full max-w-md bg-card border border-destructive/30 rounded-xl shadow-2xl p-6 space-y-4"
            onClick={(e) => e.stopPropagation()}
          >
            <div className="flex items-center gap-2.5 text-destructive">
              <AlertTriangle className="h-5 w-5" />
              <h3 className="text-base font-bold tracking-tight">Confirm Account Purge</h3>
            </div>

            <p className="text-xs text-muted-foreground">
              This action cannot be undone. To confirm permanent deletion of your account and all associated pipeline data, type <strong className="text-destructive font-mono">DELETE</strong> below:
            </p>

            <input
              type="text"
              value={deleteConfirmText}
              onChange={(e) => setDeleteConfirmText(e.target.value)}
              placeholder="Type DELETE to confirm"
              className="w-full px-3 py-2 bg-background border border-border rounded-lg text-sm text-foreground focus:outline-none focus:ring-1 focus:ring-destructive font-mono"
            />

            <div className="flex justify-end gap-2 pt-2 border-t border-border">
              <button
                type="button"
                onClick={() => setIsDeleteModalOpen(false)}
                className="px-4 py-2 border border-border text-xs font-semibold rounded-lg hover:bg-muted text-foreground"
              >
                Cancel
              </button>
              <button
                type="button"
                onClick={handleDeleteAccount}
                disabled={deleteConfirmText !== 'DELETE' || deleteMutation.isPending}
                className="px-4 py-2 bg-destructive text-destructive-foreground text-xs font-bold rounded-lg hover:bg-destructive/90 disabled:opacity-50 inline-flex items-center gap-2"
              >
                {deleteMutation.isPending ? (
                  <>
                    <Loader2 className="h-4 w-4 animate-spin" />
                    <span>Deleting...</span>
                  </>
                ) : (
                  <span>Permanently Delete</span>
                )}
              </button>
            </div>
          </div>
        </div>
      )}
    </div>
  );
};
