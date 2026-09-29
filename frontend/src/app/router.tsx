import React from 'react';
import { createBrowserRouter, Navigate, Outlet } from 'react-router-dom';
import { useAuth } from '@/features/auth/AuthContext';
import { AppLayout } from '@/components/layout/AppLayout';
import { LoginPage } from '@/features/auth/LoginPage';
import { RegisterPage } from '@/features/auth/RegisterPage';
import { DashboardPage } from '@/features/dashboard/DashboardPage';
import { ApplicationsPage } from '@/features/applications/ApplicationsPage';
import {
  ContactsPage,
  InterviewsPage,
  DocumentsPage,
  TasksPage,
  ReferencesPage,
  OffersPage,
  AnalyticsPage,
  CalendarPage,
  TemplatesPage,
  DiscoveryPage,
  SettingsPage,
} from '@/features/placeholders';

const ProtectedRoute: React.FC = () => {
  const { isAuthenticated, isLoading } = useAuth();

  if (isLoading) {
    return (
      <div className="h-screen w-screen flex items-center justify-center bg-background">
        <div className="flex flex-col items-center gap-3">
          <div className="h-8 w-8 rounded-lg bg-indigo-600 text-white font-bold flex items-center justify-center animate-pulse">
            P
          </div>
          <span className="text-xs text-muted-foreground font-medium">Loading command center...</span>
        </div>
      </div>
    );
  }

  if (!isAuthenticated) {
    return <Navigate to="/login" replace />;
  }

  return <Outlet />;
};

const PublicOnlyRoute: React.FC = () => {
  const { isAuthenticated, isLoading } = useAuth();

  if (isLoading) {
    return null;
  }

  if (isAuthenticated) {
    return <Navigate to="/" replace />;
  }

  return <Outlet />;
};

export const router = createBrowserRouter([
  // Public routes
  {
    element: <PublicOnlyRoute />,
    children: [
      { path: '/login', element: <LoginPage /> },
      { path: '/register', element: <RegisterPage /> },
    ],
  },
  // Protected routes
  {
    element: <ProtectedRoute />,
    children: [
      {
        element: <AppLayout />,
        children: [
          { path: '/', element: <DashboardPage /> },
          { path: '/applications', element: <ApplicationsPage /> },
          { path: '/contacts', element: <ContactsPage /> },
          { path: '/interviews', element: <InterviewsPage /> },
          { path: '/documents', element: <DocumentsPage /> },
          { path: '/tasks', element: <TasksPage /> },
          { path: '/references', element: <ReferencesPage /> },
          { path: '/offers', element: <OffersPage /> },
          { path: '/analytics', element: <AnalyticsPage /> },
          { path: '/calendar', element: <CalendarPage /> },
          { path: '/templates', element: <TemplatesPage /> },
          { path: '/discovery', element: <DiscoveryPage /> },
          { path: '/settings', element: <SettingsPage /> },
        ],
      },
    ],
  },
  { path: '*', element: <Navigate to="/" replace /> },
]);
