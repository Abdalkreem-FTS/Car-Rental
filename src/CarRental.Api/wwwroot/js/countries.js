import { api } from './api.js';

// The list comes from the server, so adding a country is not a front-end release. Fetched once per
// page load and shared by every caller.
let countries;

function loadCountries() {
  countries ??= api.countries().catch(() => []);

  return countries;
}

/**
 * Fills a <select> with the country list. Keeps whatever is already selected, and if that value is
 * not in the list — an older name, say — it is added rather than silently dropped.
 */
export async function fillCountrySelect(select, selected) {
  const names = await loadCountries();
  const chosen = selected ?? select.value;
  const options = names.map((name) => new Option(name, name, false, name === chosen));

  if (chosen && !names.includes(chosen)) {
    options.unshift(new Option(chosen, chosen, false, true));
  }

  select.replaceChildren(...options);
  select.value = chosen ?? '';
}
