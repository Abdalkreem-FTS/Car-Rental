import { api, session, redirectToSignIn } from './api.js';
import {
  $, $$, clearErrors, dayCount, debounce, formatDate, money, readForm,
  showAlert, showApiError, showFieldError, toast, todayIso, withBusy,
} from './ui.js';
import { carCard, emptyState, reservationCard, skeletonGrid } from './render.js';
import { fillCountrySelect } from './countries.js';
import { attachStrengthMeter, scorePassword } from './password-strength.js';

if (!session.isSignedIn) {
  redirectToSignIn();
}

const state = {
  page: 1,
  totalPages: 1,
  cars: new Map(),
  reservations: new Map(),
  reservationsLoaded: false,
  profileLoaded: false,
};

/* ---------------- shell ---------------- */

function paintUser(user) {
  if (!user) return;
  $('#user-name').textContent = `${user.firstName} ${user.lastName}`;
  $('#user-email').textContent = user.email;
  $('#user-initials').textContent = `${user.firstName[0] ?? ''}${user.lastName[0] ?? ''}`.toUpperCase();
  $('#greeting').textContent = `Welcome back, ${user.firstName}`;
}

paintUser(session.user);

function showView(name) {
  $$('.nav-link[data-view]').forEach((link) => link.classList.toggle('active', link.dataset.view === name));
  $$('.view').forEach((view) => { view.hidden = view.id !== `view-${name}`; });

  if ((name === 'reservations' || name === 'history') && !state.reservationsLoaded) loadReservations();
  if (name === 'profile' && !state.profileLoaded) loadProfile();
}

$$('.nav-link[data-view]').forEach((link) => {
  link.addEventListener('click', () => showView(link.dataset.view));
});

$('#signout').addEventListener('click', async () => {
  try {
    await api.logout();
  } catch {
    // Revoking server-side is best effort; the local session goes either way.
  }
  session.clear();
  window.location.href = '/signin.html';
});

/* ---------------- browse ---------------- */

const searchForm = $('#search-form');
const results = $('#car-results');

async function loadLocations() {
  try {
    const locations = await api.carLocations();
    searchForm.elements.location.append(...locations.map((name) => new Option(name, name)));
  } catch {
    // Not fatal — the free-text search still covers location.
  }
}

const minimumSearchTermLength = 3;

function tooShortToSearch(value) {
  const term = value.trim();

  return term.length > 0 && term.length < minimumSearchTermLength;
}

function searchQuery() {
  const query = { page: state.page, pageSize: 12 };

  for (const [key, value] of new FormData(searchForm).entries()) {
    if (value !== '') query[key] = value;
  }

  if (query.query !== undefined && tooShortToSearch(query.query)) delete query.query;

  return query;
}

async function loadCars() {
  results.innerHTML = skeletonGrid();
  $('#pagination').hidden = true;

  try {
    const page = await api.searchCars(searchQuery());

    state.totalPages = Math.max(page.totalPages, 1);
    state.cars = new Map(page.items.map((car) => [car.id, car]));

    $('#result-count').textContent = page.totalCount === 1
      ? '1 car available'
      : `${page.totalCount} cars available`;

    results.innerHTML = page.items.length
      ? `<div class="car-grid">${page.items.map(carCard).join('')}</div>`
      : emptyState('No cars match those filters', 'Try widening your dates, raising the price cap, or clearing a filter or two.');

    $('#pagination').hidden = page.totalPages <= 1;
    $('#page-info').textContent = `Page ${page.page} of ${page.totalPages}`;
    $('#prev-page').disabled = !page.hasPrevious;
    $('#next-page').disabled = !page.hasNext;
  } catch (error) {
    results.innerHTML = emptyState('We could not load the fleet', error.message);
  }
}

searchForm.addEventListener('submit', (event) => {
  event.preventDefault();
  state.page = 1;
  loadCars();
});

function rerunSearch() {
  state.page = 1;
  loadCars();
}

// Dropdowns and dates commit in one gesture, so search the moment they change.
$$('select, input[type="date"]', searchForm).forEach((input) => {
  input.addEventListener('change', rerunSearch);
});

