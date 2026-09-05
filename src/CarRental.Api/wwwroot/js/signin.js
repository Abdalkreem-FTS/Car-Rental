import { api, session } from './api.js';
import { $, $$, clearErrors, readForm, showAlert, showApiError, withBusy } from './ui.js';

const form = $('#signin-form');

// Only ever follow a same-origin path, so a crafted ?returnTo cannot bounce someone off-site.
const requested = new URLSearchParams(window.location.search).get('returnTo');
const returnTo = requested?.startsWith('/') && !requested.startsWith('//') ? requested : '/';

if (session.isSignedIn) {
  window.location.replace(returnTo);
}

if (new URLSearchParams(window.location.search).has('reset')) {
  showAlert(form, 'Your password has been changed. Sign in with your new one.', 'success');
}

$$('.pw-toggle', form).forEach((button) => {
  button.addEventListener('click', () => {
    const input = form.elements[button.dataset.toggle];
    const reveal = input.type === 'password';
    input.type = reveal ? 'text' : 'password';
    button.textContent = reveal ? 'Hide' : 'Show';
  });
});

form.addEventListener('submit', async (event) => {
  event.preventDefault();
  clearErrors(form);

  await withBusy($('#submit'), 'Signing in…', async () => {
    try {
      const auth = await api.login(readForm(form));
      session.save(auth);
      window.location.href = returnTo;
    } catch (error) {
      showApiError(form, error);
    }
  });
});
