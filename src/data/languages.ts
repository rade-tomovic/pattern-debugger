/**
 * Solution languages the site can render. `id` is used everywhere: the fence
 * language in MDX (```csharp), the rendered <pre data-language>, the
 * localStorage value, and the picker/tab labels' key.
 *
 * Adding a language here is step 1 of /add-language: the sidebar picker
 * appears automatically once this array has 2+ entries; <Solution> blocks
 * grow tabs for whichever variants each block actually contains.
 */

export interface SiteLanguage {
  /** fence language + data-language + storage value */
  id: string;
  /** human label on tabs and the picker */
  label: string;
}

export const languages: SiteLanguage[] = [
  { id: 'csharp', label: 'C#' },
  // { id: 'python', label: 'Python' },
  // { id: 'typescript', label: 'TypeScript' },
];

/** shown when no choice is stored; also the no-JS fallback (see Solution.astro CSS) */
export const defaultLang = 'csharp';

export const LANG_STORAGE_KEY = 'algo-lang';
export const LANG_EVENT = 'algo-lang-change';
