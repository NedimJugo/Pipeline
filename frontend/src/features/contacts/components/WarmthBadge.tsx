import React from 'react';
import { ContactWarmth } from '../types';
import { Flame, Snowflake, Clock, Zap } from 'lucide-react';
import { clsx } from 'clsx';

interface WarmthBadgeProps {
  warmth: ContactWarmth;
  daysSince?: number | null;
  size?: 'sm' | 'md';
}

export const WarmthBadge: React.FC<WarmthBadgeProps> = ({
  warmth,
  daysSince,
  size = 'md',
}) => {
  const getWarmthConfig = () => {
    switch (warmth) {
      case 'Hot':
        return {
          icon: Flame,
          label: 'Hot',
          subtext: daysSince !== undefined && daysSince !== null ? `${daysSince}d ago` : '<14d',
          style: 'bg-rose-500/10 text-rose-600 dark:text-rose-400 border-rose-500/20',
          iconColor: 'text-rose-500 fill-rose-500/20',
        };
      case 'Warm':
        return {
          icon: Zap,
          label: 'Warm',
          subtext: daysSince !== undefined && daysSince !== null ? `${daysSince}d ago` : '14-30d',
          style: 'bg-amber-500/10 text-amber-600 dark:text-amber-400 border-amber-500/20',
          iconColor: 'text-amber-500 fill-amber-500/20',
        };
      case 'Cooling':
        return {
          icon: Clock,
          label: 'Cooling',
          subtext: daysSince !== undefined && daysSince !== null ? `${daysSince}d ago` : '31-60d',
          style: 'bg-sky-500/10 text-sky-600 dark:text-sky-400 border-sky-500/20',
          iconColor: 'text-sky-500',
        };
      case 'Cold':
      default:
        return {
          icon: Snowflake,
          label: 'Cold',
          subtext: daysSince !== undefined && daysSince !== null ? `${daysSince}d ago` : 'No contact',
          style: 'bg-muted text-muted-foreground border-border',
          iconColor: 'text-muted-foreground',
        };
    }
  };

  const config = getWarmthConfig();
  const Icon = config.icon;

  if (size === 'sm') {
    return (
      <span
        className={clsx(
          'inline-flex items-center gap-1 px-2 py-0.5 rounded-full text-[10px] font-semibold border',
          config.style
        )}
        title={`Relationship Warmth: ${config.label} (${config.subtext})`}
      >
        <Icon className={clsx('h-3 w-3', config.iconColor)} />
        <span>{config.label}</span>
      </span>
    );
  }

  return (
    <div
      className={clsx(
        'inline-flex items-center gap-1.5 px-2.5 py-1 rounded-full text-xs font-semibold border shadow-2xs',
        config.style
      )}
      title={`Relationship Warmth: ${config.label} (${config.subtext})`}
    >
      <Icon className={clsx('h-3.5 w-3.5', config.iconColor)} />
      <span>{config.label}</span>
      <span className="text-[10px] opacity-75 font-normal">({config.subtext})</span>
    </div>
  );
};
