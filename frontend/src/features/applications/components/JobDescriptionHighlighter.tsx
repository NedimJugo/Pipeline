import React, { useState, useMemo } from 'react';
import { Copy, Check, Sparkles, Search } from 'lucide-react';
import { clsx } from 'clsx';

interface JobDescriptionHighlighterProps {
  jobDescription?: string | null;
  onEditClick?: () => void;
}

const COMMON_SKILLS = [
  // Languages & Runtimes
  'TypeScript', 'JavaScript', 'Python', 'C#', '.NET', 'Go', 'Golang', 'Java', 'Rust', 'C++',
  'Ruby', 'PHP', 'Swift', 'Kotlin', 'SQL', 'HTML', 'CSS', 'Bash',
  // Frameworks & Libraries
  'React', 'Next.js', 'Vue', 'Angular', 'Svelte', 'Node.js', 'Express', 'ASP.NET',
  'Django', 'Flask', 'FastAPI', 'Spring', 'Spring Boot', 'Rails', 'Tailwind', 'TailwindCSS',
  'Redux', 'Zustand', 'GraphQL', 'REST', 'RESTful', 'gRPC', 'WebSockets',
  // Data & Storage
  'PostgreSQL', 'Postgres', 'MySQL', 'SQLite', 'MongoDB', 'Redis', 'Elasticsearch',
  'DynamoDB', 'Cassandra', 'Prisma', 'EF Core', 'Entity Framework',
  // Cloud & DevOps
  'AWS', 'Azure', 'GCP', 'Google Cloud', 'Docker', 'Kubernetes', 'K8s', 'Terraform',
  'CI/CD', 'GitHub Actions', 'GitLab', 'Linux', 'Nginx', 'Kafka', 'RabbitMQ', 'Microservices',
  // Methodologies & Practices
  'TDD', 'Agile', 'Scrum', 'Clean Architecture', 'DDD', 'System Design', 'CI/CD'
];

