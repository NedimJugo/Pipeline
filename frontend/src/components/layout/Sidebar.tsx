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
} from 'lucide-react';
import { clsx } from 'clsx';

interface NavItem {
  to: string;
  label: string;
  icon: React.ComponentType<{ className?: string }>;
  badge?: string;
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

export const Sidebar: React.FC = () => {
  return (
    <aside className="w-64 border-r border-border bg-card/60 backdrop-blur-md flex flex-col h-screen sticky top-0 select-none">
      <div className="h-16 flex items-center px-6 border-b border-border/80 gap-3">
        <div className="h-8 w-8 rounded-lg bg-indigo-600 flex items-center justify-center text-white font-bold shadow-md shadow-indigo-500/20">
          P
        </div>
        <div>
          <span className="font-bold tracking-tight text-base block leading-none">Pipeline</span>
          <span className="text-[11px] text-muted-foreground font-medium">Command Center</span>
        </div>
      </div>

      <nav className="flex-1 overflow-y-auto py-4 px-3 space-y-1">
        {navItems.map((item) => {
          const Icon = item.icon;
          return (
            <NavLink
              key={item.to}
              to={item.to}
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
  );
};
