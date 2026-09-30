export interface DiscoveredJob {
  id: string;
  sourceId: string | null;
  sourceName: string;
  externalId: string;
  title: string;
  companyName: string;
  location: string | null;
  url: string;
  description: string | null;
  postedAt: string | null;
  tags: string[];
  status: 'New' | 'Saved' | 'Dismissed';
  fetchedAt: string;
}

export interface DiscoveredJobList {
  items: DiscoveredJob[];
  totalCount: number;
  page: number;
  pageSize: number;
}

export interface JobSource {
  id: string;
  name: string;
  type: string;
  baseUrl: string | null;
  enabled: boolean;
  lastRunAt: string | null;
}

export interface IngestJobsResult {
  totalFetched: number;
  newJobsStored: number;
  duplicatesSkipped: number;
}