// Typed filters fire on every keystroke instead, debounced. Waiting for `change` here would
// mean nothing happened until the field lost focus, which reads as a broken filter.
const rerunSearchSoon = debounce(rerunSearch);

$('#query').addEventListener('input', (event) => {
  $('#query-hint').hidden = !tooShortToSearch(event.target.value);
});
$$('#query, #maxDailyRate', searchForm).forEach((input) => {
  input.addEventListener('input', rerunSearchSoon);
});

// A return date before the pick-up date is never valid, so stop it at the input.
searchForm.elements.pickupDate.addEventListener('change', () => {
  searchForm.elements.returnDate.min = searchForm.elements.pickupDate.value || todayIso();
  if (searchForm.elements.returnDate.value && searchForm.elements.returnDate.value < searchForm.elements.pickupDate.value) {
    searchForm.elements.returnDate.value = searchForm.elements.pickupDate.value;
  }
});

$('#reset-filters').addEventListener('click', () => {
  searchForm.reset();
  state.page = 1;
  loadCars();
});

$('#prev-page').addEventListener('click', () => {
  if (state.page > 1) { state.page -= 1; loadCars(); window.scrollTo({ top: 0, behavior: 'smooth' }); }
});

$('#next-page').addEventListener('click', () => {
  if (state.page < state.totalPages) { state.page += 1; loadCars(); window.scrollTo({ top: 0, behavior: 'smooth' }); }
});

/* ---------------- booking ---------------- */

const dialog = $('#book-dialog');
const bookForm = $('#book-form');
let bookingCar = null;

// One key per booking attempt, so a double tap or a retry after a timeout replays the first
// booking instead of making a second one.
let bookingKey = null;

function paintSummary() {
  const { startDate, endDate } = bookForm.elements;
  const summary = $('#book-summary');

  if (!bookingCar || !startDate.value || !endDate.value || endDate.value < startDate.value) {
    summary.innerHTML = '<div class="summary-row muted">Choose your dates to see the total.</div>';
    return;
  }

  const days = dayCount(startDate.value, endDate.value);
  summary.innerHTML = `
    <div class="summary-row"><span class="muted">${money(bookingCar.dailyRate)} × ${days} day${days === 1 ? '' : 's'}</span><span>${money(bookingCar.dailyRate * days)}</span></div>
    <div class="summary-row total"><span>Total</span><span>${money(bookingCar.dailyRate * days)}</span></div>`;
}

function openBooking(car) {
  bookingCar = car;
  bookingKey = crypto.randomUUID();
  clearErrors(bookForm);
  bookForm.reset();

  $('#book-title').textContent = `${car.make} ${car.model}`;
  $('#book-subtitle').textContent = `${car.year} · ${car.category} · ${car.transmission} · ${car.seats} seats`;

  const pickup = searchForm.elements.pickupDate.value || todayIso(1);
  const dropOff = searchForm.elements.returnDate.value || todayIso(3);

  bookForm.elements.startDate.min = todayIso();
  bookForm.elements.startDate.value = pickup;
  bookForm.elements.endDate.min = pickup;
  bookForm.elements.endDate.value = dropOff < pickup ? pickup : dropOff;
  bookForm.elements.pickupLocation.value = car.location;

  paintSummary();
  dialog.showModal();
}

results.addEventListener('click', (event) => {
  const id = event.target.closest('[data-book]')?.dataset.book;
  if (id) openBooking(state.cars.get(id));
});

bookForm.elements.startDate.addEventListener('change', () => {
  const { startDate, endDate } = bookForm.elements;
  endDate.min = startDate.value;
  if (endDate.value < startDate.value) endDate.value = startDate.value;
  paintSummary();
});

bookForm.elements.endDate.addEventListener('change', paintSummary);
$('#book-cancel').addEventListener('click', () => dialog.close());

