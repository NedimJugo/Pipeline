import React from 'react';
import { Sun, Moon, Search, LogOut, HelpCircle, Menu } from 'lucide-react';
import { useAuth } from '@/features/auth/AuthContext';
import { useTheme } from '@/lib/theme';

export interface HeaderProps {
  onToggleMobileMenu?: () => void;
}

export const Header: React.FC<HeaderProps> = ({ onToggleMobileMenu }) => {
  const { user, logout } = useAuth();
  const { theme, setTheme } = useTheme();

  const toggleTheme = () => {
    setTheme(theme === 'dark' ? 'light' : 'dark');
  };

  const initials = user?.displayName
    ? user.displayName.split(' ').map((n) => n[0]).join('').toUpperCase().slice(0, 2)
    : user?.email ? user.email.slice(0, 2).toUpperCase() : 'U';

  return (
    <header className="h-16 border-b border-border/80 bg-card/40 backdrop-blur-md px-3 sm:px-6 flex items-center justify-between sticky top-0 z-30">
      <div className="flex items-center gap-2 sm:gap-3">
        {/* Mobile Hamburger Menu Toggle */}
        <button
          type="button"
          onClick={onToggleMobileMenu}
          className="md:hidden p-2 rounded-lg border border-border text-muted-foreground hover:text-foreground hover:bg-muted transition-colors"
          aria-label="Toggle navigation menu"
          title="Toggle navigation menu"
        >
          <Menu className="h-4 w-4" />
        </button>

        <button
          type="button"
          className="flex items-center gap-2 px-2.5 sm:px-3 py-1.5 rounded-lg border border-border bg-background/50 hover:bg-muted text-muted-foreground hover:text-foreground text-xs transition-colors"
          onClick={() => window.dispatchEvent(new CustomEvent('pipeline_open_command_palette'))}
        >
          <Search className="h-3.5 w-3.5" />
          <span className="hidden sm:inline">Quick search applications, contacts...</span>
          <span className="sm:hidden">Search...</span>
          <kbd className="ml-2 hidden sm:inline-block font-mono text-[10px] bg-muted px-1.5 py-0.5 rounded border border-border">
            Ctrl+K
          </kbd>
        </button>
      </div>

      <div className="flex items-center gap-2 sm:gap-3">
        <button
          type="button"
          onClick={() => window.dispatchEvent(new CustomEvent('pipeline_open_app_tour'))}
          className="p-2 rounded-lg border border-border text-muted-foreground hover:text-foreground hover:bg-muted transition-colors flex items-center gap-1.5 text-xs font-medium"
          title="Take App Tour"
          aria-label="Take App Tour"
        >
          <HelpCircle className="h-4 w-4 text-indigo-500" />
          <span className="hidden md:inline">Tour</span>
        </button>

        <button
          type="button"
          onClick={toggleTheme}
          className="p-2 rounded-lg border border-border text-muted-foreground hover:text-foreground hover:bg-muted transition-colors"
          title={`Switch to ${theme === 'dark' ? 'light' : 'dark'} mode`}
          aria-label="Toggle color theme"
        >
          {theme === 'dark' ? <Sun className="h-4 w-4" /> : <Moon className="h-4 w-4" />}
        </button>

        <div className="flex items-center gap-2 sm:gap-3 pl-2 border-l border-border">
          <div className="h-8 w-8 rounded-full bg-primary/20 text-primary flex items-center justify-center font-semibold text-xs border border-primary/30">
            {initials}
          </div>
          <div className="hidden sm:block text-left text-xs">
            <div className="font-semibold leading-tight">{user?.displayName || user?.email}</div>
            <div className="text-muted-foreground leading-tight text-[11px] truncate max-w-[140px]">
              {user?.targetRole || 'Job Seeker'}
            </div>
          </div>
          <button
            type="button"
            onClick={() => logout()}
            className="p-2 rounded-lg text-muted-foreground hover:text-destructive hover:bg-destructive/10 transition-colors"
            title="Log out"
            aria-label="Log out"
          >
            <LogOut className="h-4 w-4" />
          </button>
        </div>
      </div>
    </header>
  );
};
