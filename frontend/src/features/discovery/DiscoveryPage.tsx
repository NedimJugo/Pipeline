import React, { useState } from 'react';
import {
  Search,
  RefreshCw,
  SlidersHorizontal,
  Compass,
  Briefcase,
  AlertCircle,
  Loader2,
  Check,
} from 'lucide-react';
import { Link } from 'react-router-dom';
import {
  useDiscoveredJobs,
  useJobSources,
  useSaveJobToWishlist,
  useDismissJob,
  useIngestJobs,
} from './useDiscovery';
import { DiscoveryExtensionBanner } from './DiscoveryExtensionBanner';
import { DiscoveredJobCard } from './DiscoveredJobCard';

export const DiscoveryPage: React.FC = () => {
  const [search, setSearch] = useState('');
  const [status, setStatus] = useState<string>('All');
  const [sourceId, setSourceId] = useState<string>('');
  const [page, setPage] = useState(1);
  const [savedNotice, setSavedNotice] = useState<{ id: string; title: string } | null>(null);

  const { data: jobsData, isLoading, error } = useDiscoveredJobs({
    page,
    pageSize: 18,
    status: status === 'All' ? undefined : status,
    search: search.trim() || undefined,
    sourceId: sourceId || undefined,
  });

  const { data: sources } = useJobSources();
  const saveMutation = useSaveJobToWishlist();
  const dismissMutation = useDismissJob();
  const ingestMutation = useIngestJobs();

  const handleSave = async (id: string) => {
    try {
      const app = await saveMutation.mutateAsync(id);
      setSavedNotice({ id: app.id, title: app.roleTitle });
      setTimeout(() => setSavedNotice(null), 5000);
    } catch (err) {
      console.error('Failed to save discovered job:', err);
    }
  };

  const handleDismiss = async (id: string) => {
    try {
      await dismissMutation.mutateAsync(id);
    } catch (err) {
      console.error('Failed to dismiss job:', err);
    }
  };

  const handleRefresh = async () => {
    try {
      await ingestMutation.mutateAsync();
    } catch (err) {
      console.error('Failed to ingest jobs:', err);
    }
  };

  return (
    <div className="max-w-7xl mx-auto space-y-6 pb-12">
      {/* Header */}
      <div className="flex flex-col sm:flex-row sm:items-center justify-between gap-4 pb-4 border-b border-border">
        <div>
          <div className="flex items-center gap-2">
            <h1 className="text-2xl font-bold tracking-tight">Job Discovery</h1>
            <span className="px-2 py-0.5 rounded text-[11px] font-bold bg-primary/10 text-primary border border-primary/20">
              Live Feed
            </span>
          </div>
          <p className="text-sm text-muted-foreground mt-1">
            Aggregate engineering opportunities and convert them to pipeline applications with one click
          </p>
        </div>

        <button
          type="button"
          onClick={handleRefresh}
          disabled={ingestMutation.isPending}
          className="inline-flex items-center gap-2 px-4 py-2 rounded-lg bg-secondary text-secondary-foreground text-xs font-bold hover:bg-secondary/80 transition-colors shadow-2xs self-start sm:self-auto disabled:opacity-50"
        >
          <RefreshCw className={`h-3.5 w-3.5 ${ingestMutation.isPending ? 'animate-spin' : ''}`} />
          <span>{ingestMutation.isPending ? 'Syncing Feeds...' : 'Refresh Feeds'}</span>
        </button>
      </div>

      {/* Extension Architecture Explainer */}
      <DiscoveryExtensionBanner />

      {/* Saved Notification Toast */}
      {savedNotice && (
        <div className="p-3.5 bg-emerald-500/10 border border-emerald-500/20 text-emerald-600 dark:text-emerald-400 rounded-xl text-xs font-semibold flex items-center justify-between shadow-xs animate-in slide-in-from-top duration-200">
          <div className="flex items-center gap-2">
            <Check className="h-4 w-4 shrink-0" />
            <span>
              <strong>{savedNotice.title}</strong> has been added to your <strong>Wishlist</strong> pipeline!
            </span>
          </div>
          <Link
            to={`/applications/${savedNotice.id}`}
            className="underline hover:text-emerald-700 dark:hover:text-emerald-300 font-bold shrink-0 ml-4"
          >
            View Application &rarr;
          </Link>
        </div>
      )}

      {/* Search & Filter Bar */}
      <div className="bg-card border border-border rounded-xl p-4 shadow-xs space-y-3">
        <div className="flex flex-col md:flex-row items-stretch md:items-center gap-3">
          {/* Search Box */}
          <div className="relative flex-1">
            <Search className="h-4 w-4 absolute left-3 top-1/2 -translate-y-1/2 text-muted-foreground" />
            <input
              type="text"
              value={search}
              onChange={(e) => {
                setSearch(e.target.value);
                setPage(1);
              }}
              placeholder="Search by role title, company, location, or tech stack..."
              className="w-full pl-9 pr-3 py-2 bg-background border border-border rounded-lg text-xs text-foreground placeholder:text-muted-foreground focus:outline-none focus:ring-1 focus:ring-primary"
            />
          </div>

          {/* Source Dropdown */}
          {sources && sources.length > 0 && (
            <div className="w-full md:w-56 shrink-0">
              <select
                value={sourceId}
                onChange={(e) => {
                  setSourceId(e.target.value);
                  setPage(1);
                }}
                className="w-full px-3 py-2 bg-background border border-border rounded-lg text-xs text-foreground focus:outline-none focus:ring-1 focus:ring-primary"
              >
                <option value="">All Sources</option>
                {sources.map((s) => (
                  <option key={s.id} value={s.id}>
                    {s.name}
                  </option>
                ))}
              </select>
            </div>
          )}
        </div>

        {/* Status Filter Tabs */}
        <div className="flex items-center gap-1.5 pt-2 border-t border-border overflow-x-auto">
          <span className="text-[11px] font-bold uppercase tracking-wider text-muted-foreground mr-1 flex items-center gap-1">
            <SlidersHorizontal className="h-3 w-3" />
            <span>Status:</span>
          </span>
          {['All', 'New', 'Saved', 'Dismissed'].map((st) => (
            <button
              key={st}
              type="button"
              onClick={() => {
                setStatus(st);
                setPage(1);
              }}
              className={`px-3 py-1 rounded-lg text-xs font-semibold transition-colors ${
                status === st
                  ? 'bg-primary text-primary-foreground shadow-2xs font-bold'
                  : 'text-muted-foreground hover:bg-muted hover:text-foreground'
              }`}
            >
              {st}
            </button>
          ))}
        </div>
      </div>

      {/* Content Area */}
      {isLoading ? (
        <div className="flex items-center justify-center min-h-[300px]">
          <div className="flex flex-col items-center gap-3">
            <Loader2 className="h-8 w-8 animate-spin text-primary" />
            <span className="text-xs text-muted-foreground">Aggregating job listings...</span>
          </div>
        </div>
      ) : error ? (
        <div className="p-6 rounded-xl border border-destructive/20 bg-destructive/5 text-destructive flex items-center gap-3">
          <AlertCircle className="h-5 w-5 shrink-0" />
          <span className="text-xs font-medium">Failed to load discovered jobs. Please try refreshing feeds.</span>
        </div>
      ) : !jobsData || jobsData.items.length === 0 ? (
        <div className="text-center py-16 bg-card border border-border rounded-xl p-8 space-y-3">
          <div className="p-3 bg-primary/10 text-primary w-fit mx-auto rounded-full">
            <Compass className="h-6 w-6" />
          </div>
          <h3 className="text-base font-bold text-foreground">No discovered jobs found</h3>
          <p className="text-xs text-muted-foreground max-w-md mx-auto">
            {search || status !== 'All'
              ? 'No listings matched your active filters. Try adjusting your search query or status filter.'
              : 'The discovery feed is currently waiting for source connector runs. Click "Refresh Feeds" to poll configured connectors.'}
          </p>
          {(search || status !== 'All') && (
            <button
              type="button"
              onClick={() => {
                setSearch('');
                setStatus('All');
                setSourceId('');
              }}
              className="mt-2 text-xs font-semibold text-primary hover:underline"
            >
              Clear all filters
            </button>
          )}
        </div>
      ) : (
        <div className="space-y-6">
          <div className="flex items-center justify-between text-xs text-muted-foreground px-1">
            <span>
              Showing {jobsData.items.length} of {jobsData.totalCount} discovered opportunities
            </span>
          </div>

          <div className="grid grid-cols-1 md:grid-cols-2 lg:grid-cols-3 gap-4">
            {jobsData.items.map((job) => (
              <DiscoveredJobCard
                key={job.id}
                job={job}
                onSave={handleSave}
                onDismiss={handleDismiss}
                isSaving={saveMutation.isPending && saveMutation.variables === job.id}
                isDismissing={dismissMutation.isPending && dismissMutation.variables === job.id}
              />
            ))}
          </div>

          {/* Pagination */}
          {jobsData.totalCount > jobsData.pageSize && (
            <div className="flex items-center justify-center gap-2 pt-4">
              <button
                type="button"
                disabled={page <= 1}
                onClick={() => setPage((p) => Math.max(1, p - 1))}
                className="px-3 py-1.5 rounded-lg border border-border text-xs font-semibold disabled:opacity-40 hover:bg-muted"
              >
                Previous
              </button>
              <span className="text-xs text-muted-foreground px-2">
                Page {page} of {Math.ceil(jobsData.totalCount / jobsData.pageSize)}
              </span>
              <button
                type="button"
                disabled={page * jobsData.pageSize >= jobsData.totalCount}
                onClick={() => setPage((p) => p + 1)}
                className="px-3 py-1.5 rounded-lg border border-border text-xs font-semibold disabled:opacity-40 hover:bg-muted"
              >
                Next
              </button>
            </div>
          )}
        </div>
      )}
    </div>
  );
};