bookForm.addEventListener('submit', async (event) => {
  event.preventDefault();
  clearErrors(bookForm);

  const data = readForm(bookForm);

  if (data.endDate < data.startDate) {
    showFieldError(bookForm, 'endDate', 'The return date must be on or after the pick-up date.');
    return;
  }

  await withBusy($('#book-confirm'), 'Booking…', async () => {
    try {
      const reservation = await api.createReservation({ carId: bookingCar.id, ...data }, bookingKey);
      dialog.close();
      toast(`Booked — ${reservation.carMake} ${reservation.carModel} for ${money(reservation.totalPrice)}.`);

      state.reservationsLoaded = false;
      loadCars();
    } catch (error) {
      showApiError(bookForm, error);
    }
  });
});

/* ---------------- reservations and history ---------------- */

const reservationResults = $('#reservation-results');
const historyResults = $('#history-results');

async function loadReservations() {
  reservationResults.innerHTML = skeletonGrid(3);
  historyResults.innerHTML = skeletonGrid(2);

  try {
    const [ahead, done] = await Promise.all([
      api.myReservations('Upcoming'),
      api.myReservations('Past'),
    ]);

    state.reservationsLoaded = true;
    state.reservations = new Map([...ahead.items, ...done.items].map((reservation) => [reservation.id, reservation]));

    const upcoming = ahead.items;
    const history = done.items;

    reservationResults.innerHTML = upcoming.length
      ? `<div class="res-list">${upcoming.map(reservationCard).join('')}</div>`
      : emptyState('Nothing booked yet', 'Once you book a car it will show up here with everything you need for pick-up.');

    historyResults.innerHTML = history.length
      ? `<div class="res-list">${history.map(reservationCard).join('')}</div>`
      : emptyState('No past bookings', 'Rentals you have completed, and anything you cancel, will be listed here.');
  } catch (error) {
    reservationResults.innerHTML = emptyState('We could not load your reservations', error.message);
    historyResults.innerHTML = '';
  }
}

reservationResults.addEventListener('click', async (event) => {
  const modify = event.target.closest('[data-modify]');
  if (modify) {
    openModify(state.reservations.get(modify.dataset.modify));
    return;
  }

  const button = event.target.closest('[data-cancel]');
  if (!button) return;

  if (!window.confirm('Cancel this reservation? This cannot be undone.')) return;

  await withBusy(button, 'Cancelling…', async () => {
    try {
      await api.cancelReservation(button.dataset.cancel);
      toast('Reservation cancelled.');
      await loadReservations();
    } catch (error) {
      toast(error.message, 'bad');
    }
  });
});

/* ---------------- change dates ---------------- */

const modifyDialog = $('#modify-dialog');
const modifyForm = $('#modify-form');
let modifying = null;

function paintModifySummary() {
  const { startDate, endDate } = modifyForm.elements;
  const summary = $('#modify-summary');

  if (!modifying || !startDate.value || !endDate.value || endDate.value < startDate.value) {
    summary.innerHTML = '<div class="summary-row muted">Choose your dates to see the new total.</div>';
    return;
  }

  const days = dayCount(startDate.value, endDate.value);
  const total = modifying.dailyRate * days;

  summary.innerHTML = `
    <div class="summary-row"><span class="muted">${money(modifying.dailyRate)} × ${days} day${days === 1 ? '' : 's'}</span><span>${money(total)}</span></div>
    <div class="summary-row"><span class="muted">Previously</span><span>${money(modifying.totalPrice)}</span></div>
    <div class="summary-row total"><span>New total</span><span>${money(total)}</span></div>`;
}

function openModify(reservation) {
  if (!reservation) return;

  modifying = reservation;
  clearErrors(modifyForm);

  $('#modify-title').textContent = `${reservation.carMake} ${reservation.carModel}`;
  $('#modify-subtitle').textContent = `Currently ${formatDate(reservation.startDate)} → ${formatDate(reservation.endDate)}`;

  modifyForm.elements.startDate.min = todayIso();
  modifyForm.elements.startDate.value = reservation.startDate;
  modifyForm.elements.endDate.min = reservation.startDate;
  modifyForm.elements.endDate.value = reservation.endDate;
  modifyForm.elements.pickupLocation.value = reservation.pickupLocation ?? '';

  paintModifySummary();
  modifyDialog.showModal();
}

