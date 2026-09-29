import React, { useState } from 'react';
import {
  FileText,
  Download,
  Eye,
  CheckCircle2,
  UploadCloud,
  Layers,
  Sparkles,
  Link as LinkIcon,
} from 'lucide-react';
import { ApplicationDetail } from '../types';
import { useDocuments } from '@/features/documents/useDocuments';
import { documentsApi } from '@/features/documents/documents-api';
import { useUpdateApplication } from '../useApplications';
import { PdfViewerModal } from '@/features/documents/components/PdfViewerModal';
import { UploadDocumentModal } from '@/features/documents/components/UploadDocumentModal';

interface ApplicationDocumentsTabProps {
  application: ApplicationDetail;
}

export const ApplicationDocumentsTab: React.FC<ApplicationDocumentsTabProps> = ({
  application,
}) => {
  const { data: allDocs = [], isLoading } = useDocuments();
  const updateAppMutation = useUpdateApplication();

  const [selectedCvVersionId, setSelectedCvVersionId] = useState<string>(
    application.documentVersionCvId || ''
  );
  const [selectedCoverVersionId, setSelectedCoverVersionId] = useState<string>(
    application.documentVersionCoverId || ''
  );
  const [previewPdfInfo, setPreviewPdfInfo] = useState<{
    versionId: string;
    fileName: string;
  } | null>(null);
  const [isUploadAttachmentOpen, setIsUploadAttachmentOpen] = useState(false);
  const [saveSuccessMessage, setSaveSuccessMessage] = useState<string | null>(null);

  // Group documents
  const cvDocs = allDocs.filter((d) => d.type === 'CV');
  const coverDocs = allDocs.filter((d) => d.type === 'CoverLetter');
  const attachmentDocs = allDocs.filter(
    (d) => d.type === 'Other' || d.type === 'Portfolio' || d.type === 'Certificate'
  );

  // Flatten versions for select options
  const allCvVersions = cvDocs.flatMap((d) =>
    d.versions.map((v) => ({
      versionId: v.id,
      label: `${d.title} (${v.versionLabel} - ${v.fileName})`,
      docTitle: d.title,
      version: v,
    }))
  );

  const allCoverVersions = coverDocs.flatMap((d) =>
    d.versions.map((v) => ({
      versionId: v.id,
      label: `${d.title} (${v.versionLabel} - ${v.fileName})`,
      docTitle: d.title,
      version: v,
    }))
  );

  const linkedCvInfo = allCvVersions.find((v) => v.versionId === application.documentVersionCvId);
  const linkedCoverInfo = allCoverVersions.find(
    (v) => v.versionId === application.documentVersionCoverId
  );

  const handleSaveDocumentLinks = async () => {
    try {
      await updateAppMutation.mutateAsync({
        id: application.id,
        payload: {
          roleTitle: application.roleTitle,
          companyId: application.companyId,
          documentVersionCvId: selectedCvVersionId || null,
          documentVersionCoverId: selectedCoverVersionId || null,
        },
      });
      setSaveSuccessMessage('Document links updated successfully!');
      setTimeout(() => setSaveSuccessMessage(null), 3000);
    } catch (err) {
      console.error('Failed to link documents', err);
    }
  };

  const handleDownload = (versionId: string, fileName: string) => {
    documentsApi.downloadFile(versionId, fileName);
  };

  return (
    <div className="space-y-6 max-w-4xl">
      {saveSuccessMessage && (
        <div className="p-3 bg-emerald-500/10 border border-emerald-500/20 text-emerald-600 dark:text-emerald-400 rounded-xl text-xs font-semibold flex items-center gap-2 animate-in fade-in">
          <CheckCircle2 className="h-4 w-4 shrink-0" />
          <span>{saveSuccessMessage}</span>
        </div>
      )}

      {/* Primary Resumes & Cover Letters Linked */}
      <div className="grid grid-cols-1 md:grid-cols-2 gap-6">
        {/* CV / Resume Section */}
        <div className="bg-card border border-border rounded-xl p-5 shadow-2xs space-y-4 flex flex-col justify-between">
          <div className="space-y-3">
            <div className="flex items-center justify-between">
              <div className="flex items-center gap-2">
                <div className="p-2 rounded-lg bg-primary/10 text-primary">
                  <FileText className="h-4 w-4" />
                </div>
                <div>
                  <h3 className="font-bold text-sm text-foreground">CV / Resume Sent</h3>
                  <p className="text-[11px] text-muted-foreground">Version sent with this application</p>
                </div>
              </div>

              {linkedCvInfo && (
                <span className="px-2 py-0.5 rounded-full text-[10px] font-semibold bg-primary/10 text-primary">
                  Linked: {linkedCvInfo.version.versionLabel}
                </span>
              )}
            </div>

            {linkedCvInfo ? (
              <div className="p-3 rounded-lg bg-muted/40 border border-border space-y-2">
                <div className="flex items-center justify-between gap-2">
                  <div className="min-w-0">
                    <p className="text-xs font-bold text-foreground truncate">
                      {linkedCvInfo.docTitle}
                    </p>
                    <span className="text-[11px] text-muted-foreground block truncate">
                      {linkedCvInfo.version.fileName} ({(linkedCvInfo.version.sizeBytes / 1024).toFixed(0)} KB)
                    </span>
                  </div>

                  <div className="flex items-center gap-1 shrink-0">
                    {linkedCvInfo.version.fileName.toLowerCase().endsWith('.pdf') && (
                      <button
                        type="button"
                        onClick={() =>
                          setPreviewPdfInfo({
                            versionId: linkedCvInfo.version.id,
                            fileName: linkedCvInfo.version.fileName,
                          })
                        }
                        className="p-1.5 text-muted-foreground hover:text-foreground hover:bg-muted rounded-md transition-colors"
                        title="Preview PDF"
                      >
                        <Eye className="h-4 w-4" />
                      </button>
                    )}
                    <button
                      type="button"
                      onClick={() =>
                        handleDownload(linkedCvInfo.version.id, linkedCvInfo.version.fileName)
                      }
                      className="p-1.5 text-muted-foreground hover:text-foreground hover:bg-muted rounded-md transition-colors"
                      title="Download file"
                    >
                      <Download className="h-4 w-4" />
                    </button>
                  </div>
                </div>
              </div>
            ) : (
              <p className="text-xs text-muted-foreground italic bg-muted/20 p-3 rounded-lg border border-dashed border-border">
                No CV version linked to this application yet.
              </p>
            )}
          </div>

          <div className="pt-2 border-t border-border space-y-2">
            <label className="block text-[11px] font-semibold text-muted-foreground uppercase tracking-wider">
              Select or Change CV Version
            </label>
            <div className="flex items-center gap-2">
              <select
                value={selectedCvVersionId}
                onChange={(e) => setSelectedCvVersionId(e.target.value)}
                className="w-full px-2.5 py-1.5 text-xs bg-background border border-border rounded-lg text-foreground focus:outline-none focus:ring-1 focus:ring-primary"
              >
                <option value="">(None / Unlinked)</option>
                {allCvVersions.map((v) => (
                  <option key={v.versionId} value={v.versionId}>
                    {v.label}
                  </option>
                ))}
              </select>
            </div>
          </div>
        </div>

        {/* Cover Letter Section */}
        <div className="bg-card border border-border rounded-xl p-5 shadow-2xs space-y-4 flex flex-col justify-between">
          <div className="space-y-3">
            <div className="flex items-center justify-between">
              <div className="flex items-center gap-2">
                <div className="p-2 rounded-lg bg-indigo-500/10 text-indigo-500">
                  <FileText className="h-4 w-4" />
                </div>
                <div>
                  <h3 className="font-bold text-sm text-foreground">Cover Letter Sent</h3>
                  <p className="text-[11px] text-muted-foreground">Tailored cover letter version</p>
                </div>
              </div>

              {linkedCoverInfo && (
                <span className="px-2 py-0.5 rounded-full text-[10px] font-semibold bg-indigo-500/10 text-indigo-500">
                  Linked: {linkedCoverInfo.version.versionLabel}
                </span>
              )}
            </div>

            {linkedCoverInfo ? (
              <div className="p-3 rounded-lg bg-muted/40 border border-border space-y-2">
                <div className="flex items-center justify-between gap-2">
                  <div className="min-w-0">
                    <p className="text-xs font-bold text-foreground truncate">
                      {linkedCoverInfo.docTitle}
                    </p>
                    <span className="text-[11px] text-muted-foreground block truncate">
                      {linkedCoverInfo.version.fileName} ({(linkedCoverInfo.version.sizeBytes / 1024).toFixed(0)} KB)
                    </span>
                  </div>

                  <div className="flex items-center gap-1 shrink-0">
                    {linkedCoverInfo.version.fileName.toLowerCase().endsWith('.pdf') && (
                      <button
                        type="button"
                        onClick={() =>
                          setPreviewPdfInfo({
                            versionId: linkedCoverInfo.version.id,
                            fileName: linkedCoverInfo.version.fileName,
                          })
                        }
                        className="p-1.5 text-muted-foreground hover:text-foreground hover:bg-muted rounded-md transition-colors"
                        title="Preview PDF"
                      >
                        <Eye className="h-4 w-4" />
                      </button>
                    )}
                    <button
                      type="button"
                      onClick={() =>
                        handleDownload(linkedCoverInfo.version.id, linkedCoverInfo.version.fileName)
                      }
                      className="p-1.5 text-muted-foreground hover:text-foreground hover:bg-muted rounded-md transition-colors"
                      title="Download file"
                    >
                      <Download className="h-4 w-4" />
                    </button>
                  </div>
                </div>
              </div>
            ) : (
              <p className="text-xs text-muted-foreground italic bg-muted/20 p-3 rounded-lg border border-dashed border-border">
                No cover letter version linked to this application.
              </p>
            )}
          </div>

          <div className="pt-2 border-t border-border space-y-2">
            <label className="block text-[11px] font-semibold text-muted-foreground uppercase tracking-wider">
              Select or Change Cover Letter
            </label>
            <div className="flex items-center gap-2">
              <select
                value={selectedCoverVersionId}
                onChange={(e) => setSelectedCoverVersionId(e.target.value)}
                className="w-full px-2.5 py-1.5 text-xs bg-background border border-border rounded-lg text-foreground focus:outline-none focus:ring-1 focus:ring-primary"
              >
                <option value="">(None / Unlinked)</option>
                {allCoverVersions.map((v) => (
                  <option key={v.versionId} value={v.versionId}>
                    {v.label}
                  </option>
                ))}
              </select>
            </div>
          </div>
        </div>
      </div>

      {/* Save Button for Document Version Links */}
      <div className="flex items-center justify-end">
        <button
          type="button"
          onClick={handleSaveDocumentLinks}
          disabled={updateAppMutation.isPending}
          className="inline-flex items-center gap-1.5 px-4 py-2 text-xs font-semibold bg-primary text-primary-foreground rounded-lg shadow-sm hover:opacity-90 disabled:opacity-50 transition-opacity"
        >
          <LinkIcon className="h-3.5 w-3.5" />
          <span>
            {updateAppMutation.isPending ? 'Saving Links...' : 'Save Linked Document Versions'}
          </span>
        </button>
      </div>

      {/* Supporting Attachments Section */}
      <div className="bg-card border border-border rounded-xl p-5 shadow-2xs space-y-4">
        <div className="flex items-center justify-between">
          <div className="flex items-center gap-2">
            <div className="p-2 rounded-lg bg-muted text-foreground">
              <Layers className="h-4 w-4" />
            </div>
            <div>
              <h3 className="font-bold text-sm text-foreground">
                Career Attachments & Offer Letters
              </h3>
              <p className="text-[11px] text-muted-foreground">
                Portfolios, take-home task briefs, certificates, or extended offer letters
              </p>
            </div>
          </div>

          <button
            type="button"
            onClick={() => setIsUploadAttachmentOpen(true)}
            className="inline-flex items-center gap-1.5 px-3 py-1.5 text-xs font-semibold bg-muted hover:bg-muted/80 text-foreground rounded-lg transition-colors"
          >
            <UploadCloud className="h-3.5 w-3.5" />
            <span>Upload New Attachment</span>
          </button>
        </div>

        {attachmentDocs.length === 0 ? (
          <p className="text-xs text-muted-foreground italic py-4 text-center">
            No supporting attachments uploaded yet. You can upload assignments, portfolios, or certificates.
          </p>
        ) : (
          <div className="grid grid-cols-1 sm:grid-cols-2 gap-3 pt-2">
            {attachmentDocs.map((doc) => {
              const latestVer = doc.versions[0];
              if (!latestVer) return null;
              return (
                <div
                  key={doc.id}
                  className="p-3 rounded-lg border border-border bg-muted/20 flex items-center justify-between gap-3"
                >
                  <div className="min-w-0">
                    <span className="text-[10px] font-semibold uppercase text-muted-foreground block">
                      {doc.type}
                    </span>
                    <p className="text-xs font-bold text-foreground truncate">{doc.title}</p>
                    <span className="text-[11px] text-muted-foreground truncate block">
                      {latestVer.fileName} ({(latestVer.sizeBytes / 1024).toFixed(0)} KB)
                    </span>
                  </div>

                  <div className="flex items-center gap-1 shrink-0">
                    {latestVer.fileName.toLowerCase().endsWith('.pdf') && (
                      <button
                        type="button"
                        onClick={() =>
                          setPreviewPdfInfo({
                            versionId: latestVer.id,
                            fileName: latestVer.fileName,
                          })
                        }
                        className="p-1.5 text-muted-foreground hover:text-foreground hover:bg-muted rounded-md transition-colors"
                        title="Preview PDF"
                      >
                        <Eye className="h-4 w-4" />
                      </button>
                    )}
                    <button
                      type="button"
                      onClick={() => handleDownload(latestVer.id, latestVer.fileName)}
                      className="p-1.5 text-muted-foreground hover:text-foreground hover:bg-muted rounded-md transition-colors"
                      title="Download file"
                    >
                      <Download className="h-4 w-4" />
                    </button>
                  </div>
                </div>
              );
            })}
          </div>
        )}
      </div>

      {/* PDF Preview Modal */}
      {previewPdfInfo && (
        <PdfViewerModal
          isOpen={!!previewPdfInfo}
          onClose={() => setPreviewPdfInfo(null)}
          versionId={previewPdfInfo.versionId}
          fileName={previewPdfInfo.fileName}
        />
      )}

      {/* Upload Attachment Modal */}
      {isUploadAttachmentOpen && (
        <UploadDocumentModal
          isOpen={isUploadAttachmentOpen}
          onClose={() => setIsUploadAttachmentOpen(false)}
          defaultType="Other"
        />
      )}
    </div>
  );
};
