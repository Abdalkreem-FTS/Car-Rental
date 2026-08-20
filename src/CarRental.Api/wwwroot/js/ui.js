// Small DOM helpers shared by every page.

import { ApiError } from './api.js';

export const $ = (selector, root = document) => root.querySelector(selector);
export const $$ = (selector, root = document) => [...root.querySelectorAll(selector)];

export function escapeHtml(value) {
  return String(value ?? '').replace(/[&<>"']/g, (c) => (
    { '&': '&amp;', '<': '&lt;', '>': '&gt;', '"': '&quot;', "'": '&#39;' }[c]
  ));
}

/** Clears every inline field error and the form-level alert. */
export function clearErrors(form) {
  $$('.err', form).forEach((el) => { el.textContent = ''; el.classList.remove('show'); });
  $$('[aria-invalid="true"]', form).forEach((el) => el.removeAttribute('aria-invalid'));
  const alert = $('.alert', form);
  if (alert) { alert.textContent = ''; alert.classList.remove('show', 'alert-error', 'alert-success'); }
}

export function showFieldError(form, field, message) {
  const input = form.elements[field];
  const slot = $(`.err[data-for="${field}"]`, form);
  if (input) input.setAttribute('aria-invalid', 'true');
  if (slot) { slot.textContent = message; slot.classList.add('show'); }
  return Boolean(slot);
}

export function showAlert(form, message, kind = 'error') {
  const alert = $('.alert', form);
  if (!alert) return;
  alert.textContent = message;
  alert.classList.add('show', kind === 'error' ? 'alert-error' : 'alert-success');
}

/**
 * Renders a failed request onto the form: validation failures land on their own inputs
 * (the problem document keys `errors` by field name), anything else becomes a banner.
 */
export function showApiError(form, error) {
  if (!(error instanceof ApiError)) {
    showAlert(form, 'Something went wrong. Please check your connection and try again.');
    return;
  }

  if (error.fieldErrors) {
    let unplaced = [];
    for (const [field, messages] of Object.entries(error.fieldErrors)) {
      if (!showFieldError(form, field, messages[0])) unplaced.push(messages[0]);
    }
    if (unplaced.length) showAlert(form, unplaced.join(' '));
    return;
  }

  // A conflict on sign-up is about one specific field even though it is not a validation error.
  const conflictField = { 'user.email_already_in_use': 'email', 'car.plate_already_in_use': 'plateNumber' }[error.code];
  if (conflictField && showFieldError(form, conflictField, error.message)) return;

  showAlert(form, error.message);
}

/** Swaps a submit button into a spinner for the duration of `work`. */
export async function withBusy(button, label, work) {
  const original = button.innerHTML;
  button.disabled = true;
  button.innerHTML = `<span class="spinner" aria-hidden="true"></span>${escapeHtml(label)}`;
  try {
    return await work();
  } finally {
    button.disabled = false;
    button.innerHTML = original;
  }
}

let toastStack;

export function toast(message, kind = 'ok') {
  toastStack ??= (() => {
    const el = document.createElement('div');
    el.className = 'toast-stack';
    el.setAttribute('role', 'status');
    el.setAttribute('aria-live', 'polite');
    document.body.append(el);
    return el;
  })();

  const item = document.createElement('div');
  item.className = `toast ${kind}`;
  item.textContent = message;
  toastStack.append(item);
  setTimeout(() => item.remove(), 4200);
}

/** Reads a form into a plain object, turning blank optional inputs into null. */
export function readForm(form) {
  const data = {};
  for (const [key, raw] of new FormData(form).entries()) {
    const value = typeof raw === 'string' ? raw.trim() : raw;
    data[key] = value === '' ? null : value;
  }
  return data;
}

/** Trailing debounce — used so typing in a filter does not fire a request per keystroke. */
export function debounce(fn, delay = 350) {
  let timer;
  return (...args) => {
    clearTimeout(timer);
    timer = setTimeout(() => fn(...args), delay);
  };
}

export function money(amount) {
  return new Intl.NumberFormat(undefined, { style: 'currency', currency: 'USD', maximumFractionDigits: 0 }).format(amount);
}

export function formatDate(isoDate) {
  const [year, month, day] = isoDate.split('-').map(Number);
  return new Date(year, month - 1, day).toLocaleDateString(undefined, { day: 'numeric', month: 'short', year: 'numeric' });
}

export function todayIso(offsetDays = 0) {
  const date = new Date();
  date.setDate(date.getDate() + offsetDays);
  return date.toISOString().slice(0, 10);
}

/** Inclusive day count, matching how the server prices a rental. */
export function dayCount(startIso, endIso) {
  const start = new Date(`${startIso}T00:00:00`);
  const end = new Date(`${endIso}T00:00:00`);
  return Math.round((end - start) / 86_400_000) + 1;
}