export const JobDescriptionHighlighter: React.FC<JobDescriptionHighlighterProps> = ({
  jobDescription,
  onEditClick,
}) => {
  const [copied, setCopied] = useState(false);
  const [filterKeyword, setFilterKeyword] = useState('');

  // Find detected skills in text
  const detectedSkills = useMemo(() => {
    if (!jobDescription) return [];
    const text = jobDescription.toLowerCase();
    return COMMON_SKILLS.filter((skill) => {
      // Escape special characters for regex like .NET or C++ or C#
      const escaped = skill.replace(/[.*+?^${}()|[\]\\]/g, '\\$&');
      const regex = new RegExp(`(^|[^a-zA-Z0-9_#+])${escaped}(?=[^a-zA-Z0-9_#+]|$)`, 'i');
      return regex.test(text);
    });
  }, [jobDescription]);

  // Handle copy
  const handleCopy = async () => {
    if (!jobDescription) return;
    await navigator.clipboard.writeText(jobDescription);
    setCopied(true);
    setTimeout(() => setCopied(false), 2000);
  };

  // Highlight keywords in the text
  const highlightedContent = useMemo(() => {
    if (!jobDescription) return null;

    // Combine detected skills and active filter keyword
    const targetKeywords = Array.from(
      new Set(
        [
          ...detectedSkills,
          ...(filterKeyword.trim() ? [filterKeyword.trim()] : []),
        ].filter(Boolean)
      )
    );

    if (targetKeywords.length === 0) {
      return <span>{jobDescription}</span>;
    }

    // Sort descending by length so longer terms match first (e.g., "Spring Boot" before "Spring")
    const sortedKeywords = [...targetKeywords].sort((a, b) => b.length - a.length);
    const escapedTerms = sortedKeywords.map((k) =>
      k.replace(/[.*+?^${}()|[\]\\]/g, '\\$&')
    );

    const regex = new RegExp(`(${escapedTerms.join('|')})`, 'gi');
    const parts = jobDescription.split(regex);

    return parts.map((part, index) => {
      const match = sortedKeywords.find(
        (kw) => kw.toLowerCase() === part.toLowerCase()
      );
      if (match) {
        const isFiltered = filterKeyword && match.toLowerCase() === filterKeyword.toLowerCase();
        return (
          <mark
            key={index}
            className={clsx(
              'px-1 py-0.5 rounded font-semibold text-xs transition-colors',
              isFiltered
                ? 'bg-amber-500/20 text-amber-600 dark:text-amber-400 ring-1 ring-amber-500'
                : 'bg-primary/10 text-primary dark:bg-primary/20 dark:text-primary-foreground'
            )}
          >
            {part}
          </mark>
        );
      }
      return <span key={index}>{part}</span>;
    });
  }, [jobDescription, detectedSkills, filterKeyword]);

  if (!jobDescription || jobDescription.trim().length === 0) {
    return (
      <div className="flex flex-col items-center justify-center p-12 text-center border border-dashed border-border rounded-xl bg-card">
        <Sparkles className="h-10 w-10 text-muted-foreground/50 mb-3" />
        <h3 className="font-semibold text-sm text-foreground">No Job Description Provided</h3>
        <p className="text-xs text-muted-foreground mt-1 max-w-sm">
          Paste the job description to automatically extract key technical requirements, stack, and qualifications.
        </p>
        {onEditClick && (
          <button
            type="button"
            onClick={onEditClick}
            className="mt-4 px-4 py-1.5 text-xs font-semibold bg-primary text-primary-foreground rounded-lg hover:opacity-90 shadow-sm"
          >
            Add Job Description
          </button>
        )}
      </div>
    );
  }

  return (
    <div className="space-y-4">
      {/* Skill summary bar */}
      <div className="bg-card border border-border rounded-xl p-4 space-y-3">
        <div className="flex items-center justify-between flex-wrap gap-2">
          <div className="flex items-center gap-2">
            <Sparkles className="h-4 w-4 text-primary" />
            <h4 className="text-xs font-bold uppercase tracking-wider text-foreground">
              Detected Skills & Technologies ({detectedSkills.length})
            </h4>
          </div>

          <div className="flex items-center gap-2">
            {/* Quick search/filter within job description */}
            <div className="relative">
              <Search className="h-3.5 w-3.5 absolute left-2.5 top-2 text-muted-foreground" />
              <input
                type="text"
                placeholder="Highlight skill..."
                value={filterKeyword}
                onChange={(e) => setFilterKeyword(e.target.value)}
                className="pl-8 pr-2.5 py-1 text-xs bg-background border border-border rounded-md focus:outline-none focus:ring-1 focus:ring-primary w-36"
              />
            </div>

            <button
              type="button"
              onClick={handleCopy}
              className="flex items-center gap-1.5 px-2.5 py-1 text-xs font-medium text-muted-foreground hover:text-foreground bg-muted/60 hover:bg-muted rounded-md transition-colors"
            >
              {copied ? (
                <>
                  <Check className="h-3.5 w-3.5 text-emerald-500" />
                  <span className="text-emerald-500">Copied!</span>
                </>
              ) : (
                <>
                  <Copy className="h-3.5 w-3.5" />
                  <span>Copy</span>
                </>
              )}
            </button>
          </div>
        </div>

        {detectedSkills.length > 0 ? (
          <div className="flex flex-wrap gap-1.5">
            {detectedSkills.map((skill) => (
              <button
                key={skill}
                type="button"
                onClick={() => setFilterKeyword(filterKeyword === skill ? '' : skill)}
                className={clsx(
                  'px-2.5 py-1 rounded-full text-xs font-medium border transition-colors',
                  filterKeyword.toLowerCase() === skill.toLowerCase()
                    ? 'bg-primary text-primary-foreground border-primary shadow-sm'
                    : 'bg-primary/5 text-primary border-primary/20 hover:bg-primary/10'
                )}
              >
                {skill}
              </button>
            ))}
          </div>
        ) : (
          <p className="text-xs text-muted-foreground italic">
            No standard skills detected in text. You can type in the highlight box above to search any keyword.
          </p>
        )}
      </div>

      {/* Main description text */}
      <div className="bg-card border border-border rounded-xl p-5 shadow-xs">
        <div className="text-xs leading-relaxed text-foreground/90 whitespace-pre-wrap font-sans">
          {highlightedContent}
        </div>
      </div>
    </div>
  );
};
