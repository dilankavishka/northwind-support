import { useEffect, useState, type ReactNode } from 'react';
import { authApi } from '../../api/auth';
import { getAuthToken, onUnauthorized, setAuthToken } from '../../api/client';
import type { CurrentAgent, LoginCredentials, LoginResponse } from '../../types/api';
import { AuthContext } from './authContextDef';

export function AuthProvider({ children }: { children: ReactNode }) {
  const [token, setTokenState] = useState<string | null>(() => getAuthToken());
  const [user, setUser] = useState<CurrentAgent | null>(null);
  const [isLoading, setIsLoading] = useState<boolean>(true);

  useEffect(() => {
    let isMounted = true;

    async function initAuth() {
      const initialToken = getAuthToken();
      if (!initialToken) {
        if (isMounted) {
          setIsLoading(false);
        }
        return;
      }

      try {
        const currentUser = await authApi.getMe();
        if (isMounted) {
          setUser(currentUser);
          setTokenState(initialToken);
        }
      } catch {
        if (isMounted) {
          setAuthToken(null);
          setTokenState(null);
          setUser(null);
        }
      } finally {
        if (isMounted) {
          setIsLoading(false);
        }
      }
    }

    initAuth();

    const unsubscribe = onUnauthorized(() => {
      if (isMounted) {
        setUser(null);
        setTokenState(null);
      }
    });

    return () => {
      isMounted = false;
      unsubscribe();
    };
  }, []);

  const login = async (credentials: LoginCredentials): Promise<LoginResponse> => {
    const response = await authApi.login(credentials);
    setAuthToken(response.token);
    setTokenState(response.token);
    setUser(response.agent);
    return response;
  };

  const logout = () => {
    setAuthToken(null);
    setTokenState(null);
    setUser(null);
  };

  return (
    <AuthContext.Provider
      value={{
        user,
        token,
        isLoading,
        login,
        logout,
      }}
    >
      {children}
    </AuthContext.Provider>
  );
}
