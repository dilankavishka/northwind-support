import type { CurrentAgent, LoginCredentials, LoginResponse } from '../types/api';
import { request } from './client';

export const authApi = {
  login: (credentials: LoginCredentials) =>
    request<LoginResponse>('/auth/login', {
      method: 'POST',
      body: JSON.stringify(credentials),
    }),

  getMe: () => request<CurrentAgent>('/auth/me'),
};
