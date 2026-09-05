// Scores a password against the same four rules the server enforces, plus a length bonus,
// so the meter never tells a user they are fine when Identity would reject them.

const RULES = [
  { test: (value) => value.length >= 8, hint: 'at least 8 characters' },
  { test: (value) => /[A-Z]/.test(value) && /[a-z]/.test(value), hint: 'upper and lower case' },
  { test: (value) => /[0-9]/.test(value), hint: 'a digit' },
  { test: (value) => /[^a-zA-Z0-9]/.test(value), hint: 'a symbol' },
];

const LABELS = ['', 'Weak', 'Fair', 'Good', 'Strong'];

export function scorePassword(value) {
  const missing = RULES.filter((rule) => !rule.test(value)).map((rule) => rule.hint);
  let score = RULES.length - missing.length;

  // A password that satisfies every rule but is still short is "Good", not "Strong".
  if (score === RULES.length && value.length < 12) score = 3;

  return { score, missing };
}

export function attachStrengthMeter(input, container, label) {
  const update = () => {
    const value = input.value;
    container.hidden = value.length === 0;
    if (!value) return;

    const { score, missing } = scorePassword(value);
    container.dataset.score = String(score);
    label.textContent = missing.length
      ? `${LABELS[score] || 'Too short'} — still needs ${missing.join(', ')}.`
      : `${LABELS[score]} password.`;
  };

  input.addEventListener('input', update);
  update();
}
