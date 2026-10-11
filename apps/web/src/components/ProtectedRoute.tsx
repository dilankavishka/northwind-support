import { Navigate, Outlet, useLocation } from 'react-router-dom';
import { useAuth } from '../features/auth/useAuth';
import { Spinner } from './Spinner';

interface ProtectedRouteProps {
  children?: React.ReactNode;
}

/**
 * Ensures the route can only be accessed by an authenticated agent.
 * Redirects unauthenticated visitors to /login with a return URL preserved in query param.
 */
export function ProtectedRoute({ children }: ProtectedRouteProps) {
  const { user, isLoading } = useAuth();
  const location = useLocation();

  if (isLoading) {
    return (
      <div className="protected-route__loading" style={{ padding: '3rem', textAlign: 'center' }}>
        <Spinner label="Checking session" />
      </div>
    );
  }

  if (!user) {
    const returnPath = encodeURIComponent(`${location.pathname}${location.search}`);
    return <Navigate to={`/login?redirect=${returnPath}`} replace />;
  }

  return children ? <>{children}</> : <Outlet />;
}
