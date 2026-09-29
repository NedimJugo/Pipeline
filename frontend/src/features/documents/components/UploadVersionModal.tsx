import React, { useState } from 'react';
import { X, UploadCloud, FileText, AlertCircle } from 'lucide-react';
import { useUploadVersion } from '../useDocuments';

interface UploadVersionModalProps {
  isOpen: boolean;
  onClose: () => void;
  documentId: string;
  documentTitle: string;
  suggestedVersionLabel?: string;
}

export const UploadVersionModal: React.FC<UploadVersionModalProps> = ({
  isOpen,
  onClose,
  documentId,
  documentTitle,
  suggestedVersionLabel = 'v2',
}) => {
  const [versionLabel, setVersionLabel] = useState(suggestedVersionLabel);
  const [notes, setNotes] = useState('');
  const [isDefault, setIsDefault] = useState(false);
  const [file, setFile] = useState<File | null>(null);
  const [error, setError] = useState<string | null>(null);

  const uploadVersionMutation = useUploadVersion();

  if (!isOpen) return null;

  const handleFileChange = (e: React.ChangeEvent<HTMLInputElement>) => {
    if (e.target.files && e.target.files[0]) {
      const selected = e.target.files[0];
      if (selected.size > 10 * 1024 * 1024) {
        setError('File size exceeds the 10 MB limit.');
        return;
      }
      setFile(selected);
      setError(null);
    }
  };

  const handleSubmit = async (e: React.FormEvent) => {
    e.preventDefault();
    if (!versionLabel.trim()) {
      setError('Version tag is required.');
      return;
    }
    if (!file) {
      setError('Please select a file to upload.');
      return;
    }

    try {
      await uploadVersionMutation.mutateAsync({
        documentId,
        data: {
          versionLabel: versionLabel.trim(),
          notes: notes.trim() || undefined,
          isDefault,
          file,
        },
      });
      onClose();
    } catch (err: any) {
      setError(err?.response?.data?.detail || err?.message || 'Failed to upload version.');
    }
  };

  return (
    <div className="fixed inset-0 z-50 flex items-center justify-center p-4 bg-background/80 backdrop-blur-sm animate-in fade-in duration-200">
      <div className="border border-border bg-card max-w-md w-full rounded-2xl shadow-2xl p-6 space-y-6">
        {/* Header */}
        <div className="flex items-center justify-between pb-3 border-b border-border">
          <div className="flex items-center gap-2.5">
            <div className="p-2 rounded-lg bg-primary/10 text-primary">
              <UploadCloud className="h-5 w-5" />
            </div>
            <div>
              <h3 className="font-bold text-sm text-foreground">Upload New Version</h3>
              <p className="text-xs text-muted-foreground truncate max-w-[260px]">{documentTitle}</p>
            </div>
          </div>
          <button
            type="button"
            onClick={onClose}
            className="p-1.5 text-muted-foreground hover:text-foreground rounded-lg hover:bg-muted transition-colors"
          >
            <X className="h-4 w-4" />
          </button>
        </div>

        {error && (
          <div className="p-3 rounded-lg bg-destructive/10 border border-destructive/20 text-destructive text-xs flex items-center gap-2">
            <AlertCircle className="h-4 w-4 shrink-0" />
            <span>{error}</span>
          </div>
        )}

        <form onSubmit={handleSubmit} className="space-y-4">
          <div>
            <label className="block text-xs font-semibold text-foreground mb-1">
              Version Tag *
            </label>
            <input
              type="text"
              required
              value={versionLabel}
              onChange={(e) => setVersionLabel(e.target.value)}
              placeholder="e.g. v2 or tailored-ai"
              className="w-full px-3 py-2 text-xs bg-background border border-border rounded-lg text-foreground placeholder:text-muted-foreground focus:outline-none focus:ring-1 focus:ring-primary font-mono"
            />
          </div>

          <div>
            <label className="block text-xs font-semibold text-foreground mb-1">
              What changed in this version? (Notes)
            </label>
            <input
              type="text"
              value={notes}
              onChange={(e) => setNotes(e.target.value)}
              placeholder="e.g. Added leadership section and quantified achievements"
              className="w-full px-3 py-2 text-xs bg-background border border-border rounded-lg text-foreground placeholder:text-muted-foreground focus:outline-none focus:ring-1 focus:ring-primary"
            />
          </div>

          <div className="flex items-center gap-2 pt-1">
            <input
              type="checkbox"
              id="setAsDefault"
              checked={isDefault}
              onChange={(e) => setIsDefault(e.target.checked)}
              className="rounded border-border text-primary focus:ring-primary h-4 w-4"
            />
            <label htmlFor="setAsDefault" className="text-xs font-medium text-foreground cursor-pointer">
              Set as default active version for this document
            </label>
          </div>

          {/* File Dropzone */}
          <div>
            <label className="block text-xs font-semibold text-foreground mb-1">
              File (PDF, DOCX, PNG, JPG - max 10MB) *
            </label>
            <div className="relative border-2 border-dashed border-border rounded-xl p-4 text-center hover:bg-muted/30 transition-colors">
              <input
                type="file"
                required
                accept=".pdf,.docx,.png,.jpg,.jpeg"
                onChange={handleFileChange}
                className="absolute inset-0 w-full h-full opacity-0 cursor-pointer"
              />
              {file ? (
                <div className="flex items-center justify-center gap-2 text-xs font-medium text-foreground">
                  <FileText className="h-5 w-5 text-primary" />
                  <span className="truncate max-w-[240px]">{file.name}</span>
                  <span className="text-[11px] text-muted-foreground">
                    ({(file.size / 1024).toFixed(0)} KB)
                  </span>
                </div>
              ) : (
                <div className="space-y-1">
                  <UploadCloud className="h-6 w-6 text-muted-foreground mx-auto" />
                  <p className="text-xs text-foreground font-medium">Click to select new version file</p>
                  <p className="text-[11px] text-muted-foreground">PDF, DOCX, PNG, JPG</p>
                </div>
              )}
            </div>
          </div>

          {/* Actions */}
          <div className="flex items-center justify-end gap-2 pt-2 border-t border-border">
            <button
              type="button"
              onClick={onClose}
              className="px-3.5 py-1.5 text-xs font-medium text-muted-foreground hover:text-foreground hover:bg-muted rounded-lg"
            >
              Cancel
            </button>
            <button
              type="submit"
              disabled={uploadVersionMutation.isPending || !file}
              className="px-4 py-1.5 text-xs font-semibold bg-primary text-primary-foreground rounded-lg hover:opacity-90 disabled:opacity-50"
            >
              {uploadVersionMutation.isPending ? 'Uploading...' : 'Upload Version'}
            </button>
          </div>
        </form>
      </div>
    </div>
  );
};
