import React from 'react';
import { Outlet } from 'react-router-dom';
import { Sidebar } from './Sidebar';
import { Header } from './Header';
import { CommandPalette } from '@/components/CommandPalette';
import { OfflineBanner } from '@/components/OfflineBanner';

export const AppLayout: React.FC = () => {
  return (
    <div className="flex min-h-screen bg-background text-foreground">
      <Sidebar />
      <div className="flex-1 flex flex-col min-w-0">
        <OfflineBanner />
        <Header />
        <main className="flex-1 p-6 overflow-y-auto">
          <Outlet />
        </main>
      </div>
      <CommandPalette />
    </div>
  );
};
