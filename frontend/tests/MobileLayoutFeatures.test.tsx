import { describe, it, expect, vi } from 'vitest';
import { render, screen, fireEvent } from '@testing-library/react';
import React from 'react';
import { BrowserRouter } from 'react-router-dom';
import { Sidebar } from '@/components/layout/Sidebar';
import { Header } from '@/components/layout/Header';

vi.mock('@/features/auth/AuthContext', () => ({
  useAuth: () => ({
    user: {
      id: 'user-1',
      email: 'alex@example.com',
      displayName: 'Alex Rivers',
      targetRole: 'Staff Platform Engineer',
      onboardingCompleted: true,
    },
    logout: vi.fn(),
    updateUser: vi.fn(),
  }),
}));

describe('MobileLayoutFeatures', () => {
  it('Header renders hamburger button that triggers onToggleMobileMenu', () => {
    const onToggle = vi.fn();
    render(<Header onToggleMobileMenu={onToggle} />);

    const menuButton = screen.getByRole('button', { name: /toggle navigation menu/i });
    expect(menuButton).toBeInTheDocument();

    fireEvent.click(menuButton);
    expect(onToggle).toHaveBeenCalledTimes(1);
  });

  it('Sidebar renders backdrop and drawer when isMobileOpen is true, and calls onMobileClose on backdrop click', () => {
    const onClose = vi.fn();
    render(
      <BrowserRouter>
        <Sidebar isMobileOpen={true} onMobileClose={onClose} />
      </BrowserRouter>
    );

    const backdrop = screen.getByTestId('mobile-drawer-backdrop');
    expect(backdrop).toBeInTheDocument();

    fireEvent.click(backdrop);
    expect(onClose).toHaveBeenCalledTimes(1);
  });

  it('Sidebar calls onMobileClose when a navigation link is clicked in mobile mode', () => {
    const onClose = vi.fn();
    render(
      <BrowserRouter>
        <Sidebar isMobileOpen={true} onMobileClose={onClose} />
      </BrowserRouter>
    );

    const applicationsLink = screen.getByRole('link', { name: /applications/i });
    expect(applicationsLink).toBeInTheDocument();

    fireEvent.click(applicationsLink);
    expect(onClose).toHaveBeenCalled();
  });
});
