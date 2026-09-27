SHELL := /bin/bash

VENV_DIR ?= .venv
PYTHON ?= python3
AVD_NAME ?= Medium_Phone_API_36.0
APPIUM_HOST ?= 127.0.0.1
APPIUM_PORT ?= 4723
APPIUM_URL := http://$(APPIUM_HOST):$(APPIUM_PORT)

APPIUM_PID_FILE := .appium.pid
EMULATOR_LOG := emulator.log
APPIUM_LOG := appium.log
TEST_LOG := clock_simple_test_results.log

.PHONY: all help check-tools setup deps python appium test run-emulator run-appium run-tests stop-all clean status logs screenshot

all: help

help:
	@echo "Mobile testing with Appium and Android Emulator"
	@echo ""
	@echo "Setup:"
	@echo "  make setup                         Install Python/Appium dependencies"
	@echo "  make check-tools                   Check required commands and AVD"
	@echo ""
	@echo "Run:"
	@echo "  make run-emulator                  Start the configured AVD with a GUI"
	@echo "  make run-appium                    Start Appium on port $(APPIUM_PORT)"
	@echo "  make test                          Run test_clock.py"
	@echo "  make run-tests                     Start emulator, Appium and tests"
	@echo ""
	@echo "Diagnostics:"
	@echo "  make status                        Show ADB/Appium status"
	@echo "  make logs                          Show recent test and service logs"
	@echo "  make screenshot NAME=clock.png     Save an emulator screenshot"
	@echo "  make stop-all                      Stop services started by this Makefile"
	@echo "  make clean                         Remove local Python/test caches"

check-tools:
	@command -v $(PYTHON) >/dev/null || { echo "ERROR: $(PYTHON) is not installed"; exit 1; }
	@command -v adb >/dev/null || { echo "ERROR: adb is not in PATH"; exit 1; }
	@command -v emulator >/dev/null || { echo "ERROR: emulator is not in PATH"; exit 1; }
	@command -v appium >/dev/null || { echo "ERROR: appium is not in PATH"; exit 1; }
	@emulator -list-avds | grep -Fxq "$(AVD_NAME)" || { echo "ERROR: AVD '$(AVD_NAME)' was not found. Override it with AVD_NAME=..."; exit 1; }
	@echo "Required commands and AVD are available."

setup: deps python appium
	@mkdir -p screenshots logs reports
	@echo "Setup complete. Run 'make check-tools' before starting the emulator."

# Install the host packages using the native package manager. Android system
# images and AVDs are intentionally not created here because they are large and
# require an explicit choice of image.
deps:
	@if command -v apt-get >/dev/null; then \
		sudo apt-get update; \
		sudo apt-get install -y openjdk-17-jdk nodejs npm python3-venv python3-pip wget unzip curl; \
	elif command -v pacman >/dev/null; then \
		sudo pacman -S --needed --noconfirm jdk17-openjdk nodejs npm python python-pip wget unzip curl android-tools; \
	else \
		echo "ERROR: unsupported package manager. Install Java, Node.js, npm, Python, adb, emulator and curl manually."; \
		exit 1; \
	fi

python:
	$(PYTHON) -m venv $(VENV_DIR)
	$(VENV_DIR)/bin/python -m pip install --upgrade pip
	$(VENV_DIR)/bin/python -m pip install -r requirements.txt

appium:
	sudo npm install -g appium
	appium driver install uiautomator2

test:
	@test -x "$(VENV_DIR)/bin/python" || { echo "ERROR: virtual environment is missing. Run 'make python'."; exit 1; }
	@adb devices | awk '$$2 == "device" && $$1 ~ /^emulator-/ { found=1 } END { exit !found }' || { echo "ERROR: no ready Android emulator found. Run 'make run-emulator'."; exit 1; }
	@curl --silent --fail "$(APPIUM_URL)/status" >/dev/null || { echo "ERROR: Appium is not ready. Run 'make run-appium'."; exit 1; }
	@mkdir -p screenshots logs reports
	$(VENV_DIR)/bin/python test_clock.py 2>&1 | tee -a run.log

run-emulator: check-tools
	@if adb devices | awk '$$2 == "device" && $$1 ~ /^emulator-/ { found=1 } END { exit !found }'; then \
		echo "An Android emulator is already ready."; \
	else \
		echo "Starting AVD $(AVD_NAME)..."; \
		emulator -avd "$(AVD_NAME)" >"$(EMULATOR_LOG)" 2>&1 & \
		for i in $$(seq 1 60); do \
			if adb devices | awk '$$2 == "device" && $$1 ~ /^emulator-/ { found=1 } END { exit !found }' && [ "$$(adb shell getprop sys.boot_completed 2>/dev/null | tr -d '\r')" = "1" ]; then break; fi; \
			sleep 5; \
		done; \
		adb devices | awk '$$2 == "device" && $$1 ~ /^emulator-/ { found=1 } END { exit !found }' || { echo "ERROR: emulator did not become ready. See $(EMULATOR_LOG)."; exit 1; }; \
		echo "Emulator is ready."; \
	fi

run-appium:
	@command -v appium >/dev/null || { echo "ERROR: appium is not in PATH"; exit 1; }
	@if curl --silent --fail "$(APPIUM_URL)/status" >/dev/null; then \
		echo "Appium is already ready at $(APPIUM_URL)."; \
	else \
		echo "Starting Appium at $(APPIUM_URL)..."; \
		appium --address "$(APPIUM_HOST)" --port "$(APPIUM_PORT)" >"$(APPIUM_LOG)" 2>&1 & echo $$! >"$(APPIUM_PID_FILE)"; \
		for i in $$(seq 1 30); do curl --silent --fail "$(APPIUM_URL)/status" >/dev/null && break; sleep 1; done; \
		curl --silent --fail "$(APPIUM_URL)/status" >/dev/null || { echo "ERROR: Appium did not become ready. See $(APPIUM_LOG)."; exit 1; }; \
		echo "Appium is ready."; \
	fi

run-tests: run-emulator run-appium
	$(MAKE) test

status:
	@echo "ADB devices:"
	@if command -v adb >/dev/null; then adb devices; else echo "adb is not installed or not in PATH"; fi
	@echo "Appium:"
	@if ! command -v curl >/dev/null; then echo "curl is not installed"; elif curl --silent --fail "$(APPIUM_URL)/status" >/dev/null; then echo "ready ($(APPIUM_URL))"; else echo "not ready ($(APPIUM_URL))"; fi
	@echo "AVD: $(AVD_NAME)"

logs:
	@for file in "$(TEST_LOG)" run.log "$(APPIUM_LOG)" "$(EMULATOR_LOG)"; do \
		if [ -f "$$file" ]; then echo "--- $$file ---"; tail -50 "$$file"; fi; \
	done

screenshot:
	@test -n "$(NAME)" || { echo "Usage: make screenshot NAME=clock.png"; exit 1; }
	@mkdir -p screenshots
	adb exec-out screencap -p > "screenshots/$(NAME)"
	@echo "Saved screenshots/$(NAME)"

stop-all:
	@if [ -f "$(APPIUM_PID_FILE)" ]; then kill "$$(cat "$(APPIUM_PID_FILE)")" 2>/dev/null || true; rm -f "$(APPIUM_PID_FILE)"; fi
	@adb emu kill >/dev/null 2>&1 || true
	@echo "Requested Appium and emulator shutdown."

clean:
	rm -rf __pycache__ .pytest_cache
	rm -f *.pyc run.log
