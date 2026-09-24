/**
 * Valid Palindrome — mirror pointers from both ends, skipping anything
 * that isn't alphanumeric, comparing case-insensitively. Not in the
 * artifact; written fresh in its voice.
 *
 * Input: "No 'x' in Nixon" — a classic short palindrome phrase that forces
 * both an apostrophe-skip and a space-skip on the way to the center, so the
 * SKIP branch actually gets exercised (not just the compare branch).
 *
 * Used on: /patterns/two-pointers/valid-palindrome/
 * (visualizer: 'two-pointers-palindrome')
 */
import { P, type ArrayStep, type CellState } from '../types';

export function buildSteps(): ArrayStep[] {
  const raw = "No 'x' in Nixon";
  const s = raw.split('');
  const isAlnum = (ch: string) => /[a-z0-9]/i.test(ch);
  const steps: ArrayStep[] = [];
  let L = 0;
  let R = s.length - 1;
  const cellState: Record<number, CellState> = {};

  steps.push({
    data: s,
    note: `Valid Palindrome: mirror pointers from both ends. Skip anything that isn't a letter or digit, compare the rest case-insensitively.`,
    pointers: { [L]: [P('L', 'amber')], [R]: [P('R', 'cyan')] },
    vars: { L, R, input: `"${raw}"` },
  });

  while (L < R) {
    if (!isAlnum(s[L]!)) {
      cellState[L] = 'dim';
      steps.push({
        data: s,
        note: `'${s[L]}' isn't alphanumeric → SKIP → L++`,
        pointers: { [L]: [P('L', 'amber')], [R]: [P('R', 'cyan')] },
        cells: { ...cellState },
        vars: { L, R },
      });
      L++;
      continue;
    }
    if (!isAlnum(s[R]!)) {
      cellState[R] = 'dim';
      steps.push({
        data: s,
        note: `'${s[R]}' isn't alphanumeric → SKIP → R--`,
        pointers: { [L]: [P('L', 'amber')], [R]: [P('R', 'cyan')] },
        cells: { ...cellState },
        vars: { L, R },
      });
      R--;
      continue;
    }

    const a = s[L]!.toLowerCase();
    const b = s[R]!.toLowerCase();
    if (a !== b) {
      cellState[L] = 'warn';
      cellState[R] = 'warn';
      steps.push({
        data: s,
        done: true,
        note: `'${a}' != '${b}' → mismatch → NOT a palindrome ✗`,
        pointers: { [L]: [P('L', 'amber')], [R]: [P('R', 'cyan')] },
        cells: { ...cellState },
        vars: { L, R },
      });
      return steps;
    }

    cellState[L] = 'ok';
    cellState[R] = 'ok';
    steps.push({
      data: s,
      note: `'${a}' == '${b}' → match → converge: L++, R--`,
      pointers: { [L]: [P('L', 'amber')], [R]: [P('R', 'cyan')] },
      cells: { ...cellState },
      vars: { L, R },
    });
    L++;
    R--;
  }

  steps.push({
    data: s,
    done: true,
    note: `L >= R → every pair matched → IS a palindrome ✓`,
    cells: { ...cellState },
    vars: { result: 'true' },
  });

  return steps;
}
