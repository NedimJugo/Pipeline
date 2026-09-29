import { create } from 'zustand';

export type Theme = 'light' | 'dark' | 'system';

interface ThemeState {
  theme: Theme;
  setTheme: (theme: Theme) => void;
}

export const useTheme = create<ThemeState>((set) => ({
  theme: (localStorage.getItem('pipeline_theme') as Theme) || 'dark',
  setTheme: (theme: Theme) => {
    localStorage.setItem('pipeline_theme', theme);
    set({ theme });
    applyTheme(theme);
  },
}));

export const applyTheme = (theme: Theme) => {
  const root = document.documentElement;
  const systemPrefersDark = window.matchMedia('(prefers-color-scheme: dark)').matches;

  if (theme === 'dark' || (theme === 'system' && systemPrefersDark)) {
    root.classList.add('dark');
  } else {
    root.classList.remove('dark');
  }
};
