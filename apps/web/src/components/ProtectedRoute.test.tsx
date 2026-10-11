import { render, screen } from '@testing-library/react';
import { MemoryRouter, Route, Routes } from 'react-router-dom';
import { describe, expect, it, vi } from 'vitest';
import * as authContext from '../features/auth/useAuth';
import { ProtectedRoute } from './ProtectedRoute';

describe('ProtectedRoute', () => {
  it('renders loading spinner while authentication state is resolving', () => {
    vi.spyOn(authContext, 'useAuth').mockReturnValue({
      user: null,
      token: null,
      isLoading: true,
      login: vi.fn(),
      logout: vi.fn(),
    });

    render(
      <MemoryRouter initialEntries={['/tickets']}>
        <Routes>
          <Route element={<ProtectedRoute />}>
            <Route path="/tickets" element={<div>Protected Tickets</div>} />
          </Route>
        </Routes>
      </MemoryRouter>,
    );

    expect(screen.getByText(/Checking session/i)).toBeInTheDocument();
    expect(screen.queryByText('Protected Tickets')).not.toBeInTheDocument();
  });

  it('redirects unauthenticated visitor to /login with redirect URL encoded', () => {
    vi.spyOn(authContext, 'useAuth').mockReturnValue({
      user: null,
      token: null,
      isLoading: false,
      login: vi.fn(),
      logout: vi.fn(),
    });

    render(
      <MemoryRouter initialEntries={['/tickets?page=2']}>
        <Routes>
          <Route path="/login" element={<div>Login Page</div>} />
          <Route element={<ProtectedRoute />}>
            <Route path="/tickets" element={<div>Protected Content</div>} />
          </Route>
        </Routes>
      </MemoryRouter>,
    );

    expect(screen.getByText('Login Page')).toBeInTheDocument();
    expect(screen.queryByText('Protected Content')).not.toBeInTheDocument();
  });

  it('renders protected child component when user is authenticated', () => {
    vi.spyOn(authContext, 'useAuth').mockReturnValue({
      user: { id: 1, fullName: 'Alex Turner', email: 'alex@northwind.com' },
      token: 'valid.token',
      isLoading: false,
      login: vi.fn(),
      logout: vi.fn(),
    });

    render(
      <MemoryRouter initialEntries={['/tickets']}>
        <Routes>
          <Route element={<ProtectedRoute />}>
            <Route path="/tickets" element={<div>Protected Tickets Content</div>} />
          </Route>
        </Routes>
      </MemoryRouter>,
    );

    expect(screen.getByText('Protected Tickets Content')).toBeInTheDocument();
  });
});
