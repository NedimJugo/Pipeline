import React, { useEffect, useState } from 'react';
import { X, Download, ExternalLink, FileText, Loader2 } from 'lucide-react';
import { documentsApi } from '../documents-api';

interface PdfViewerModalProps {
  isOpen: boolean;
  onClose: () => void;
  versionId: string | null;
  fileName: string;
}

export const PdfViewerModal: React.FC<PdfViewerModalProps> = ({
  isOpen,
  onClose,
  versionId,
  fileName,
}) => {
  const [blobUrl, setBlobUrl] = useState<string | null>(null);
  const [isLoading, setIsLoading] = useState<boolean>(false);
  const [error, setError] = useState<string | null>(null);

  useEffect(() => {
    let active = true;
    let createdUrl: string | null = null;

    if (isOpen && versionId) {
      setIsLoading(true);
      setError(null);

      documentsApi
        .getStreamBlobUrl(versionId)
        .then((url) => {
          if (!active) {
            window.URL.revokeObjectURL(url);
            return;
          }
          createdUrl = url;
          setBlobUrl(url);
          setIsLoading(false);
        })
        .catch((err) => {
          if (!active) return;
          console.error('Failed to load PDF preview', err);
          setError('Failed to load document preview.');
          setIsLoading(false);
        });
    } else {
      setBlobUrl(null);
      setError(null);
    }

    return () => {
      active = false;
      if (createdUrl) {
        window.URL.revokeObjectURL(createdUrl);
      }
    };
  }, [isOpen, versionId]);

  useEffect(() => {
    const handleKeyDown = (e: KeyboardEvent) => {
      if (e.key === 'Escape') onClose();
    };
    if (isOpen) {
      window.addEventListener('keydown', handleKeyDown);
      return () => window.removeEventListener('keydown', handleKeyDown);
    }
  }, [isOpen, onClose]);

  if (!isOpen) return null;

  const handleDownload = () => {
    if (versionId) {
      documentsApi.downloadFile(versionId, fileName);
    }
  };

  return (
    <div className="fixed inset-0 z-50 flex items-center justify-center p-4 bg-background/80 backdrop-blur-sm animate-in fade-in duration-200">
      <div className="border border-border bg-card max-w-5xl w-full h-[90vh] rounded-2xl shadow-2xl flex flex-col overflow-hidden">
        {/* Header */}
        <div className="flex items-center justify-between px-6 py-4 border-b border-border bg-muted/30 shrink-0">
          <div className="flex items-center gap-2.5 min-w-0">
            <div className="p-2 rounded-lg bg-primary/10 text-primary">
              <FileText className="h-5 w-5" />
            </div>
            <div className="truncate">
              <h3 className="font-semibold text-sm text-foreground truncate">{fileName}</h3>
              <p className="text-[11px] text-muted-foreground">In-App Document Preview</p>
            </div>
          </div>

          <div className="flex items-center gap-2">
            {blobUrl && (
              <a
                href={blobUrl}
                target="_blank"
                rel="noreferrer"
                className="inline-flex items-center gap-1.5 px-3 py-1.5 text-xs font-medium text-foreground bg-muted hover:bg-muted/80 rounded-lg transition-colors"
                title="Open in new window"
              >
                <ExternalLink className="h-3.5 w-3.5" />
                <span className="hidden sm:inline">New Tab</span>
              </a>
            )}

            <button
              type="button"
              onClick={handleDownload}
              className="inline-flex items-center gap-1.5 px-3 py-1.5 text-xs font-medium text-primary bg-primary/10 hover:bg-primary/20 rounded-lg transition-colors"
              title="Download file"
            >
              <Download className="h-3.5 w-3.5" />
              <span>Download</span>
            </button>

            <button
              type="button"
              aria-label="Close modal"
              onClick={onClose}
              className="p-1.5 text-muted-foreground hover:text-foreground rounded-lg hover:bg-muted transition-colors ml-1"
            >
              <X className="h-5 w-5" />
            </button>
          </div>
        </div>

        {/* Content Viewer */}
        <div className="flex-1 bg-muted/10 relative overflow-hidden flex items-center justify-center">
          {isLoading && (
            <div className="flex flex-col items-center gap-3">
              <Loader2 className="h-8 w-8 text-primary animate-spin" />
              <p className="text-xs text-muted-foreground">Preparing preview...</p>
            </div>
          )}

          {error && (
            <div className="text-center p-6 space-y-3">
              <p className="text-xs text-destructive font-medium">{error}</p>
              <button
                type="button"
                onClick={handleDownload}
                className="px-4 py-2 text-xs font-semibold bg-primary text-primary-foreground rounded-lg"
              >
                Download File Directly
              </button>
            </div>
          )}

          {!isLoading && !error && blobUrl && (
            <object
              data={blobUrl}
              type="application/pdf"
              className="w-full h-full"
            >
              <div className="p-8 text-center space-y-3">
                <p className="text-xs text-muted-foreground">
                  Your browser does not support embedded PDF preview.
                </p>
                <button
                  type="button"
                  onClick={handleDownload}
                  className="px-4 py-2 text-xs font-semibold bg-primary text-primary-foreground rounded-lg"
                >
                  Download {fileName}
                </button>
              </div>
            </object>
          )}
        </div>
      </div>
    </div>
  );
};
