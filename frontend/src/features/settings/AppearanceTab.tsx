import React, { useState, useEffect } from 'react';
import { Sun, Moon, Laptop, Smartphone, Download, CheckCircle2, ShieldCheck, WifiOff } from 'lucide-react';
import { useTheme } from '@/lib/theme';

export const AppearanceTab: React.FC = () => {
  const { theme, setTheme } = useTheme();
  const [deferredPrompt, setDeferredPrompt] = useState<any>(null);
  const [isInstalled, setIsInstalled] = useState(false);

  useEffect(() => {
    // Check if app is running as installed PWA
    if (window.matchMedia('(display-mode: standalone)').matches || (window.navigator as any).standalone) {
      setIsInstalled(true);
    }

    const handleBeforeInstall = (e: Event) => {
      e.preventDefault();
      setDeferredPrompt(e);
    };

    window.addEventListener('beforeinstallprompt', handleBeforeInstall);
    return () => window.removeEventListener('beforeinstallprompt', handleBeforeInstall);
  }, []);

  const handleInstallClick = async () => {
    if (!deferredPrompt) {
      alert('To install Pipeline as an app, use your browser menu (e.g. Chrome Settings -> Install Pipeline or Safari Share -> Add to Home Screen).');
      return;
    }
    deferredPrompt.prompt();
    const { outcome } = await deferredPrompt.userChoice;
    if (outcome === 'accepted') {
      setIsInstalled(true);
    }
    setDeferredPrompt(null);
  };

  return (
    <div className="space-y-6">
      {/* Theme Selection */}
      <div className="bg-card border border-border rounded-xl p-6 shadow-xs space-y-4">
        <div>
          <h2 className="text-base font-bold tracking-tight">Theme & Appearance</h2>
          <p className="text-xs text-muted-foreground mt-0.5">
            Select your preferred visual theme for the command center
          </p>
        </div>

        <div className="grid grid-cols-2 sm:grid-cols-3 gap-3 pt-2">
          <button
            type="button"
            onClick={() => setTheme('light')}
            className={`p-4 rounded-xl border text-center transition-all flex flex-col items-center gap-2.5 ${
              theme === 'light'
                ? 'border-primary bg-primary/10 text-primary font-bold shadow-2xs'
                : 'border-border hover:bg-muted text-foreground font-medium'
            }`}
          >
            <Sun className="h-5 w-5" />
            <div className="text-xs">Light Theme</div>
          </button>

          <button
            type="button"
            onClick={() => setTheme('dark')}
            className={`p-4 rounded-xl border text-center transition-all flex flex-col items-center gap-2.5 ${
              theme === 'dark'
                ? 'border-primary bg-primary/10 text-primary font-bold shadow-2xs'
                : 'border-border hover:bg-muted text-foreground font-medium'
            }`}
          >
            <Moon className="h-5 w-5" />
            <div className="text-xs">Dark Theme</div>
          </button>
        </div>
      </div>

      {/* PWA & Offline Support */}
      <div className="bg-card border border-border rounded-xl p-6 shadow-xs space-y-4">
        <div className="flex items-center gap-3">
          <img
            src="/app_icon.png"
            alt="Pipeline App"
            className="h-10 w-10 rounded-xl shadow-xs object-contain border border-border"
          />
          <div>
            <h2 className="text-base font-bold tracking-tight">Progressive Web App (PWA) & Offline Mode</h2>
            <p className="text-xs text-muted-foreground mt-0.5">
              Install Pipeline to your desktop or mobile home screen with offline resilience
            </p>
          </div>
        </div>

        <div className="grid grid-cols-1 sm:grid-cols-2 gap-4 pt-2">
          <div className="p-4 rounded-xl border border-border bg-muted/20 space-y-2">
            <div className="text-xs font-bold text-foreground flex items-center gap-1.5">
              <Download className="h-4 w-4 text-primary" />
              <span>App Installation</span>
            </div>
            <p className="text-xs text-muted-foreground">
              {isInstalled
                ? 'Pipeline is currently installed and running as a standalone desktop/mobile app.'
                : 'Install Pipeline for instant windowed launch, keyboard shortcuts, and native desktop integration.'}
            </p>
            {!isInstalled && (
              <button
                type="button"
                onClick={handleInstallClick}
                className="mt-3 inline-flex items-center gap-2 px-4 py-2 bg-primary text-primary-foreground text-xs font-bold rounded-lg hover:bg-primary/90 transition-colors shadow-2xs"
              >
                <Download className="h-4 w-4" />
                <span>Install Pipeline App</span>
              </button>
            )}
          </div>

          <div className="p-4 rounded-xl border border-border bg-muted/20 space-y-2">
            <div className="text-xs font-bold text-foreground flex items-center gap-1.5">
              <WifiOff className="h-4 w-4 text-emerald-500" />
              <span>Offline Resilience</span>
            </div>
            <p className="text-xs text-muted-foreground">
              Core application scripts and stylesheets are automatically cached via the service worker. If network connectivity drops, your views remain accessible in read-only mode with active sync notifications.
            </p>
          </div>
        </div>
      </div>
    </div>
  );
};
