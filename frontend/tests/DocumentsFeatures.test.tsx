import { describe, it, expect, vi } from 'vitest';
import { render, screen } from '@testing-library/react';
import React from 'react';
import { BrowserRouter } from 'react-router-dom';
import { QueryClient, QueryClientProvider } from '@tanstack/react-query';
import { DocumentCard } from '@/features/documents/components/DocumentCard';
import { DocumentVersionCompareModal } from '@/features/documents/components/DocumentVersionCompareModal';
import { ApplicationDocumentsTab } from '@/features/applications/components/ApplicationDocumentsTab';
import { Document } from '@/features/documents/types';
import { ApplicationDetail } from '@/features/applications/types';

const queryClient = new QueryClient({
  defaultOptions: {
    queries: {
      retry: false,
    },
  },
});

const renderWithProviders = (ui: React.ReactElement) => {
  return render(
    <QueryClientProvider client={queryClient}>
      <BrowserRouter>{ui}</BrowserRouter>
    </QueryClientProvider>
  );
};

describe('DocumentCard', () => {
  const mockDocument: Document = {
    id: 'doc-123',
    type: 'CV',
    title: 'Senior Software Engineer CV',
    description: 'Primary resume tailored for distributed systems roles',
    defaultVersionId: 'ver-2',
    defaultVersionLabel: 'v2',
    versionCount: 2,
    createdAt: new Date().toISOString(),
    updatedAt: new Date().toISOString(),
    versions: [
      {
        id: 'ver-2',
        documentId: 'doc-123',
        versionLabel: 'v2',
        fileName: 'resume_v2.pdf',
        contentType: 'application/pdf',
        sizeBytes: 204800,
        notes: 'Updated with Kubernetes and gRPC projects',
        isDefault: true,
        createdAt: new Date().toISOString(),
        stats: {
          versionId: 'ver-2',
          versionLabel: 'v2',
          documentTitle: 'Senior Software Engineer CV',
          documentType: 'CV',
          sentCount: 5,
          replyCount: 4,
          interviewCount: 3,
          offerCount: 1,
          responseRate: 80.0,
          interviewRate: 60.0,
          offerRate: 20.0,
        },
      },
      {
        id: 'ver-1',
        documentId: 'doc-123',
        versionLabel: 'v1',
        fileName: 'resume_v1.pdf',
        contentType: 'application/pdf',
        sizeBytes: 153600,
        notes: 'Initial draft',
        isDefault: false,
        createdAt: new Date().toISOString(),
      },
    ],
  };

  it('renders document title, type badge, and default version tag', () => {
    renderWithProviders(
      <DocumentCard
        document={mockDocument}
        onUploadVersion={vi.fn()}
        onPreviewPdf={vi.fn()}
      />
    );

    expect(screen.getByText('Senior Software Engineer CV')).toBeInTheDocument();
    expect(screen.getByText('CV')).toBeInTheDocument();
    expect(screen.getByText('Default: v2')).toBeInTheDocument();
    expect(screen.getByText('2 versions')).toBeInTheDocument();
    expect(screen.getByText('resume_v2.pdf')).toBeInTheDocument();
    expect(screen.getByText('resume_v1.pdf')).toBeInTheDocument();
    expect(screen.getByText('Active Default')).toBeInTheDocument();
    expect(screen.getByText('Set Default')).toBeInTheDocument();
  });
});

describe('DocumentVersionCompareModal', () => {
  it('renders modal header and tab options when open', () => {
    renderWithProviders(
      <DocumentVersionCompareModal isOpen={true} onClose={vi.fn()} />
    );

    expect(screen.getByText('Compare Version Performance')).toBeInTheDocument();
    expect(screen.getByText('CVs / Resumes')).toBeInTheDocument();
    expect(screen.getByText('Cover Letters')).toBeInTheDocument();
  });
});

describe('ApplicationDocumentsTab', () => {
  const mockApplication: ApplicationDetail = {
    id: 'app-999',
    companyId: 'comp-1',
    companyName: 'Datadog',
    companyWebsite: 'https://datadoghq.com',
    roleTitle: 'Staff Backend Engineer',
    source: 'LinkedIn',
    status: 'Interview',
    statusChangedAt: new Date().toISOString(),
    workMode: 'Remote',
    employmentType: 'FullTime',
    currency: 'USD',
    priority: 3,
    favorite: true,
    excitementRating: 5,
    daysInStage: 4,
    createdAt: new Date().toISOString(),
    updatedAt: new Date().toISOString(),
    documentVersionCvId: 'ver-2',
    documentVersionCvLabel: 'v2',
  };

  it('renders CV and Cover Letter linking sections', () => {
    renderWithProviders(<ApplicationDocumentsTab application={mockApplication} />);

    expect(screen.getByText('CV / Resume Sent')).toBeInTheDocument();
    expect(screen.getByText('Cover Letter Sent')).toBeInTheDocument();
    expect(screen.getByText('Save Linked Document Versions')).toBeInTheDocument();
    expect(
      screen.getByText('Career Attachments & Offer Letters')
    ).toBeInTheDocument();
  });
});