modifyForm.elements.startDate.addEventListener('change', () => {
  const { startDate, endDate } = modifyForm.elements;
  endDate.min = startDate.value;
  if (endDate.value < startDate.value) endDate.value = startDate.value;
  paintModifySummary();
});

modifyForm.elements.endDate.addEventListener('change', paintModifySummary);
$('#modify-cancel').addEventListener('click', () => modifyDialog.close());

modifyForm.addEventListener('submit', async (event) => {
  event.preventDefault();
  clearErrors(modifyForm);

  const data = readForm(modifyForm);

  if (data.endDate < data.startDate) {
    showFieldError(modifyForm, 'endDate', 'The return date must be on or after the pick-up date.');
    return;
  }

  await withBusy($('#modify-confirm'), 'Saving…', async () => {
    try {
      const updated = await api.updateReservation(modifying.id, data, modifying.version);
      modifyDialog.close();
      const change = updated.previousTotalPrice != null && updated.previousTotalPrice !== updated.totalPrice
        ? ` (was ${money(updated.previousTotalPrice)})`
        : '';
      toast(`Moved to ${formatDate(updated.startDate)} — ${money(updated.totalPrice)}${change}.`);

      await loadReservations();
      loadCars();
    } catch (error) {
      showApiError(modifyForm, error);
    }
  });
});

/* ---------------- profile ---------------- */

const profileForm = $('#profile-form');
const passwordForm = $('#password-form');

fillCountrySelect(profileForm.elements.country);
attachStrengthMeter(passwordForm.elements.newPassword, $('#pw-strength'), $('#pw-strength-label'));
profileForm.elements.dateOfBirth.max = todayIso();

async function loadProfile() {
  try {
    const profile = await api.profile();
    state.profileLoaded = true;

    // Wait for the options before assigning the stored country, or the assignment finds an empty
    // list and is dropped.
    await fillCountrySelect(profileForm.elements.country, profile.country);

    for (const field of ['firstName', 'lastName', 'phoneNumber', 'dateOfBirth',
      'addressLine1', 'addressLine2', 'city', 'country', 'driverLicenseNumber']) {
      profileForm.elements[field].value = profile[field] ?? '';
    }
  } catch (error) {
    showAlert(profileForm, error.message);
  }
}

profileForm.addEventListener('submit', async (event) => {
  event.preventDefault();
  clearErrors(profileForm);

  await withBusy($('#profile-save'), 'Saving…', async () => {
    try {
      const profile = await api.updateProfile(readForm(profileForm));

      // Keep the sidebar and the cached session in step with the new name.
      const user = session.user;
      session.save({
        accessToken: session.accessToken,
        user: { ...user, firstName: profile.firstName, lastName: profile.lastName, phoneNumber: profile.phoneNumber },
      });
      paintUser(session.user);

      showAlert(profileForm, 'Your details have been saved.', 'success');
    } catch (error) {
      showApiError(profileForm, error);
    }
  });
});

passwordForm.addEventListener('submit', async (event) => {
  event.preventDefault();
  clearErrors(passwordForm);

  const data = readForm(passwordForm);

  const { missing } = scorePassword(data.newPassword ?? '');
  if (missing.length) {
    showFieldError(passwordForm, 'newPassword', `Your password still needs ${missing.join(', ')}.`);
    return;
  }
  if (data.newPassword !== data.confirmPassword) {
    showFieldError(passwordForm, 'confirmPassword', 'The passwords do not match.');
    return;
  }

  await withBusy($('#password-save'), 'Changing…', async () => {
    try {
      await api.changePassword(data);
      passwordForm.reset();
      $('#pw-strength').hidden = true;
      showAlert(passwordForm, 'Your password has been changed. Other devices have been signed out.', 'success');
    } catch (error) {
      showApiError(passwordForm, error);
    }
  });
});

/* ---------------- boot ---------------- */

searchForm.elements.pickupDate.min = todayIso();
searchForm.elements.returnDate.min = todayIso();

loadLocations();
loadCars();
