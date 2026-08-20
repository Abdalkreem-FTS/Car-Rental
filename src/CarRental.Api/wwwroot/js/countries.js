export const COUNTRIES = [
  'Jordan', 'United Arab Emirates', 'Saudi Arabia', 'Qatar', 'Kuwait', 'Bahrain', 'Oman',
  'Egypt', 'Lebanon', 'Palestine', 'Iraq', 'Morocco', 'Tunisia', 'Algeria', 'Libya',
  'Turkey', 'Cyprus', 'Greece', 'Italy', 'Spain', 'Portugal', 'France', 'Germany',
  'Netherlands', 'Belgium', 'Switzerland', 'Austria', 'Sweden', 'Norway', 'Denmark',
  'Finland', 'Poland', 'Czechia', 'Romania', 'Ireland', 'United Kingdom',
  'United States', 'Canada', 'Mexico', 'Brazil', 'Argentina', 'Chile',
  'India', 'Pakistan', 'Bangladesh', 'China', 'Japan', 'South Korea',
  'Malaysia', 'Singapore', 'Indonesia', 'Philippines', 'Thailand', 'Vietnam',
  'Australia', 'New Zealand', 'South Africa', 'Nigeria', 'Kenya', 'Ethiopia',
];

export function fillCountrySelect(select, selected) {
  select.append(...COUNTRIES.map((name) => new Option(name, name, false, name === selected)));
}
