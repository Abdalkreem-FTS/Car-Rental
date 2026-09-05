import { api } from './api.js';
import { $, clearErrors, escapeHtml, readForm, showApiError, withBusy } from './ui.js';

const form = $('#forgot-form');

form.addEventListener('submit', async (event) => {
  event.preventDefault();
  clearErrors(form);

  const { email } = readForm(form);

  await withBusy($('#submit'), 'Sending…', async () => {
    try {
      await api.forgotPassword({ email });

      // The server answers the same way whether or not the address is registered, so the
      // page must not imply that an account exists.
      form.innerHTML = `
        <div class="alert alert-success show">
          If an account exists for <strong>${escapeHtml(email)}</strong>, a reset link is on its way.
          The link expires shortly, so use it soon.
        </div>
        <p class="form-foot">
          Nothing after five minutes? Check your spam folder, then
          <a href="/forgot-password.html">ask for another link</a>.
        </p>
        <p class="form-foot"><a href="/signin.html">Back to sign in</a></p>`;
    } catch (error) {
      showApiError(form, error);
    }
  });
});
