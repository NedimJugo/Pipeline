import React, { createContext, useContext, useEffect, useState } from 'react';
import { api } from '@/lib/api-client';

export interface UserProfile {
  id: string;
  email: string;
  displayName: string | null;
  targetRole: string | null;
  onboardingCompleted: boolean;
}

interface AuthContextType {
  user: UserProfile | null;
  token: string | null;
  isLoading: boolean;
  isAuthenticated: boolean;
  login: (email: string, password: string) => Promise<void>;
  register: (email: string, password: string, displayName?: string) => Promise<void>;
  logout: () => Promise<void>;
  updateUser: (updates: Partial<UserProfile>) => void;
}

const AuthContext = createContext<AuthContextType | undefined>(undefined);

export const AuthProvider: React.FC<{ children: React.ReactNode }> = ({ children }) => {
  const [user, setUser] = useState<UserProfile | null>(() => {
    const saved = localStorage.getItem('pipeline_user');
    return saved ? JSON.parse(saved) : null;
  });
  const [token, setToken] = useState<string | null>(() => localStorage.getItem('pipeline_token'));
  const [isLoading, setIsLoading] = useState<boolean>(true);

  useEffect(() => {
    const initAuth = async () => {
      const storedToken = localStorage.getItem('pipeline_token');
      if (storedToken) {
        try {
          const res = await api.get('/api/auth/me');
          setUser(res.data);
          localStorage.setItem('pipeline_user', JSON.stringify(res.data));
        } catch {
          setUser(null);
          setToken(null);
          localStorage.removeItem('pipeline_token');
          localStorage.removeItem('pipeline_user');
        }
      }
      setIsLoading(false);
    };

    initAuth();

    const handleForceLogout = () => {
      setUser(null);
      setToken(null);
    };

    window.addEventListener('pipeline_auth_logout', handleForceLogout);
    return () => window.removeEventListener('pipeline_auth_logout', handleForceLogout);
  }, []);

  const login = async (email: string, password: string) => {
    const res = await api.post('/api/auth/login', { email, password });
    const { accessToken, user: userData } = res.data;
    localStorage.setItem('pipeline_token', accessToken);
    localStorage.setItem('pipeline_user', JSON.stringify(userData));
    setToken(accessToken);
    setUser(userData);
  };

  const register = async (email: string, password: string, displayName?: string) => {
    const res = await api.post('/api/auth/register', { email, password, displayName });
    const { accessToken, user: userData } = res.data;
    localStorage.setItem('pipeline_token', accessToken);
    localStorage.setItem('pipeline_user', JSON.stringify(userData));
    setToken(accessToken);
    setUser(userData);
  };

  const logout = async () => {
    try {
      await api.post('/api/auth/logout');
    } catch {
      // Ignore network errors on logout
    } finally {
      localStorage.removeItem('pipeline_token');
      localStorage.removeItem('pipeline_user');
      setUser(null);
      setToken(null);
    }
  };

  const updateUser = (updates: Partial<UserProfile>) => {
    setUser((prev) => {
      if (!prev) return null;
      const updated = { ...prev, ...updates };
      localStorage.setItem('pipeline_user', JSON.stringify(updated));
      return updated;
    });
  };

  return (
    <AuthContext.Provider
      value={{
        user,
        token,
        isLoading,
        isAuthenticated: !!user && !!token,
        login,
        register,
        logout,
        updateUser,
      }}
    >
      {children}
    </AuthContext.Provider>
  );
};

export const useAuth = () => {
  const context = useContext(AuthContext);
  if (!context) {
    throw new Error('useAuth must be used within an AuthProvider');
  }
  return context;
};
