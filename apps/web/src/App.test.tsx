import { render, screen } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import { MemoryRouter, Route, Routes } from 'react-router-dom';
import { beforeEach, describe, expect, it, vi } from 'vitest';
import { App } from './App';
import * as authContext from './features/auth/useAuth';

describe('App Shell', () => {
  beforeEach(() => {
    vi.clearAllMocks();
  });

  it('renders login link when visitor is unauthenticated', () => {
    vi.spyOn(authContext, 'useAuth').mockReturnValue({
      user: null,
      token: null,
      isLoading: false,
      login: vi.fn(),
      logout: vi.fn(),
    });

    render(
      <MemoryRouter>
        <App />
      </MemoryRouter>,
    );

    expect(screen.getByRole('link', { name: /sign in/i })).toHaveAttribute('href', '/login');
    expect(screen.queryByText(/log out/i)).not.toBeInTheDocument();
  });

  it('displays authenticated agent name and handles logout action', async () => {
    const user = userEvent.setup();
    const mockLogout = vi.fn();

    vi.spyOn(authContext, 'useAuth').mockReturnValue({
      user: { id: 1, fullName: 'Alex Turner', email: 'alex@northwind.com' },
      token: 'jwt.token',
      isLoading: false,
      login: vi.fn(),
      logout: mockLogout,
    });

    render(
      <MemoryRouter initialEntries={['/tickets']}>
        <Routes>
          <Route path="/" element={<App />}>
            <Route path="tickets" element={<div>Tickets</div>} />
          </Route>
          <Route path="/login" element={<div>Login Page</div>} />
        </Routes>
      </MemoryRouter>,
    );

    expect(screen.getByText('Alex Turner')).toBeInTheDocument();
    const logoutBtn = screen.getByRole('button', { name: /log out/i });
    expect(logoutBtn).toBeInTheDocument();

    await user.click(logoutBtn);

    expect(mockLogout).toHaveBeenCalledTimes(1);
    expect(screen.getByText('Login Page')).toBeInTheDocument();
  });
});
