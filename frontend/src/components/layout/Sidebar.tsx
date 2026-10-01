import React from 'react';
import { NavLink } from 'react-router-dom';
import {
  LayoutDashboard,
  KanbanSquare,
  Users,
  CalendarCheck,
  FileText,
  CheckSquare,
  Award,
  Sparkles,
  BarChart3,
  Calendar as CalendarIcon,
  Mail,
  Compass,
  Settings,
  X,
} from 'lucide-react';
import { clsx } from 'clsx';

interface NavItem {
  to: string;
  label: string;
  icon: React.ComponentType<{ className?: string }>;
  badge?: string;
}

export interface SidebarProps {
  isMobileOpen?: boolean;
  onMobileClose?: () => void;
}

const navItems: NavItem[] = [
  { to: '/', label: 'Today', icon: LayoutDashboard },
  { to: '/applications', label: 'Applications', icon: KanbanSquare },
  { to: '/contacts', label: 'Contacts', icon: Users },
  { to: '/interviews', label: 'Interviews', icon: CalendarCheck },
  { to: '/documents', label: 'Documents', icon: FileText },
  { to: '/tasks', label: 'Tasks', icon: CheckSquare },
  { to: '/references', label: 'References', icon: Award },
  { to: '/offers', label: 'Offer Compare', icon: Sparkles },
  { to: '/analytics', label: 'Analytics', icon: BarChart3 },
  { to: '/calendar', label: 'Calendar', icon: CalendarIcon },
  { to: '/templates', label: 'Templates', icon: Mail },
  { to: '/discovery', label: 'Discovery', icon: Compass },
  { to: '/settings', label: 'Settings', icon: Settings },
];

export const Sidebar: React.FC<SidebarProps> = ({ isMobileOpen = false, onMobileClose }) => {
  return (
    <>
      {/* Mobile Backdrop */}
      {isMobileOpen && (
        <div
          data-testid="mobile-drawer-backdrop"
          onClick={onMobileClose}
          className="fixed inset-0 bg-black/60 z-40 backdrop-blur-xs md:hidden"
          aria-label="Close navigation menu"
        />
      )}

      {/* Sidebar / Mobile Drawer */}
      <aside
        className={clsx(
          'border-r border-border bg-card flex flex-col h-screen select-none transition-transform duration-200 ease-in-out',
          // Desktop positioning
          'md:w-64 md:translate-x-0 md:bg-card/60 md:backdrop-blur-md md:sticky md:top-0 md:z-30',
          // Mobile Drawer positioning
          'fixed inset-y-0 left-0 z-50 w-72 md:relative',
          isMobileOpen ? 'translate-x-0 shadow-2xl' : '-translate-x-full md:translate-x-0'
        )}
      >
        <div className="h-16 flex items-center justify-between px-6 border-b border-border/80">
          <div className="flex items-center gap-3">
            <img
              src="/app_icon.png"
              alt="Pipeline Logo"
              className="h-8 w-8 rounded-lg object-contain shadow-md shadow-indigo-500/20"
            />
            <div>
              <span className="font-bold tracking-tight text-base block leading-none">Pipeline</span>
              <span className="text-[11px] text-muted-foreground font-medium">Command Center</span>
            </div>
          </div>

          {/* Close button inside mobile drawer */}
          <button
            type="button"
            onClick={onMobileClose}
            className="md:hidden p-1.5 rounded-lg text-muted-foreground hover:text-foreground hover:bg-muted transition-colors"
            aria-label="Close navigation drawer"
          >
            <X className="h-5 w-5" />
          </button>
        </div>

        <nav className="flex-1 overflow-y-auto py-4 px-3 space-y-1">
          {navItems.map((item) => {
            const Icon = item.icon;
            return (
              <NavLink
                key={item.to}
                to={item.to}
                onClick={() => {
                  if (onMobileClose) {
                    onMobileClose();
                  }
                }}
                className={({ isActive }) =>
                  clsx(
                    'flex items-center gap-3 px-3 py-2 rounded-md text-sm font-medium transition-colors',
                    isActive
                      ? 'bg-primary text-primary-foreground font-semibold shadow-sm'
                      : 'text-muted-foreground hover:text-foreground hover:bg-muted/80'
                  )
                }
              >
                <Icon className="h-4 w-4 shrink-0" />
                <span className="flex-1 truncate">{item.label}</span>
                {item.badge && (
                  <span className="text-[10px] uppercase font-bold tracking-wider px-1.5 py-0.5 rounded bg-muted text-muted-foreground">
                    {item.badge}
                  </span>
                )}
              </NavLink>
            );
          })}
        </nav>
      </aside>
    </>
  );
};
