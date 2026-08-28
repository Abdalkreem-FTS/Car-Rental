// Talks to the Minimal API and owns the token pair. Every failed response from the server is
// an RFC 9457 problem document, so there is exactly one place that turns one into an Error.

const ACCESS_KEY = 'cr.accessToken';
const REFRESH_KEY = 'cr.refreshToken';
const USER_KEY = 'cr.user';

export const session = {
  get accessToken() { return localStorage.getItem(ACCESS_KEY); },
  get refreshToken() { return localStorage.getItem(REFRESH_KEY); },
  get user() {
    const raw = localStorage.getItem(USER_KEY);
    return raw ? JSON.parse(raw) : null;
  },
  save(auth) {
    localStorage.setItem(ACCESS_KEY, auth.accessToken);
    localStorage.setItem(REFRESH_KEY, auth.refreshToken);
    localStorage.setItem(USER_KEY, JSON.stringify(auth.user));
  },
  clear() {
    localStorage.removeItem(ACCESS_KEY);
    localStorage.removeItem(REFRESH_KEY);
    localStorage.removeItem(USER_KEY);
  },
  get isSignedIn() { return Boolean(localStorage.getItem(ACCESS_KEY)); },
};

/**
 * A failed request. `fieldErrors` is the problem document's `errors` map (field name -> messages)
 * for validation failures; `code` is the `errorCode` extension for everything else.
 */
export class ApiError extends Error {
  constructor(status, { title, detail, errors, errorCode } = {}) {
    super(detail || title || `Request failed with status ${status}`);
    this.name = 'ApiError';
    this.status = status;
    this.code = errorCode || null;
    this.fieldErrors = errors || null;
  }
}

async function readProblem(response) {
  try {
    return await response.json();
  } catch {
    return {};
  }
}

// One in-flight refresh at a time: several 401s arriving together must not each spend the
// refresh token, since redeeming one revokes it.
let refreshInFlight = null;

async function refreshTokens() {
  const refreshToken = session.refreshToken;
  if (!refreshToken) return false;

  refreshInFlight ??= (async () => {
    try {
      const response = await fetch('/api/auth/refresh', {
        method: 'POST',
        headers: { 'Content-Type': 'application/json' },
        body: JSON.stringify({ refreshToken }),
      });
      if (!response.ok) return false;
      session.save(await response.json());
      return true;
    } catch {
      return false;
    } finally {
      // Cleared on the next microtask so concurrent callers all observe this same attempt.
      queueMicrotask(() => { refreshInFlight = null; });
    }
  })();

  return refreshInFlight;
}

async function send(method, path, { body, auth = true, retry = true } = {}) {
  const headers = {};
  if (body !== undefined) headers['Content-Type'] = 'application/json';
  if (auth && session.accessToken) headers.Authorization = `Bearer ${session.accessToken}`;

  const response = await fetch(path, {
    method,
    headers,
    body: body === undefined ? undefined : JSON.stringify(body),
  });

  // An expired access token is normal — spend the refresh token and replay the request once.
  if (response.status === 401 && auth && retry && session.refreshToken) {
    if (await refreshTokens()) {
      return send(method, path, { body, auth, retry: false });
    }
    session.clear();
    redirectToSignIn();
    throw new ApiError(401, { detail: 'Your session has expired. Please sign in again.' });
  }

  if (response.status === 204) return null;

  if (!response.ok) {
    throw new ApiError(response.status, await readProblem(response));
  }

  return response.status === 202 ? null : response.json();
}

function redirectToSignIn() {
  const here = window.location.pathname + window.location.search;
  window.location.href = `/signin.html?returnTo=${encodeURIComponent(here)}`;
}

export const api = {
  register: (payload) => send('POST', '/api/auth/register', { body: payload, auth: false }),
  login: (payload) => send('POST', '/api/auth/login', { body: payload, auth: false }),
  logout: () => send('POST', '/api/auth/logout'),
  forgotPassword: (payload) => send('POST', '/api/auth/forgot-password', { body: payload, auth: false }),
  resetPassword: (payload) => send('POST', '/api/auth/reset-password', { body: payload, auth: false }),

  countries: () => send('GET', '/api/countries', { auth: false }),

  searchCars: (query) => send('GET', `/api/cars?${new URLSearchParams(query)}`),
  carLocations: () => send('GET', '/api/cars/locations'),

  createReservation: (payload) => send('POST', '/api/reservations', { body: payload }),
  myReservations: () => send('GET', '/api/reservations'),
  updateReservation: (id, payload) => send('PUT', `/api/reservations/${id}`, { body: payload }),
  cancelReservation: (id) => send('POST', `/api/reservations/${id}/cancel`),

  profile: () => send('GET', '/api/profile'),
  updateProfile: (payload) => send('PUT', '/api/profile', { body: payload }),
  changePassword: (payload) => send('PUT', '/api/profile/password', { body: payload }),
};

export { redirectToSignIn };
