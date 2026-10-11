import { createContext } from 'react';
import type { CurrentAgent, LoginCredentials, LoginResponse } from '../../types/api';

export interface AuthContextValue {
  user: CurrentAgent | null;
  token: string | null;
  isLoading: boolean;
  login: (credentials: LoginCredentials) => Promise<LoginResponse>;
  logout: () => void;
}

export const AuthContext = createContext<AuthContextValue | undefined>(undefined);
