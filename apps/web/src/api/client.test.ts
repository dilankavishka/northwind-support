import { afterEach, beforeEach, describe, expect, it, vi } from 'vitest';
import {
  AUTH_TOKEN_KEY,
  ApiError,
  getAuthToken,
  onUnauthorized,
  request,
  setAuthToken,
} from './client';

describe('client.ts auth and token handling', () => {
  beforeEach(() => {
    localStorage.clear();
    vi.restoreAllMocks();
  });

  afterEach(() => {
    localStorage.clear();
  });

  it('stores and retrieves token from localStorage', () => {
    expect(getAuthToken()).toBeNull();

    setAuthToken('my-test-jwt-token');
    expect(getAuthToken()).toBe('my-test-jwt-token');
    expect(localStorage.getItem(AUTH_TOKEN_KEY)).toBe('my-test-jwt-token');

    setAuthToken(null);
    expect(getAuthToken()).toBeNull();
    expect(localStorage.getItem(AUTH_TOKEN_KEY)).toBeNull();
  });

  it('attaches Authorization header when token exists in storage', async () => {
    setAuthToken('sample-bearer-token');

    const fetchSpy = vi.spyOn(globalThis, 'fetch').mockResolvedValueOnce(
      new Response(JSON.stringify({ success: true }), {
        status: 200,
        headers: { 'Content-Type': 'application/json' },
      }),
    );

    const result = await request<{ success: boolean }>('/test-endpoint');

    expect(result).toEqual({ success: true });
    expect(fetchSpy).toHaveBeenCalledWith(
      '/api/test-endpoint',
      expect.objectContaining({
        headers: expect.objectContaining({
          Authorization: 'Bearer sample-bearer-token',
          'Content-Type': 'application/json',
        }),
      }),
    );
  });

  it('does not attach Authorization header when no token exists', async () => {
    setAuthToken(null);

    const fetchSpy = vi.spyOn(globalThis, 'fetch').mockResolvedValueOnce(
      new Response(JSON.stringify({ ok: true }), {
        status: 200,
        headers: { 'Content-Type': 'application/json' },
      }),
    );

    await request('/public-endpoint');

    const headers = fetchSpy.mock.calls[0][1]?.headers as Record<string, string>;
    expect(headers.Authorization).toBeUndefined();
  });

  it('clears token and notifies onUnauthorized listeners when API returns 401', async () => {
    setAuthToken('expired-jwt');
    const unauthorizedCallback = vi.fn();
    const unsubscribe = onUnauthorized(unauthorizedCallback);

    vi.spyOn(globalThis, 'fetch').mockResolvedValueOnce(
      new Response(
        JSON.stringify({
          title: 'Unauthorized',
          detail: 'Invalid or expired token.',
          status: 401,
        }),
        {
          status: 401,
          headers: { 'Content-Type': 'application/problem+json' },
        },
      ),
    );

    await expect(request('/protected')).rejects.toThrow(ApiError);
    expect(getAuthToken()).toBeNull();
    expect(unauthorizedCallback).toHaveBeenCalledTimes(1);

    unsubscribe();
  });
});
