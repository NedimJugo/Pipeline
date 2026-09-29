import React, { useState } from 'react';
import {
  FileText,
  Download,
  Eye,
  CheckCircle2,
  Trash2,
  Plus,
  Clock,
  Layers,
  Sparkles,
  ChevronDown,
  ChevronUp,
} from 'lucide-react';
import { Document, DocumentVersion } from '../types';
import { useSetDefaultVersion, useDeleteVersion, useDeleteDocument } from '../useDocuments';
import { documentsApi } from '../documents-api';
import clsx from 'clsx';

interface DocumentCardProps {
  document: Document;
  onUploadVersion: (doc: Document) => void;
  onPreviewPdf: (versionId: string, fileName: string) => void;
}

export const DocumentCard: React.FC<DocumentCardProps> = ({
  document,
  onUploadVersion,
  onPreviewPdf,
}) => {
  const [isExpanded, setIsExpanded] = useState(true);
  const [isDeleteDocConfirmOpen, setIsDeleteDocConfirmOpen] = useState(false);

  const setDefaultMutation = useSetDefaultVersion();
  const deleteVersionMutation = useDeleteVersion();
  const deleteDocMutation = useDeleteDocument();

  const handleDownload = (version: DocumentVersion) => {
    documentsApi.downloadFile(version.id, version.fileName);
  };

  const handleSetDefault = (versionId: string) => {
    setDefaultMutation.mutate(versionId);
  };

  const handleDeleteVersion = (versionId: string) => {
    if (window.confirm('Are you sure you want to delete this version? Applications linked to it will be unlinked.')) {
      deleteVersionMutation.mutate(versionId);
    }
  };

  const handleDeleteDocument = () => {
    deleteDocMutation.mutate(document.id);
    setIsDeleteDocConfirmOpen(false);
  };

  const defaultVersion = document.versions.find((v) => v.isDefault);

  return (
    <div className="bg-card border border-border rounded-xl p-5 shadow-xs space-y-4 hover:border-border/80 transition-colors">
      {/* Header */}
      <div className="flex items-start justify-between gap-3">
        <div className="flex items-start gap-3">
          <div className="p-2.5 rounded-xl bg-primary/10 text-primary shrink-0 mt-0.5">
            <FileText className="h-5 w-5" />
          </div>

          <div>
            <div className="flex items-center gap-2 flex-wrap">
              <h3 className="font-bold text-sm text-foreground">{document.title}</h3>
              <span className="px-2 py-0.5 rounded-full text-[10px] font-semibold bg-muted text-muted-foreground border border-border">
                {document.type}
              </span>
              {defaultVersion && (
                <span className="px-2 py-0.5 rounded-full text-[10px] font-semibold bg-emerald-500/10 text-emerald-600 dark:text-emerald-400 border border-emerald-500/20 flex items-center gap-1">
                  <CheckCircle2 className="h-3 w-3" />
                  Default: {defaultVersion.versionLabel}
                </span>
              )}
            </div>

            {document.description && (
              <p className="text-xs text-muted-foreground mt-1 line-clamp-2">
                {document.description}
              </p>
            )}

            <div className="flex items-center gap-3 text-[11px] text-muted-foreground mt-2">
              <span className="flex items-center gap-1 font-mono">
                <Layers className="h-3 w-3" />
                {document.versionCount} {document.versionCount === 1 ? 'version' : 'versions'}
              </span>
              <span>•</span>
              <span className="flex items-center gap-1">
                <Clock className="h-3 w-3" />
                Updated {new Date(document.updatedAt).toLocaleDateString()}
              </span>
            </div>
          </div>
        </div>

        <div className="flex items-center gap-1 shrink-0">
          <button
            type="button"
            onClick={() => onUploadVersion(document)}
            className="inline-flex items-center gap-1 px-2.5 py-1 text-xs font-medium bg-muted hover:bg-muted/80 text-foreground rounded-lg transition-colors"
            title="Upload new version"
          >
            <Plus className="h-3.5 w-3.5" />
            <span className="hidden sm:inline">New Version</span>
          </button>

          <button
            type="button"
            onClick={() => setIsDeleteDocConfirmOpen(true)}
            className="p-1.5 text-muted-foreground hover:text-destructive hover:bg-destructive/10 rounded-lg transition-colors"
            title="Delete Document"
          >
            <Trash2 className="h-4 w-4" />
          </button>

          <button
            type="button"
            onClick={() => setIsExpanded(!isExpanded)}
            className="p-1.5 text-muted-foreground hover:text-foreground rounded-lg hover:bg-muted transition-colors"
            title={isExpanded ? 'Collapse versions' : 'Expand versions'}
          >
            {isExpanded ? <ChevronUp className="h-4 w-4" /> : <ChevronDown className="h-4 w-4" />}
          </button>
        </div>
      </div>

      {/* Version History List */}
      {isExpanded && document.versions.length > 0 && (
        <div className="space-y-2 pt-2 border-t border-border">
          <span className="block text-[10px] font-bold uppercase tracking-wider text-muted-foreground">
            Version History
          </span>

          <div className="divide-y divide-border/60 rounded-lg border border-border/80 overflow-hidden bg-muted/20">
            {document.versions.map((v) => {
              const isPdf = v.fileName.toLowerCase().endsWith('.pdf');
              return (
                <div
                  key={v.id}
                  className="p-3 flex flex-col sm:flex-row sm:items-center justify-between gap-3 hover:bg-muted/40 transition-colors"
                >
                  <div className="space-y-1 min-w-0">
                    <div className="flex items-center gap-2 flex-wrap">
                      <span className="px-2 py-0.5 rounded bg-background border border-border font-mono font-bold text-xs text-foreground">
                        {v.versionLabel}
                      </span>
                      <span className="text-xs font-semibold text-foreground truncate max-w-[200px] sm:max-w-xs">
                        {v.fileName}
                      </span>
                      <span className="text-[11px] text-muted-foreground">
                        ({(v.sizeBytes / 1024).toFixed(0)} KB)
                      </span>
                      {v.isDefault && (
                        <span className="px-1.5 py-0.5 rounded text-[10px] font-semibold bg-emerald-500/10 text-emerald-600 dark:text-emerald-400">
                          Active Default
                        </span>
                      )}
                    </div>

                    {v.notes && (
                      <p className="text-[11px] text-foreground/80 italic font-sans">{v.notes}</p>
                    )}

                    {/* Stats summary chip if stats available */}
                    {v.stats && (
                      <div className="flex items-center gap-2 text-[11px] text-muted-foreground pt-0.5">
                        <span className="font-semibold text-foreground">
                          Sent: {v.stats.sentCount}
                        </span>
                        <span>•</span>
                        <span>Interview Rate: {v.stats.interviewRate}%</span>
                        <span>•</span>
                        <span>Offer Rate: {v.stats.offerRate}%</span>
                      </div>
                    )}
                  </div>

                  {/* Actions */}
                  <div className="flex items-center gap-1.5 shrink-0 self-end sm:self-center">
                    {isPdf && (
                      <button
                        type="button"
                        onClick={() => onPreviewPdf(v.id, v.fileName)}
                        className="inline-flex items-center gap-1 px-2.5 py-1 text-xs font-medium text-foreground bg-background hover:bg-muted border border-border rounded-md shadow-2xs transition-colors"
                        title="Preview PDF in-app"
                      >
                        <Eye className="h-3.5 w-3.5 text-muted-foreground" />
                        <span>Preview</span>
                      </button>
                    )}

                    <button
                      type="button"
                      onClick={() => handleDownload(v)}
                      className="inline-flex items-center gap-1 px-2.5 py-1 text-xs font-medium text-foreground bg-background hover:bg-muted border border-border rounded-md shadow-2xs transition-colors"
                      title="Download file"
                    >
                      <Download className="h-3.5 w-3.5 text-muted-foreground" />
                      <span>Download</span>
                    </button>

                    {!v.isDefault && (
                      <button
                        type="button"
                        onClick={() => handleSetDefault(v.id)}
                        disabled={setDefaultMutation.isPending}
                        className="px-2 py-1 text-xs font-medium text-muted-foreground hover:text-foreground rounded-md hover:bg-muted transition-colors"
                        title="Set as default version"
                      >
                        Set Default
                      </button>
                    )}

                    {document.versions.length > 1 && (
                      <button
                        type="button"
                        onClick={() => handleDeleteVersion(v.id)}
                        disabled={deleteVersionMutation.isPending}
                        className="p-1 text-muted-foreground hover:text-destructive hover:bg-destructive/10 rounded-md transition-colors"
                        title="Delete this version"
                      >
                        <Trash2 className="h-3.5 w-3.5" />
                      </button>
                    )}
                  </div>
                </div>
              );
            })}
          </div>
        </div>
      )}

      {/* Delete Document Confirmation Dialog */}
      {isDeleteDocConfirmOpen && (
        <div className="fixed inset-0 z-50 flex items-center justify-center p-4 bg-background/80 backdrop-blur-sm animate-in fade-in duration-200">
          <div className="border border-border bg-card max-w-sm w-full rounded-xl shadow-2xl p-6 space-y-4">
            <h3 className="font-bold text-sm text-foreground">Delete Document</h3>
            <p className="text-xs text-muted-foreground">
              Are you sure you want to delete <strong className="text-foreground">{document.title}</strong> and all its version history?
            </p>
            <div className="flex items-center justify-end gap-2 pt-2">
              <button
                type="button"
                onClick={() => setIsDeleteDocConfirmOpen(false)}
                className="px-3.5 py-1.5 text-xs font-medium text-muted-foreground hover:text-foreground hover:bg-muted rounded-lg"
              >
                Cancel
              </button>
              <button
                type="button"
                onClick={handleDeleteDocument}
                disabled={deleteDocMutation.isPending}
                className="px-4 py-1.5 text-xs font-semibold bg-destructive text-destructive-foreground rounded-lg hover:opacity-90 disabled:opacity-50"
              >
                {deleteDocMutation.isPending ? 'Deleting...' : 'Delete'}
              </button>
            </div>
          </div>
        </div>
      )}
    </div>
  );
};
