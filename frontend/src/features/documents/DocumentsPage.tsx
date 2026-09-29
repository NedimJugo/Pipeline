import React, { useState } from 'react';
import {
  FileText,
  Plus,
  BarChart2,
  Search,
  Filter,
  Layers,
  Sparkles,
  Award,
} from 'lucide-react';
import { DocumentType, Document } from './types';
import { useDocuments, useDocumentStatsSummary } from './useDocuments';
import { DocumentCard } from './components/DocumentCard';
import { UploadDocumentModal } from './components/UploadDocumentModal';
import { UploadVersionModal } from './components/UploadVersionModal';
import { PdfViewerModal } from './components/PdfViewerModal';
import { DocumentVersionCompareModal } from './components/DocumentVersionCompareModal';
import clsx from 'clsx';

export const DocumentsPage: React.FC = () => {
  const [selectedCategory, setSelectedCategory] = useState<DocumentType | 'ALL'>('ALL');
  const [searchQuery, setSearchQuery] = useState('');
  const [isUploadDocOpen, setIsUploadDocOpen] = useState(false);
  const [selectedDocForVersion, setSelectedDocForVersion] = useState<Document | null>(null);
  const [isCompareModalOpen, setIsCompareModalOpen] = useState(false);
  const [previewPdfInfo, setPreviewPdfInfo] = useState<{ versionId: string; fileName: string } | null>(null);

  const { data: documents = [], isLoading } = useDocuments(
    selectedCategory === 'ALL' ? undefined : selectedCategory
  );
  const { data: statsSummary } = useDocumentStatsSummary();

  const filteredDocuments = documents.filter((d) => {
    if (!searchQuery.trim()) return true;
    const q = searchQuery.toLowerCase();
    return (
      d.title.toLowerCase().includes(q) ||
      d.description?.toLowerCase().includes(q) ||
      d.versions.some((v) => v.fileName.toLowerCase().includes(q) || v.versionLabel.toLowerCase().includes(q))
    );
  });

  const topCv = statsSummary?.cvVersionStats?.[0];

  return (
    <div className="space-y-6">
      {/* Page Header */}
      <div className="flex flex-col sm:flex-row sm:items-center justify-between gap-4">
        <div>
          <h1 className="text-2xl font-bold tracking-tight text-foreground">Documents & Versions</h1>
          <p className="text-xs text-muted-foreground mt-0.5">
            CV versioning, tailored cover letters, and live interview conversion tracking
          </p>
        </div>

        <div className="flex items-center gap-2">
          <button
            type="button"
            onClick={() => setIsCompareModalOpen(true)}
            className="inline-flex items-center gap-1.5 px-3 py-2 text-xs font-semibold bg-card border border-border rounded-xl text-foreground hover:bg-muted shadow-2xs transition-colors"
          >
            <BarChart2 className="h-4 w-4 text-indigo-500" />
            <span>Compare Versions</span>
          </button>

          <button
            type="button"
            onClick={() => setIsUploadDocOpen(true)}
            className="inline-flex items-center gap-1.5 px-3.5 py-2 text-xs font-semibold bg-primary text-primary-foreground rounded-xl shadow-sm hover:opacity-90 transition-opacity"
          >
            <Plus className="h-4 w-4" />
            <span>Upload Document</span>
          </button>
        </div>
      </div>

      {/* Analytics Overview Cards */}
      <div className="grid grid-cols-1 sm:grid-cols-3 gap-4">
        <div className="bg-card border border-border rounded-xl p-4 shadow-2xs space-y-1">
          <span className="text-[11px] font-bold uppercase tracking-wider text-muted-foreground block">
            Total Documents
          </span>
          <div className="flex items-center justify-between">
            <span className="text-2xl font-black text-foreground">
              {statsSummary?.totalDocuments ?? documents.length}
            </span>
            <div className="p-2 rounded-lg bg-primary/10 text-primary">
              <FileText className="h-4 w-4" />
            </div>
          </div>
        </div>

        <div className="bg-card border border-border rounded-xl p-4 shadow-2xs space-y-1">
          <span className="text-[11px] font-bold uppercase tracking-wider text-muted-foreground block">
            Version Iterations
          </span>
          <div className="flex items-center justify-between">
            <span className="text-2xl font-black text-foreground">
              {statsSummary?.totalVersions ?? 0}
            </span>
            <div className="p-2 rounded-lg bg-indigo-500/10 text-indigo-500">
              <Layers className="h-4 w-4" />
            </div>
          </div>
        </div>

        <div className="bg-card border border-border rounded-xl p-4 shadow-2xs space-y-1">
          <span className="text-[11px] font-bold uppercase tracking-wider text-muted-foreground block">
            Top Converting CV
          </span>
          <div className="flex items-center justify-between">
            <div>
              {topCv && topCv.sentCount > 0 ? (
                <>
                  <span className="text-sm font-bold text-foreground block truncate max-w-[150px]">
                    {topCv.versionLabel} ({topCv.interviewRate}% Int.)
                  </span>
                  <span className="text-[10px] text-muted-foreground">
                    Sent {topCv.sentCount}x
                  </span>
                </>
              ) : (
                <span className="text-xs text-muted-foreground italic">Link CVs to track stats</span>
              )}
            </div>
            <div className="p-2 rounded-lg bg-emerald-500/10 text-emerald-500">
              <Award className="h-4 w-4" />
            </div>
          </div>
        </div>
      </div>

      {/* Filter Tabs & Search */}
      <div className="flex flex-col sm:flex-row sm:items-center justify-between gap-4 pt-1">
        <div className="flex items-center gap-1.5 overflow-x-auto pb-1 sm:pb-0">
          {[
            { id: 'ALL', label: 'All Documents' },
            { id: 'CV', label: 'CV / Resumes' },
            { id: 'CoverLetter', label: 'Cover Letters' },
            { id: 'Portfolio', label: 'Portfolios' },
            { id: 'Certificate', label: 'Certificates' },
            { id: 'Other', label: 'Other' },
          ].map((tab) => (
            <button
              key={tab.id}
              type="button"
              onClick={() => setSelectedCategory(tab.id as DocumentType | 'ALL')}
              className={clsx(
                'px-3 py-1.5 text-xs font-semibold rounded-lg transition-colors whitespace-nowrap',
                selectedCategory === tab.id
                  ? 'bg-primary text-primary-foreground shadow-2xs'
                  : 'bg-card border border-border text-muted-foreground hover:text-foreground hover:bg-muted'
              )}
            >
              {tab.label}
            </button>
          ))}
        </div>

        <div className="relative max-w-xs w-full">
          <Search className="h-4 w-4 text-muted-foreground absolute left-3 top-1/2 -translate-y-1/2" />
          <input
            type="text"
            value={searchQuery}
            onChange={(e) => setSearchQuery(e.target.value)}
            placeholder="Search documents or files..."
            className="w-full pl-9 pr-3 py-1.5 text-xs bg-background border border-border rounded-lg text-foreground placeholder:text-muted-foreground focus:outline-none focus:ring-1 focus:ring-primary"
          />
        </div>
      </div>

      {/* Document Cards List */}
      {isLoading ? (
        <div className="py-20 text-center space-y-3">
          <div className="h-8 w-8 rounded-full border-2 border-primary border-t-transparent animate-spin mx-auto" />
          <p className="text-xs text-muted-foreground">Loading documents & versions...</p>
        </div>
      ) : filteredDocuments.length === 0 ? (
        <div className="py-16 text-center border-2 border-dashed border-border rounded-2xl p-8 space-y-4">
          <div className="p-3 bg-muted rounded-full w-fit mx-auto text-muted-foreground">
            <FileText className="h-8 w-8" />
          </div>
          <div className="space-y-1">
            <h3 className="font-semibold text-sm text-foreground">No documents found</h3>
            <p className="text-xs text-muted-foreground max-w-sm mx-auto">
              {searchQuery
                ? 'No documents match your search filters.'
                : 'Upload your CV and cover letter versions to track conversions and link them to job applications.'}
            </p>
          </div>
          <button
            type="button"
            onClick={() => setIsUploadDocOpen(true)}
            className="inline-flex items-center gap-1.5 px-4 py-2 text-xs font-semibold bg-primary text-primary-foreground rounded-lg"
          >
            <Plus className="h-4 w-4" />
            <span>Upload First Document</span>
          </button>
        </div>
      ) : (
        <div className="space-y-4">
          {filteredDocuments.map((doc) => (
            <DocumentCard
              key={doc.id}
              document={doc}
              onUploadVersion={(d) => setSelectedDocForVersion(d)}
              onPreviewPdf={(versionId, fileName) => setPreviewPdfInfo({ versionId, fileName })}
            />
          ))}
        </div>
      )}

      {/* Modals */}
      {isUploadDocOpen && (
        <UploadDocumentModal
          isOpen={isUploadDocOpen}
          onClose={() => setIsUploadDocOpen(false)}
          defaultType={selectedCategory === 'ALL' ? 'CV' : selectedCategory}
        />
      )}

      {selectedDocForVersion && (
        <UploadVersionModal
          isOpen={!!selectedDocForVersion}
          onClose={() => setSelectedDocForVersion(null)}
          documentId={selectedDocForVersion.id}
          documentTitle={selectedDocForVersion.title}
          suggestedVersionLabel={`v${selectedDocForVersion.versionCount + 1}`}
        />
      )}

      {previewPdfInfo && (
        <PdfViewerModal
          isOpen={!!previewPdfInfo}
          onClose={() => setPreviewPdfInfo(null)}
          versionId={previewPdfInfo.versionId}
          fileName={previewPdfInfo.fileName}
        />
      )}

      {isCompareModalOpen && (
        <DocumentVersionCompareModal
          isOpen={isCompareModalOpen}
          onClose={() => setIsCompareModalOpen(false)}
          initialType={selectedCategory === 'CoverLetter' ? 'CoverLetter' : 'CV'}
        />
      )}
    </div>
  );
};
