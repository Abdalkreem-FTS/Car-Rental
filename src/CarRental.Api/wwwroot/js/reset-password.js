import { api } from './api.js';
import { $, $$, clearErrors, readForm, showAlert, showApiError, showFieldError, withBusy } from './ui.js';
import { attachStrengthMeter, scorePassword } from './password-strength.js';

const form = $('#reset-form');
const params = new URLSearchParams(window.location.hash.slice(1));
const email = params.get('email');
const token = params.get('token');

history.replaceState(null, '', window.location.pathname);

attachStrengthMeter(form.elements.password, $('#strength'), $('#strength-label'));

$$('.pw-toggle', form).forEach((button) => {
  button.addEventListener('click', () => {
    const input = form.elements[button.dataset.toggle];
    const reveal = input.type === 'password';
    input.type = reveal ? 'text' : 'password';
    button.textContent = reveal ? 'Hide' : 'Show';
  });
});

if (!email || !token) {
  showAlert(form, 'This reset link is incomplete. Please request a new one from the sign-in page.');
  $('#submit').disabled = true;
} else {
  $('#reset-sub').textContent = `Choose a new password for ${email}.`;
}

form.addEventListener('submit', async (event) => {
  event.preventDefault();
  clearErrors(form);

  const data = readForm(form);

  const { missing } = scorePassword(data.password ?? '');
  if (missing.length) {
    showFieldError(form, 'password', `Your password still needs ${missing.join(', ')}.`);
    return;
  }
  if (data.password !== data.confirmPassword) {
    showFieldError(form, 'confirmPassword', 'The passwords do not match.');
    return;
  }

  await withBusy($('#submit'), 'Saving…', async () => {
    try {
      await api.resetPassword({ email, token, ...data });
      window.location.href = '/signin.html?reset=1';
    } catch (error) {
      showApiError(form, error);
    }
  });
});
