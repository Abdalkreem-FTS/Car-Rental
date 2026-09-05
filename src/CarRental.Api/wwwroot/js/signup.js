import { api, session } from './api.js';
import { $, $$, clearErrors, readForm, showApiError, showFieldError, withBusy } from './ui.js';
import { fillCountrySelect } from './countries.js';
import { attachStrengthMeter, scorePassword } from './password-strength.js';

const form = $('#signup-form');
const password = form.elements.password;

fillCountrySelect(form.elements.country, 'Jordan');
attachStrengthMeter(password, $('#strength'), $('#strength-label'));

// Never let someone pick a date of birth that is in the future.
form.elements.dateOfBirth.max = new Date().toISOString().slice(0, 10);

$$('.pw-toggle', form).forEach((button) => {
  button.addEventListener('click', () => {
    const input = form.elements[button.dataset.toggle];
    const reveal = input.type === 'password';
    input.type = reveal ? 'text' : 'password';
    button.textContent = reveal ? 'Hide' : 'Show';
  });
});

/** Catches the two mistakes worth catching before a round trip; the server re-checks everything. */
function localProblems(data) {
  const problems = [];
  const { missing } = scorePassword(data.password ?? '');
  if (missing.length) problems.push(['password', `Your password still needs ${missing.join(', ')}.`]);
  if (data.password !== data.confirmPassword) problems.push(['confirmPassword', 'The passwords do not match.']);
  return problems;
}

form.addEventListener('submit', async (event) => {
  event.preventDefault();
  clearErrors(form);

  const data = readForm(form);

  const problems = localProblems(data);
  if (problems.length) {
    problems.forEach(([field, message]) => showFieldError(form, field, message));
    form.elements[problems[0][0]].focus();
    return;
  }

  await withBusy($('#submit'), 'Creating account…', async () => {
    try {
      const auth = await api.register(data);
      session.save(auth);
      window.location.href = '/';
    } catch (error) {
      showApiError(form, error);
      const firstInvalid = $('[aria-invalid="true"]', form);
      (firstInvalid ?? form).scrollIntoView({ behavior: 'smooth', block: 'center' });
      firstInvalid?.focus({ preventScroll: true });
    }
  });
});
