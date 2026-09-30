import React, { useEffect, useState, useRef } from 'react';
import { useNavigate } from 'react-router-dom';
import {
  Search,
  Briefcase,
  Building2,
  Calendar,
  BarChart3,
  CheckSquare,
  Users,
  FileText,
  Sliders,
  Award,
  TrendingUp,
  X,
  ArrowRight,
  Sparkles,
} from 'lucide-react';
import { api } from '@/lib/api-client';

interface SearchResultItem {
  id: string;
  title: string;
  subtitle: string;
  type: 'action' | 'application' | 'company';
  path: string;
}

export const CommandPalette: React.FC = () => {
  const [isOpen, setIsOpen] = useState(false);
  const [query, setQuery] = useState('');
  const [selectedIndex, setSelectedIndex] = useState(0);
  const [applications, setApplications] = useState<any[]>([]);
  const [companies, setCompanies] = useState<any[]>([]);
  const [isLoading, setIsLoading] = useState(false);
  const inputRef = useRef<HTMLInputElement>(null);
  const navigate = useNavigate();

  // Listen for Ctrl+K / Cmd+K and custom event
  useEffect(() => {
    const handleKeyDown = (e: KeyboardEvent) => {
      if ((e.ctrlKey || e.metaKey) && e.key.toLowerCase() === 'k') {
        e.preventDefault();
        setIsOpen((prev) => !prev);
      } else if (e.key === 'Escape' && isOpen) {
        setIsOpen(false);
      }
    };

    const handleCustomOpen = () => setIsOpen(true);

    window.addEventListener('keydown', handleKeyDown);
    window.addEventListener('pipeline_open_command_palette', handleCustomOpen);

    return () => {
      window.removeEventListener('keydown', handleKeyDown);
      window.removeEventListener('pipeline_open_command_palette', handleCustomOpen);
    };
  }, [isOpen]);

  useEffect(() => {
    if (isOpen) {
      setTimeout(() => inputRef.current?.focus(), 50);
      setSelectedIndex(0);
    } else {
      setQuery('');
      setApplications([]);
      setCompanies([]);
    }
  }, [isOpen]);

  // Debounced search
  useEffect(() => {
    if (!query.trim()) {
      setApplications([]);
      setCompanies([]);
      return;
    }

    const timer = setTimeout(async () => {
      setIsLoading(true);
      try {
        const res = await api.get('/api/search', { params: { q: query.trim() } });
        setApplications(res.data?.applications || []);
        setCompanies(res.data?.companies || []);
      } catch (err) {
        console.error('Command palette search error:', err);
      } finally {
        setIsLoading(false);
      }
    }, 200);

    return () => clearTimeout(timer);
  }, [query]);

  // Standard Quick Navigation Actions
  const quickActions: SearchResultItem[] = [
    { id: 'act-apps', title: 'Applications Pipeline', subtitle: 'View kanban board & all tracked jobs', type: 'action', path: '/applications' },
    { id: 'act-calendar', title: 'Recruitment Calendar', subtitle: 'Interviews, deadlines & follow-ups', type: 'action', path: '/calendar' },
    { id: 'act-analytics', title: 'Analytics & Insights', subtitle: 'Conversion funnel & channel performance', type: 'action', path: '/analytics' },
    { id: 'act-tasks', title: 'Tasks & Next Actions', subtitle: 'Daily deliverables & automation items', type: 'action', path: '/tasks' },
    { id: 'act-contacts', title: 'Networking Contacts', subtitle: 'Recruiters, hiring managers & peers', type: 'action', path: '/contacts' },
    { id: 'act-offers', title: 'Offer Comparison Matrix', subtitle: 'Compare total compensation & benefits', type: 'action', path: '/offers/compare' },
    { id: 'act-references', title: 'Professional References', subtitle: 'Reference directory & consent tracking', type: 'action', path: '/references' },
    { id: 'act-docs', title: 'Resume & Documents', subtitle: 'CV versions & application materials', type: 'action', path: '/documents' },
    { id: 'act-settings', title: 'Account & Settings', subtitle: 'Preferences, CSV data & GDPR exports', type: 'action', path: '/settings' },
  ];

  // Filter actions based on query
  const filteredActions = query.trim()
    ? quickActions.filter(
        (a) =>
          a.title.toLowerCase().includes(query.toLowerCase()) ||
          a.subtitle.toLowerCase().includes(query.toLowerCase())
      )
    : quickActions.slice(0, 5);

  // Map backend results to unified list items
  const appItems: SearchResultItem[] = applications.map((a) => ({
    id: a.id,
    title: a.roleTitle,
    subtitle: `${a.companyName} • ${a.status} • ${a.workMode}`,
    type: 'application',
    path: `/applications/${a.id}`,
  }));

  const companyItems: SearchResultItem[] = companies.map((c) => ({
    id: c.id,
    title: c.name,
    subtitle: c.industry ? `${c.industry} • ${c.location || 'Company'}` : 'Tracked Company',
    type: 'company',
    path: `/applications?search=${encodeURIComponent(c.name)}`,
  }));

  const combinedItems: SearchResultItem[] = [
    ...appItems,
    ...companyItems,
    ...filteredActions,
  ];

  // Handle keyboard cycling and execution
  const handleKeyDown = (e: React.KeyboardEvent) => {
    if (combinedItems.length === 0) return;

    if (e.key === 'ArrowDown') {
      e.preventDefault();
      setSelectedIndex((prev) => (prev + 1) % combinedItems.length);
    } else if (e.key === 'ArrowUp') {
      e.preventDefault();
      setSelectedIndex((prev) => (prev - 1 + combinedItems.length) % combinedItems.length);
    } else if (e.key === 'Enter') {
      e.preventDefault();
      const item = combinedItems[selectedIndex];
      if (item) {
        navigate(item.path);
        setIsOpen(false);
      }
    }
  };

  if (!isOpen) return null;

  return (
    <div
      role="dialog"
      aria-modal="true"
      aria-label="Command Palette"
      className="fixed inset-0 z-50 flex items-start justify-center pt-20 p-4 bg-black/60 backdrop-blur-sm animate-in fade-in duration-150"
      onClick={() => setIsOpen(false)}
    >
      <div
        className="w-full max-w-xl bg-card border border-border rounded-xl shadow-2xl overflow-hidden flex flex-col max-h-[75vh]"
        onClick={(e) => e.stopPropagation()}
        onKeyDown={handleKeyDown}
      >
        {/* Search Input Bar */}
        <div className="flex items-center px-4 py-3 border-b border-border bg-muted/20 gap-3">
          <Search className="h-5 w-5 text-muted-foreground shrink-0" />
          <input
            ref={inputRef}
            type="text"
            value={query}
            onChange={(e) => {
              setQuery(e.target.value);
              setSelectedIndex(0);
            }}
            placeholder="Search applications, companies, or quick actions..."
            className="flex-1 bg-transparent text-sm text-foreground placeholder:text-muted-foreground focus:outline-none"
          />
          {query && (
            <button
              onClick={() => setQuery('')}
              className="p-1 rounded text-muted-foreground hover:text-foreground"
            >
              <X className="h-4 w-4" />
            </button>
          )}
          <kbd className="hidden sm:inline-block font-mono text-[10px] bg-muted px-1.5 py-0.5 rounded border border-border text-muted-foreground">
            ESC
          </kbd>
        </div>

        {/* Results List */}
        <div className="flex-1 overflow-y-auto p-2 space-y-4">
          {isLoading && (
            <div className="p-4 text-center text-xs text-muted-foreground animate-pulse">
              Searching command center...
            </div>
          )}

          {/* Applications Section */}
          {appItems.length > 0 && (
            <div>
              <div className="px-3 py-1 text-[11px] font-bold uppercase tracking-wider text-muted-foreground flex items-center gap-1.5">
                <Briefcase className="h-3.5 w-3.5 text-primary" />
                <span>Applications</span>
              </div>
              <div className="space-y-0.5 mt-1">
                {appItems.map((item, idx) => {
                  const isSelected = selectedIndex === idx;
                  return (
                    <button
                      key={item.id}
                      onClick={() => {
                        navigate(item.path);
                        setIsOpen(false);
                      }}
                      className={`w-full flex items-center justify-between px-3 py-2 rounded-lg text-left text-xs transition-colors ${
                        isSelected ? 'bg-primary text-primary-foreground font-semibold' : 'hover:bg-muted text-foreground'
                      }`}
                    >
                      <div className="truncate">
                        <div className="font-medium truncate">{item.title}</div>
                        <div className={`text-[11px] truncate ${isSelected ? 'text-primary-foreground/80' : 'text-muted-foreground'}`}>
                          {item.subtitle}
                        </div>
                      </div>
                      <ArrowRight className="h-3.5 w-3.5 opacity-60 ml-2 shrink-0" />
                    </button>
                  );
                })}
              </div>
            </div>
          )}

          {/* Companies Section */}
          {companyItems.length > 0 && (
            <div>
              <div className="px-3 py-1 text-[11px] font-bold uppercase tracking-wider text-muted-foreground flex items-center gap-1.5">
                <Building2 className="h-3.5 w-3.5 text-blue-500" />
                <span>Companies</span>
              </div>
              <div className="space-y-0.5 mt-1">
                {companyItems.map((item, idx) => {
                  const globalIdx = appItems.length + idx;
                  const isSelected = selectedIndex === globalIdx;
                  return (
                    <button
                      key={item.id}
                      onClick={() => {
                        navigate(item.path);
                        setIsOpen(false);
                      }}
                      className={`w-full flex items-center justify-between px-3 py-2 rounded-lg text-left text-xs transition-colors ${
                        isSelected ? 'bg-primary text-primary-foreground font-semibold' : 'hover:bg-muted text-foreground'
                      }`}
                    >
                      <div className="truncate">
                        <div className="font-medium truncate">{item.title}</div>
                        <div className={`text-[11px] truncate ${isSelected ? 'text-primary-foreground/80' : 'text-muted-foreground'}`}>
                          {item.subtitle}
                        </div>
                      </div>
                      <ArrowRight className="h-3.5 w-3.5 opacity-60 ml-2 shrink-0" />
                    </button>
                  );
                })}
              </div>
            </div>
          )}

          {/* Quick Actions Section */}
          {filteredActions.length > 0 && (
            <div>
              <div className="px-3 py-1 text-[11px] font-bold uppercase tracking-wider text-muted-foreground flex items-center gap-1.5">
                <Sparkles className="h-3.5 w-3.5 text-amber-500" />
                <span>Quick Actions</span>
              </div>
              <div className="space-y-0.5 mt-1">
                {filteredActions.map((item, idx) => {
                  const globalIdx = appItems.length + companyItems.length + idx;
                  const isSelected = selectedIndex === globalIdx;
                  return (
                    <button
                      key={item.id}
                      onClick={() => {
                        navigate(item.path);
                        setIsOpen(false);
                      }}
                      className={`w-full flex items-center justify-between px-3 py-2 rounded-lg text-left text-xs transition-colors ${
                        isSelected ? 'bg-primary text-primary-foreground font-semibold' : 'hover:bg-muted text-foreground'
                      }`}
                    >
                      <div className="truncate">
                        <div className="font-medium truncate">{item.title}</div>
                        <div className={`text-[11px] truncate ${isSelected ? 'text-primary-foreground/80' : 'text-muted-foreground'}`}>
                          {item.subtitle}
                        </div>
                      </div>
                      <span className={`text-[10px] font-mono px-1.5 py-0.5 rounded ${isSelected ? 'bg-primary-foreground/20' : 'bg-muted'}`}>
                        Jump
                      </span>
                    </button>
                  );
                })}
              </div>
            </div>
          )}

          {combinedItems.length === 0 && !isLoading && (
            <div className="p-8 text-center text-xs text-muted-foreground">
              No matching applications, companies, or commands found for "{query}".
            </div>
          )}
        </div>

        {/* Footer shortcuts */}
        <div className="px-4 py-2 border-t border-border bg-muted/30 text-[11px] text-muted-foreground flex items-center justify-between">
          <div className="flex items-center gap-3">
            <span>↑↓ Navigate</span>
            <span>↵ Select</span>
            <span>ESC Close</span>
          </div>
          <span className="font-mono text-[10px]">Pipeline Command Center</span>
        </div>
      </div>
    </div>
  );
};
