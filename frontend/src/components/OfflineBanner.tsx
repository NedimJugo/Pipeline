import React, { useState, useEffect } from 'react';
import { WifiOff, RefreshCw } from 'lucide-react';

export const OfflineBanner: React.FC = () => {
  const [isOffline, setIsOffline] = useState(!navigator.onLine);

  useEffect(() => {
    const handleOnline = () => setIsOffline(false);
    const handleOffline = () => setIsOffline(true);

    window.addEventListener('online', handleOnline);
    window.addEventListener('offline', handleOffline);

    return () => {
      window.removeEventListener('online', handleOnline);
      window.removeEventListener('offline', handleOffline);
    };
  }, []);

  if (!isOffline) return null;

  return (
    <div
      role="status"
      aria-live="polite"
      className="bg-amber-600 text-white px-4 py-2 text-xs font-semibold flex items-center justify-between shadow-md sticky top-0 z-50 animate-in slide-in-from-top duration-200"
    >
      <div className="flex items-center gap-2 max-w-4xl mx-auto flex-1">
        <WifiOff className="h-4 w-4 shrink-0" />
        <span>
          You are currently working offline. Cached pipeline views remain accessible in read-only mode.
        </span>
      </div>
      <button
        onClick={() => window.location.reload()}
        className="flex items-center gap-1 bg-white/20 hover:bg-white/30 text-white px-2 py-0.5 rounded text-[11px] transition-colors"
      >
        <RefreshCw className="h-3 w-3" />
        <span>Reconnect</span>
      </button>
    </div>
  );
};
