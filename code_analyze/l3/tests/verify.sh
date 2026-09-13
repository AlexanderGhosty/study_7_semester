#!/usr/bin/env bash

set -euo pipefail

project_dir=$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)
project_file="$project_dir/src/CodeAnalyzer/CodeAnalyzer.csproj"
test_output_dir=$(mktemp -d)
empty_input_dir=$(mktemp -d)

cleanup() {
    rm -rf "$test_output_dir" "$empty_input_dir"
}

trap cleanup EXIT

run_case() {
    local name=$1
    local expected_status=$2
    local input_path=$3
    local output_file="$test_output_dir/$name.txt"
    local actual_status

    set +e
    dotnet run --no-build --project "$project_file" -- "$input_path" >"$output_file" 2>&1
    actual_status=$?
    set -e

    if [[ $actual_status -ne $expected_status ]]; then
        printf 'FAIL: %s returned %s, expected %s\n' "$name" "$actual_status" "$expected_status" >&2
        sed -n '1,180p' "$output_file" >&2
        exit 1
    fi
}

assert_contains() {
    local name=$1
    local expected=$2
    local output_file="$test_output_dir/$name.txt"

    if ! grep -Fq "$expected" "$output_file"; then
        printf 'FAIL: %s does not contain: %s\n' "$name" "$expected" >&2
        sed -n '1,180p' "$output_file" >&2
        exit 1
    fi
}

dotnet build "$project_file" --nologo

run_case correct 0 "$project_dir/samples/correct/Correct.cs"
assert_contains correct "Всего предупреждений: 0"

run_case defects 1 "$project_dir/samples/defects/Defects.cs"
assert_contains defects "Всего предупреждений: 5"
assert_contains defects "Строка: 7"
assert_contains defects "локальной переменной 'neverRead'"
assert_contains defects "Строка: 9"
assert_contains defects "локальной переменной 'overwritten'"
assert_contains defects "Строка: 14"
assert_contains defects "локальной переменной 'lastWrite'"
assert_contains defects "Строка: 16"
assert_contains defects "локальной переменной 'output'"
assert_contains defects "Строка: 19"
assert_contains defects "локальной переменной 'incremented'"

run_case corner 1 "$project_dir/samples/corner/CornerCase.cs"
assert_contains corner "Всего предупреждений: 1"
assert_contains corner "Строка: 14"
assert_contains corner "локальной переменной 'marker'"

run_case directory 1 "$project_dir/samples/directory"
assert_contains directory "Всего предупреждений: 1"
assert_contains directory "Bad.cs"
assert_contains directory "локальной переменной 'unused'"

run_case missing 2 "$project_dir/samples/missing.cs"
assert_contains missing "Ошибка: указанный путь не найден."

run_case wrong_extension 2 "$project_dir/PLAN.md"
assert_contains wrong_extension "Ошибка: указанный файл должен иметь расширение .cs."

run_case empty_directory 2 "$empty_input_dir"
assert_contains empty_directory "Ошибка: в указанном каталоге не найдены файлы .cs."

run_case invalid_syntax 2 "$project_dir/samples/invalid/Broken.cs"
assert_contains invalid_syntax "Ошибка анализа: синтаксическая ошибка"

printf 'All checks passed.\n'
