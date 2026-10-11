import { Link, NavLink, Outlet, useNavigate } from 'react-router-dom';
import { useAuth } from './features/auth/useAuth';

/** Application shell: the header, the navigation, and wherever the router puts the page. */
export function App() {
  const { user, logout } = useAuth();
  const navigate = useNavigate();

  const handleLogout = () => {
    logout();
    navigate('/login', { replace: true });
  };

  return (
    <div className="app">
      <header className="app__header">
        <div className="app__header-left">
          <Link className="app__brand" to="/tickets">
            Northwind Support
          </Link>

          {user && (
            <nav className="app__nav" aria-label="Main">
              <NavLink to="/tickets">Tickets</NavLink>
              <NavLink to="/customers">Customers</NavLink>
            </nav>
          )}
        </div>

        <div className="app__user-nav">
          {user ? (
            <div className="app__user-info">
              <span className="app__user-greeting" aria-label={`Signed in as ${user.fullName}`}>
                {user.fullName}
              </span>
              <button
                type="button"
                className="button button--ghost app__logout-button"
                onClick={handleLogout}
              >
                Log out
              </button>
            </div>
          ) : (
            <Link to="/login" className="button button--ghost">
              Sign in
            </Link>
          )}
        </div>
      </header>

      <main className="app__main">
        <Outlet />
      </main>
    </div>
  );
}

