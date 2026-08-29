import { api } from './api.js';
import { $, clearErrors, readForm, showAlert, showApiError, withBusy } from './ui.js';

const form = $('#confirm-form');
const resendForm = $('#resend-form');
const heading = $('#confirm-heading');
const sub = $('#confirm-sub');

const params = new URLSearchParams(window.location.hash.slice(1));
const email = params.get('email');
const token = params.get('token');

history.replaceState(null, '', window.location.pathname);

function offerResend(message) {
  heading.textContent = 'We could not confirm that link';
  sub.textContent = message;
  resendForm.hidden = false;

  if (email) {
    resendForm.elements.email.value = email;
  }
}

if (!email || !token) {
  offerResend('The link is incomplete. Enter your address and we will send a new one.');
} else {
  try {
    await api.confirmEmail({ email, token });

    heading.textContent = 'Email confirmed';
    sub.textContent = `${email} is confirmed. You can book a car now.`;
  } catch (error) {
    showApiError(form, error);
    offerResend('That link may have expired or already been used.');
  }
}

resendForm.addEventListener('submit', async (event) => {
  event.preventDefault();
  clearErrors(resendForm);

  await withBusy($('#resend'), 'Sending…', async () => {
    try {
      await api.resendConfirmation(readForm(resendForm));

      resendForm.hidden = true;
      heading.textContent = 'Check your inbox';
      sub.textContent = 'If that address needs confirming, a new link is on its way.';
    } catch (error) {
      showApiError(resendForm, error);
    }
  });
});
