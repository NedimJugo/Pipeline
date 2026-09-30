import React, { useState } from 'react';
import { Layers, ChevronDown, ChevronUp, Code, ExternalLink, Terminal } from 'lucide-react';

export const DiscoveryExtensionBanner: React.FC = () => {
  const [isExpanded, setIsExpanded] = useState(false);

  return (
    <div className="bg-gradient-to-r from-primary/10 via-card to-background border border-primary/20 rounded-xl p-5 shadow-xs transition-all">
      <div className="flex flex-col sm:flex-row sm:items-center justify-between gap-4">
        <div className="flex items-start sm:items-center gap-3">
          <div className="p-2.5 rounded-lg bg-primary text-primary-foreground shrink-0 shadow-2xs">
            <Layers className="h-5 w-5" />
          </div>
          <div>
            <div className="flex items-center gap-2">
              <h2 className="text-sm font-bold tracking-tight text-foreground">
                Job Discovery Extension Architecture
              </h2>
              <span className="px-2 py-0.5 rounded text-[10px] font-bold uppercase tracking-wider bg-primary/20 text-primary border border-primary/30">
                Pluggable Connector API
              </span>
            </div>
            <p className="text-xs text-muted-foreground mt-0.5">
              Built with the <code className="text-primary font-mono text-[11px]">IJobSourceConnector</code> interface. Add scrapers, RSS feeds, or custom ATS integrations with zero core refactoring.
            </p>
          </div>
        </div>

        <div className="flex items-center gap-2 shrink-0">
          <button
            type="button"
            onClick={() => setIsExpanded(!isExpanded)}
            className="inline-flex items-center gap-1.5 px-3 py-1.5 rounded-lg border border-border text-xs font-semibold hover:bg-muted text-foreground transition-colors"
          >
            <Code className="h-3.5 w-3.5" />
            <span>{isExpanded ? 'Hide Spec' : 'View Spec'}</span>
            {isExpanded ? <ChevronUp className="h-3.5 w-3.5" /> : <ChevronDown className="h-3.5 w-3.5" />}
          </button>
        </div>
      </div>

      {isExpanded && (
        <div className="mt-4 pt-4 border-t border-border/80 space-y-3 animate-in fade-in duration-200">
          <p className="text-xs text-muted-foreground">
            The discovery engine aggregates external job opportunities into a unified pipeline. Discovered jobs can be converted into tracked applications in your Wishlist with a single click.
          </p>

          <div className="bg-muted/40 border border-border rounded-lg p-3 font-mono text-xs overflow-x-auto space-y-1 text-foreground">
            <div className="text-muted-foreground">// 1. Implement connector in Pipeline.Infrastructure</div>
            <div><span className="text-blue-500">public interface</span> <span className="text-emerald-500">IJobSourceConnector</span> &#123;</div>
            <div className="pl-4"><span className="text-blue-500">string</span> SourceKey &#123; <span className="text-blue-500">get</span>; &#125;</div>
            <div className="pl-4"><span className="text-blue-500">Task</span>&lt;<span className="text-blue-500">IReadOnlyList</span>&lt;<span className="text-emerald-500">RawJob</span>&gt;&gt; FetchAsync(<span className="text-emerald-500">JobSource</span> source, <span className="text-emerald-500">CancellationToken</span> ct);</div>
            <div>&#125;</div>
          </div>

          <div className="flex items-center justify-between text-[11px] text-muted-foreground pt-1">
            <span className="flex items-center gap-1">
              <Terminal className="h-3.5 w-3.5 text-primary" />
              <span>Full setup guide available in repository root: <code className="font-semibold text-foreground">docs/discovery.md</code></span>
            </span>
          </div>
        </div>
      )}
    </div>
  );
};
