import React, { useState, useEffect } from 'react';
import { Outlet } from 'react-router-dom';
import { Sidebar } from './Sidebar';
import { Header } from './Header';
import { CommandPalette } from '@/components/CommandPalette';
import { OfflineBanner } from '@/components/OfflineBanner';
import { useAuth } from '@/features/auth/AuthContext';
import { OnboardingWizardModal } from '@/features/onboarding/OnboardingWizardModal';
import { AppTourModal } from '@/features/onboarding/AppTourModal';

export const AppLayout: React.FC = () => {
  const { user, updateUser } = useAuth();
  const [isTourOpen, setIsTourOpen] = useState(false);
  const [isMobileNavOpen, setIsMobileNavOpen] = useState(false);

  useEffect(() => {
    const handleOpenTour = () => setIsTourOpen(true);
    window.addEventListener('pipeline_open_app_tour', handleOpenTour);
    return () => window.removeEventListener('pipeline_open_app_tour', handleOpenTour);
  }, []);

  return (
    <div className="flex min-h-screen bg-background text-foreground">
      <Sidebar
        isMobileOpen={isMobileNavOpen}
        onMobileClose={() => setIsMobileNavOpen(false)}
      />
      <div className="flex-1 flex flex-col min-w-0">
        <OfflineBanner />
        <Header onToggleMobileMenu={() => setIsMobileNavOpen((prev) => !prev)} />
        <main className="flex-1 p-4 sm:p-6 overflow-y-auto">
          <Outlet />
        </main>
      </div>
      <CommandPalette />

      {/* Onboarding Wizard for new registrations */}
      {user && !user.onboardingCompleted && (
        <OnboardingWizardModal
          isOpen={true}
          onComplete={() => updateUser({ onboardingCompleted: true })}
        />
      )}

      {/* Standalone App Tour replayable anytime from header */}
      <AppTourModal isOpen={isTourOpen} onClose={() => setIsTourOpen(false)} />
    </div>
  );
};
