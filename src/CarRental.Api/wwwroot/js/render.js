// Pure markup builders for the dashboard. Nothing here touches the network or the DOM tree.

import { escapeHtml, formatDate, money } from './ui.js';

const CATEGORY_ART = {
  SUV: '<path d="M4 17h16M5 17V11l2-4.2A2 2 0 0 1 8.8 5.6h6.4a2 2 0 0 1 1.8 1.2L19 11v6"/><path d="M5 11h14"/><circle cx="8" cy="17" r="1.8"/><circle cx="16" cy="17" r="1.8"/>',
  Van: '<path d="M3 17h18M4 17V7a1.4 1.4 0 0 1 1.4-1.4h9.2L20 11v6"/><path d="M14.6 5.6V11H20"/><circle cx="7.5" cy="17" r="1.8"/><circle cx="16.5" cy="17" r="1.8"/>',
  Pickup: '<path d="M3 16h18M4 16V9h8l1.6 3H20v4"/><path d="M12 9v3"/><circle cx="7.5" cy="16" r="1.8"/><circle cx="16.5" cy="16" r="1.8"/>',
  Luxury: '<path d="M3 16h18M4.5 16v-4.4l2.2-3.8A2 2 0 0 1 8.4 6.8h7.2a2 2 0 0 1 1.7 1l2.2 3.8V16"/><path d="M4.5 11.6h15"/><circle cx="8" cy="16" r="1.7"/><circle cx="16" cy="16" r="1.7"/>',
};

const DEFAULT_ART = '<path d="M4 16h16M5.5 16v-4.5l1.9-3.8A2 2 0 0 1 9.2 6.6h5.6a2 2 0 0 1 1.8 1.1l1.9 3.8V16"/><path d="M5.5 11.5h13"/><circle cx="8.5" cy="16" r="1.7"/><circle cx="15.5" cy="16" r="1.7"/>';

function carArt(car) {
  if (car.imageUrl) {
    return `<img src="${escapeHtml(car.imageUrl)}" alt="" loading="lazy"
      onerror="this.remove()">`;
  }
  return `<svg width="72" height="72" viewBox="0 0 24 24" fill="none" stroke="currentColor"
    stroke-width="1.4" stroke-linecap="round" stroke-linejoin="round">${CATEGORY_ART[car.category] ?? DEFAULT_ART}</svg>`;
}

export function carCard(car) {
  return `
    <article class="car-card">
      <div class="car-art">
        ${carArt(car)}
        <span class="car-badge">${escapeHtml(car.category)}</span>
      </div>
      <div class="car-body">
        <h3 class="car-title">${escapeHtml(car.make)} ${escapeHtml(car.model)}</h3>
        <p class="car-sub">${car.year} · ${escapeHtml(car.location)}</p>
        <div class="car-specs">
          <span class="spec">${escapeHtml(car.transmission)}</span>
          <span class="spec">${escapeHtml(car.fuel)}</span>
          <span class="spec">${car.seats} seats</span>
        </div>
        <div class="car-foot">
          <div class="price">${money(car.dailyRate)}<span> / day</span></div>
          <button class="btn btn-primary btn-sm" type="button" data-book="${car.id}">Book</button>
        </div>
      </div>
    </article>`;
}

/** Today in the same yyyy-mm-dd shape the API uses for rental days. */
const today = () => new Date().toISOString().slice(0, 10);

/** Still ahead of the renter: confirmed, and not yet returned. */
export function reservationCard(reservation) {
  // A rental can only be changed or called off before the day it starts.
  const isChangeable = reservation.status === 'Confirmed' && reservation.startDate > today();

  return `
    <article class="res-card">
      <div>
        <h3 class="res-title">${escapeHtml(reservation.carMake)} ${escapeHtml(reservation.carModel)} · ${reservation.carYear}</h3>
        <p class="res-meta">
          ${formatDate(reservation.startDate)} → ${formatDate(reservation.endDate)}
          · ${reservation.totalDays} day${reservation.totalDays === 1 ? '' : 's'}
          ${reservation.pickupLocation ? `· ${escapeHtml(reservation.pickupLocation)}` : ''}
        </p>
      </div>
      <div class="res-side">
        <span class="tag tag-${reservation.status.toLowerCase()}">${escapeHtml(reservation.status)}</span>
        <div class="price">${money(reservation.totalPrice)}</div>
        ${isChangeable ? `
          <button class="btn btn-ghost btn-sm" type="button" data-modify="${reservation.id}">Change dates</button>
          <button class="btn btn-danger btn-sm" type="button" data-cancel="${reservation.id}">Cancel</button>` : ''}
      </div>
    </article>`;
}

export function emptyState(title, message) {
  return `
    <div class="empty">
      <svg width="34" height="34" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="1.6" stroke-linecap="round" stroke-linejoin="round"><circle cx="11" cy="11" r="7"/><path d="m20 20-3.5-3.5"/></svg>
      <h3>${escapeHtml(title)}</h3>
      <p>${escapeHtml(message)}</p>
    </div>`;
}

export function skeletonGrid(count = 6) {
  return `<div class="skeleton-grid">${'<div class="skeleton"></div>'.repeat(count)}</div>`;
}
