import { useState, type FormEvent } from 'react';
import { Navigate, useNavigate, useSearchParams } from 'react-router-dom';
import { toErrorMessage } from '../../api/client';
import { ErrorMessage } from '../../components/ErrorMessage';
import { Field } from '../../components/Field';
import { useAuth } from './useAuth';

export function LoginPage() {
  const { user, isLoading, login } = useAuth();
  const navigate = useNavigate();
  const [searchParams] = useSearchParams();

  const [email, setEmail] = useState('');
  const [password, setPassword] = useState('');
  const [fieldErrors, setFieldErrors] = useState<{ email?: string; password?: string }>({});
  const [submitError, setSubmitError] = useState<string | null>(null);
  const [isSubmitting, setIsSubmitting] = useState(false);

  const redirectUrl = searchParams.get('redirect') || '/tickets';

  if (!isLoading && user) {
    return <Navigate to={redirectUrl} replace />;
  }

  const validate = (): boolean => {
    const errors: { email?: string; password?: string } = {};

    const trimmedEmail = email.trim();
    if (!trimmedEmail) {
      errors.email = 'Email address is required.';
    } else if (!/^[^\s@]+@[^\s@]+\.[^\s@]+$/.test(trimmedEmail)) {
      errors.email = 'Please enter a valid email address.';
    }

    if (!password) {
      errors.password = 'Password is required.';
    }

    setFieldErrors(errors);
    return Object.keys(errors).length === 0;
  };

  const handleSubmit = async (e: FormEvent<HTMLFormElement>) => {
    e.preventDefault();
    setSubmitError(null);

    if (!validate()) {
      return;
    }

    setIsSubmitting(true);
    try {
      await login({
        email: email.trim(),
        password,
      });
      navigate(redirectUrl, { replace: true });
    } catch (err: unknown) {
      setSubmitError(toErrorMessage(err, 'Failed to sign in. Please verify your credentials.'));
    } finally {
      setIsSubmitting(false);
    }
  };

  return (
    <div className="login-layout">
      <div className="card login-card">
        <div className="login-card__header">
          <h1 className="login-card__title">Sign In</h1>
          <p className="login-card__subtitle">
            Welcome back to Northwind Support Desk. Enter your agent credentials to continue.
          </p>
        </div>

        {submitError && <ErrorMessage message={submitError} />}

        <form className="login-form" onSubmit={handleSubmit} noValidate>
          <Field id="login-email" label="Email address" error={fieldErrors.email}>
            {(controlProps) => (
              <input
                {...controlProps}
                type="email"
                autoComplete="email"
                placeholder="agent@northwind.com"
                value={email}
                disabled={isSubmitting}
                onChange={(e) => {
                  setEmail(e.target.value);
                  if (fieldErrors.email) {
                    setFieldErrors((prev) => ({ ...prev, email: undefined }));
                  }
                }}
              />
            )}
          </Field>

          <Field id="login-password" label="Password" error={fieldErrors.password}>
            {(controlProps) => (
              <input
                {...controlProps}
                type="password"
                autoComplete="current-password"
                placeholder="••••••••"
                value={password}
                disabled={isSubmitting}
                onChange={(e) => {
                  setPassword(e.target.value);
                  if (fieldErrors.password) {
                    setFieldErrors((prev) => ({ ...prev, password: undefined }));
                  }
                }}
              />
            )}
          </Field>

          <div className="login-form__actions">
            <button
              type="submit"
              className="button button--primary login-button"
              disabled={isSubmitting}
              aria-busy={isSubmitting}
            >
              {isSubmitting ? 'Signing in...' : 'Sign in'}
            </button>
          </div>
        </form>
      </div>
    </div>
  );
}
