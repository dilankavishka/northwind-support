import { act, render, screen, waitFor } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import { MemoryRouter, Route, Routes } from 'react-router-dom';
import { beforeEach, describe, expect, it, vi } from 'vitest';
import { ApiError } from '../../api/client';
import * as authContext from './useAuth';
import { LoginPage } from './LoginPage';

describe('LoginPage', () => {
  const mockLogin = vi.fn();

  beforeEach(() => {
    vi.clearAllMocks();
    vi.spyOn(authContext, 'useAuth').mockReturnValue({
      user: null,
      token: null,
      isLoading: false,
      login: mockLogin,
      logout: vi.fn(),
    });
  });

  it('renders accessible, properly labelled inputs for email and password', () => {
    render(
      <MemoryRouter>
        <LoginPage />
      </MemoryRouter>,
    );

    expect(screen.getByRole('heading', { name: /sign in/i })).toBeInTheDocument();
    expect(screen.getByLabelText(/email address/i)).toBeInTheDocument();
    expect(screen.getByLabelText(/password/i)).toBeInTheDocument();
    expect(screen.getByRole('button', { name: /^sign in$/i })).toBeInTheDocument();
  });

  it('validates client-side and blocks submit when inputs are empty or invalid', async () => {
    const user = userEvent.setup();

    render(
      <MemoryRouter>
        <LoginPage />
      </MemoryRouter>,
    );

    const submitButton = screen.getByRole('button', { name: /^sign in$/i });
    await user.click(submitButton);

    expect(screen.getByText(/email address is required/i)).toBeInTheDocument();
    expect(screen.getByText(/password is required/i)).toBeInTheDocument();
    expect(mockLogin).not.toHaveBeenCalled();

    // Type invalid email format
    const emailInput = screen.getByLabelText(/email address/i);
    await user.type(emailInput, 'not-an-email');
    await user.click(submitButton);

    expect(screen.getByText(/please enter a valid email address/i)).toBeInTheDocument();
    expect(mockLogin).not.toHaveBeenCalled();
  });

  it('displays clear error message when credentials are rejected', async () => {
    const user = userEvent.setup();
    mockLogin.mockRejectedValueOnce(
      new ApiError(401, 'Invalid email or password.'),
    );

    render(
      <MemoryRouter>
        <LoginPage />
      </MemoryRouter>,
    );

    await user.type(screen.getByLabelText(/email address/i), 'alex@northwind.com');
    await user.type(screen.getByLabelText(/password/i), 'WrongPass');
    await user.click(screen.getByRole('button', { name: /^sign in$/i }));

    expect(mockLogin).toHaveBeenCalledWith({
      email: 'alex@northwind.com',
      password: 'WrongPass',
    });

    await waitFor(() => {
      expect(screen.getByRole('alert')).toHaveTextContent(/invalid email or password/i);
    });
  });

  it('displays pending state while sign in is in flight', async () => {
    const user = userEvent.setup();
    let resolveLogin: () => void = () => {};
    mockLogin.mockReturnValueOnce(
      new Promise<void>((resolve) => {
        resolveLogin = resolve;
      }),
    );

    render(
      <MemoryRouter>
        <LoginPage />
      </MemoryRouter>,
    );

    await user.type(screen.getByLabelText(/email address/i), 'alex@northwind.com');
    await user.type(screen.getByLabelText(/password/i), 'Admin123');
    await user.click(screen.getByRole('button', { name: /^sign in$/i }));

    expect(screen.getByRole('button', { name: /signing in/i })).toBeDisabled();
    expect(screen.getByLabelText(/email address/i)).toBeDisabled();
    expect(screen.getByLabelText(/password/i)).toBeDisabled();

    act(() => {
      resolveLogin();
    });
    await waitFor(() => {
      expect(screen.getByRole('button', { name: /^sign in$/i })).not.toBeDisabled();
    });
  });

  it('redirects to return URL destination upon successful sign in', async () => {
    const user = userEvent.setup();
    mockLogin.mockResolvedValueOnce({
      token: 'jwt.token',
      expiresAtUtc: '2026-10-12T00:00:00Z',
      agent: { id: 1, fullName: 'Alex Turner', email: 'alex@northwind.com' },
    });

    render(
      <MemoryRouter initialEntries={['/login?redirect=%2Fcustomers%2F5']}>
        <Routes>
          <Route path="/login" element={<LoginPage />} />
          <Route path="/customers/5" element={<div>Customer 5 Page</div>} />
        </Routes>
      </MemoryRouter>,
    );

    await user.type(screen.getByLabelText(/email address/i), 'alex@northwind.com');
    await user.type(screen.getByLabelText(/password/i), 'Admin123');
    await user.click(screen.getByRole('button', { name: /^sign in$/i }));

    await waitFor(() => {
      expect(screen.getByText('Customer 5 Page')).toBeInTheDocument();
    });
  });
});
