#!/usr/bin/env bash
# Serialize timing runs — but only the ones that actually need it.
#
# Content jobs run in parallel (up to 14 at once on this box). A timing run
# that shares the machine with another timing run measures the other one, so
# measurements must not overlap. But most of what those jobs run is NOT a
# measurement — compile checks, `sizeof` probes, IL dumps, objdump, a bug hunt
# reproducing a race, a cross-language correctness check. Forcing those through
# the same exclusive gate leaves the box idle while a dozen jobs queue behind
# work that never needed isolation.
#
# So there are two modes, backed by flock(2):
#
#   scripts/bench-lock.sh env DOTNET_TieredCompilation=0 dotnet run x.cs -c Release
#       EXCLUSIVE (default). Use for anything whose NUMBERS reach a page.
#       Runs alone: no other exclusive run, no shared runs.
#
#   scripts/bench-lock.sh --shared dotnet run check.cs -c Release
#       SHARED. Use for correctness-only runs, where you care whether it
#       compiles / throws / reproduces, not how long it took. Many shared runs
#       proceed at once, but all of them wait while an exclusive run holds the
#       lock, so they can never perturb a measurement.
#
# If in doubt, use exclusive. A slow correct number beats a fast wrong one.
#
# flock is used rather than a mkdir spinlock because the kernel drops the lock
# when the holder dies — there is no stale-lock window and no breaker heuristic
# to get wrong.
set -euo pipefail

LOCK="${BENCH_LOCK_FILE:-/tmp/algo-ds-bench.lock}"
TIMEOUT="${BENCH_LOCK_TIMEOUT:-7200}"
MODE=-x
MODE_NAME=exclusive

if [ "${1:-}" = "--shared" ]; then
  MODE=-s
  MODE_NAME=shared
  shift
elif [ "${1:-}" = "--exclusive" ]; then
  shift
fi

if [ "$#" -eq 0 ]; then
  echo "usage: bench-lock.sh [--shared|--exclusive] <command> [args...]" >&2
  exit 2
fi

# The lock file must exist and be writable by every job.
if [ ! -e "$LOCK" ]; then
  ( umask 000; : > "$LOCK" ) 2>/dev/null || true
fi

START=$(date +%s)
exec 9>>"$LOCK"
if ! flock "$MODE" -w "$TIMEOUT" 9; then
  echo "bench-lock: gave up after ${TIMEOUT}s waiting for a $MODE_NAME lock on $LOCK" >&2
  exit 1
fi
WAITED=$(( $(date +%s) - START ))
[ "$WAITED" -gt 5 ] && echo "bench-lock: acquired $MODE_NAME lock after ${WAITED}s" >&2

"$@"
