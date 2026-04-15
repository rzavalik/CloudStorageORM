#!/usr/bin/env bash
set -euo pipefail

ROOT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"
cd "$ROOT_DIR"

TEST_PROJECT="tests/CloudStorageORM.Tests/CloudStorageORM.Tests.csproj"
CONFIGURATION="Debug"
THRESHOLD_MS=1000
MAX_TESTS=0

usage() {
  cat <<'EOF'
Usage: ./scripts/run-unit-tests-one-by-one.sh [options]

Runs CloudStorageORM unit tests one at a time.
Each test is canceled when it reaches the threshold timeout.
Tests taking threshold or longer are reported as slow.

Options:
  --threshold-ms <number>   Timeout/slow threshold in milliseconds (default: 1000)
  --configuration <name>    dotnet test configuration (default: Debug)
  --max-tests <number>      Run only the first N discovered tests (default: 0 = all)
  --help                    Show this help
EOF
}

while [[ $# -gt 0 ]]; do
  case "$1" in
    --threshold-ms)
      THRESHOLD_MS="$2"
      shift 2
      ;;
    --configuration)
      CONFIGURATION="$2"
      shift 2
      ;;
    --max-tests)
      MAX_TESTS="$2"
      shift 2
      ;;
    --help|-h)
      usage
      exit 0
      ;;
    *)
      echo "Unknown option: $1" >&2
      usage
      exit 1
      ;;
  esac
done

if ! [[ "$THRESHOLD_MS" =~ ^[0-9]+$ ]]; then
  echo "--threshold-ms must be an integer (milliseconds)." >&2
  exit 1
fi

if ! [[ "$MAX_TESTS" =~ ^[0-9]+$ ]]; then
  echo "--max-tests must be an integer (0 means all tests)." >&2
  exit 1
fi

echo "Discovering tests in $TEST_PROJECT..."
TESTS=()
while IFS= read -r test_name; do
  [[ -n "$test_name" ]] && TESTS+=("$test_name")
done < <(
  dotnet test "$TEST_PROJECT" \
    --configuration "$CONFIGURATION" \
    --list-tests \
    --nologo \
    --verbosity minimal | awk '
      /The following Tests are available:/ { capture=1; next }
      capture && /^[[:space:]]+[^[:space:]]/ {
        sub(/^[[:space:]]+/, "", $0)
        print $0
      }
    '
)

if [[ ${#TESTS[@]} -eq 0 ]]; then
  echo "No tests found."
  exit 1
fi

if [[ "$MAX_TESTS" -gt 0 && "$MAX_TESTS" -lt ${#TESTS[@]} ]]; then
  TESTS=("${TESTS[@]:0:$MAX_TESTS}")
fi

echo "Running ${#TESTS[@]} test(s) one by one..."

declare -a SLOW_TESTS=()
declare -a FAILED_TESTS=()
declare -a TIMED_OUT_TESTS=()

index=0
for test_name in "${TESTS[@]}"; do
  index=$((index + 1))
  printf '[%d/%d] %s\n' "$index" "${#TESTS[@]}" "$test_name"

  output_file="$(mktemp)"
  # dotnet --list-tests returns display names for data-driven tests; strip '(...)' suffix for stable filtering.
  filter_name="${test_name%%(*}"

  # Run each test in an isolated process with a hard timeout so we can skip slow outliers.
  set +e
  run_result=$(python3 - "$THRESHOLD_MS" "$output_file" "$TEST_PROJECT" "$CONFIGURATION" "$filter_name" <<'PY'
import subprocess
import sys
import time

threshold_ms = int(sys.argv[1])
output_path = sys.argv[2]
project = sys.argv[3]
configuration = sys.argv[4]
filter_name = sys.argv[5]

command = [
    "dotnet", "test", project,
    "--configuration", configuration,
    "--no-build",
    "--no-restore",
    "--nologo",
    "--verbosity", "quiet",
    "--filter", f"FullyQualifiedName={filter_name}",
]

start = time.perf_counter()
timed_out = False

try:
    completed = subprocess.run(command, capture_output=True, text=True, timeout=threshold_ms / 1000.0)
    exit_code = completed.returncode
    combined_output = f"{completed.stdout}{completed.stderr}"
except subprocess.TimeoutExpired as ex:
    timed_out = True
    exit_code = 124
    stdout = ex.stdout or ""
    stderr = ex.stderr or ""
    if isinstance(stdout, bytes):
        stdout = stdout.decode("utf-8", errors="replace")
    if isinstance(stderr, bytes):
        stderr = stderr.decode("utf-8", errors="replace")
    combined_output = f"{stdout}{stderr}"

elapsed_ms = int(round((time.perf_counter() - start) * 1000))

with open(output_path, "w", encoding="utf-8") as file:
    file.write(combined_output)

print(f"{elapsed_ms}|{exit_code}|{1 if timed_out else 0}")
PY
  )
  exit_code=$?
  set -e

  if [[ "$exit_code" -ne 0 ]]; then
    FAILED_TESTS+=("$test_name")
    echo "  FAILED (runner error)"
    sed 's/^/    /' "$output_file"
    rm -f "$output_file"
    continue
  fi

  IFS='|' read -r elapsed_ms test_exit_code timed_out <<< "$run_result"

  if [[ "$elapsed_ms" -ge "$THRESHOLD_MS" ]]; then
    SLOW_TESTS+=("$elapsed_ms|$test_name")
  fi

  if [[ "$timed_out" -eq 1 ]]; then
    TIMED_OUT_TESTS+=("$test_name")
    echo "  CANCELED (${elapsed_ms}ms)"
    rm -f "$output_file"
    continue
  fi

  if [[ "$test_exit_code" -ne 0 ]]; then
    FAILED_TESTS+=("$test_name")
    echo "  FAILED (${elapsed_ms}ms)"
    sed 's/^/    /' "$output_file"
  else
    echo "  OK (${elapsed_ms}ms)"
  fi

  rm -f "$output_file"
done

echo
echo "Slow tests (>=${THRESHOLD_MS}ms):"
if [[ ${#SLOW_TESTS[@]} -eq 0 ]]; then
  echo "  None"
else
  printf '%s\n' "${SLOW_TESTS[@]}" | sort -t'|' -k1,1nr | while IFS='|' read -r elapsed_ms test_name; do
    printf '  %6sms  %s\n' "$elapsed_ms" "$test_name"
  done
fi

echo
echo "Canceled tests: ${#TIMED_OUT_TESTS[@]}"
if [[ ${#TIMED_OUT_TESTS[@]} -gt 0 ]]; then
  printf '  %s\n' "${TIMED_OUT_TESTS[@]}"
fi

echo
echo "Failed tests: ${#FAILED_TESTS[@]}"
if [[ ${#FAILED_TESTS[@]} -gt 0 ]]; then
  printf '  %s\n' "${FAILED_TESTS[@]}"
  exit 1
fi

echo "Done."