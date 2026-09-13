#!/usr/bin/env bash

set -euo pipefail

project_root="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"
project="$project_root/src/CodeAnalyzer/CodeAnalyzer.csproj"

run_case() {
    local name="$1"
    local input="$2"
    local expected_status="$3"
    local expected_warnings="$4"
    local output
    local status

    set +e
    output="$(dotnet run --no-build --project "$project" -- "$input" 2>&1)"
    status=$?
    set -e

    if [[ "$status" -ne "$expected_status" ]]; then
        printf 'FAIL: %s: ожидался код %s, получен %s\n%s\n' \
            "$name" "$expected_status" "$status" "$output"
        return 1
    fi

    if [[ "$expected_warnings" != "-" && "$output" != *"Всего предупреждений: $expected_warnings"* ]]; then
        printf 'FAIL: %s: ожидалось предупреждений: %s\n%s\n' \
            "$name" "$expected_warnings" "$output"
        return 1
    fi

    printf 'PASS: %s (код %s, предупреждений %s)\n' \
        "$name" "$status" "$expected_warnings"
}

dotnet build "$project" --nologo
run_case "корректный файл" "$project_root/samples/correct/Correct.cs" 0 0
run_case "файл с дефектами" "$project_root/samples/defects/Defects.cs" 1 5
run_case "каталог" "$project_root/samples/directory" 1 2
run_case "неверный путь" "$project_root/samples/missing.cs" 2 -

printf 'Все проверки пройдены.\n'
