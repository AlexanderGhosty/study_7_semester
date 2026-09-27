# Лабораторная работа №7: тестирование мобильного приложения

Проект демонстрирует ручное и автоматизированное тестирование стандартного приложения **Clock** на Android-эмуляторе с помощью Appium и Python.

## Что есть в проекте

- `test_clock.py` — основной Appium-сценарий: запуск Clock, поиск элементов, нажатия, жест и проверка ориентации;
- `mobile_testing_checklist.md` — чек-лист ручного тестирования;
- `test_environment.py` — проверка Python, Appium, Android SDK и структуры проекта;
- `test_driver_creation.py` — проверка создания Appium-драйвера;
- `test_config.py` — конфигурация окружения;
- `requirements.txt` — Python-зависимости;
- `Makefile` — команды установки, запуска, диагностики и сохранения скриншотов;
- `setup_environment.sh` — вспомогательная установка для Ubuntu/Debian;
- `screenshots/` — скриншоты для отчёта (создаётся при необходимости);
- `clock_simple_test_results.log` и `run.log` — результаты запуска тестов.

Файлы `test_notes_app.py`, `test_calculator_app.py` и `run_all_tests.py` в текущей версии проекта отсутствуют и не используются.

## Требования

Makefile поддерживает Ubuntu/Debian (`apt-get`) и Arch/CachyOS (`pacman`). Android system image и AVD не создаются автоматически: нужно явно выбрать образ и создать виртуальное устройство.

Нужны:

- Python 3 и `venv`;
- Java 17 или совместимая JDK;
- Node.js и npm;
- Android SDK: `adb`, `emulator`, `sdkmanager`, `avdmanager`;
- Android AVD;
- Appium 2 и драйвер `uiautomator2`;
- графическое окружение, если нужны скриншоты эмулятора.

Рекомендуется не запускать установку от root. `sudo` используется только отдельными командами установки системных пакетов и Appium.

## Установка

### Ubuntu/Debian или Arch/CachyOS

Из каталога проекта:

```bash
cd /home/alex/Projects/study_7_semester/qa/l7
make setup
```

`make setup` устанавливает системные пакеты через доступный пакетный менеджер, создаёт `.venv`, устанавливает Python-зависимости, Appium и драйвер UiAutomator2.

Если Android SDK и AVD устанавливались через Android Studio, достаточно выполнить:

```bash
make python
make appium
```

На Arch/CachyOS команда `make setup` использует `pacman`. Если отдельный пакет Android SDK отсутствует в подключённых репозиториях, установите Android Studio или соответствующие SDK-пакеты вручную, затем выполните:

```bash
make python
make appium
```

## Создание и проверка AVD

Пример команд Android SDK:

```bash
sdkmanager "platform-tools" "emulator" \
  "platforms;android-34" \
  "system-images;android-34;google_apis;x86_64"

yes | sdkmanager --licenses
avdmanager create avd \
  -n Medium_Phone_API_36.0 \
  -k "system-images;android-34;google_apis;x86_64"
```

Проверьте наличие AVD и инструментов:

```bash
make check-tools
emulator -list-avds
```

Если используется другое имя AVD:

```bash
make check-tools AVD_NAME=My_AVD
```

## Запуск

Для получения скриншотов запускайте эмулятор с графическим окном.

### Полный запуск

```bash
make run-tests
```

Эта команда:

1. проверяет инструменты и AVD;
2. запускает эмулятор и ждёт `sys.boot_completed=1`;
3. запускает Appium на `http://127.0.0.1:4723`;
4. запускает `test_clock.py`;
5. сохраняет вывод в `run.log`.

### Пошаговый запуск

```bash
make run-emulator
make run-appium
make test
```

Проверить состояние можно в любой момент:

```bash
make status
adb devices
curl http://127.0.0.1:4723/status
```

В `adb devices` устройство должно иметь состояние `device`, а не `offline` или `unauthorized`.

## Скриншоты и материалы для отчёта

Снимок текущего экрана эмулятора:

```bash
make screenshot NAME=01_clock_main.png
```

Файл появится в `screenshots/01_clock_main.png`. Можно сохранить отдельные состояния:

```bash
make screenshot NAME=02_alarm_tab.png
make screenshot NAME=03_landscape.png
make screenshot NAME=04_portrait.png
```

Для отчёта полезно сохранить:

- экран Clock после запуска;
- экран после нажатия на Alarm;
- экран в альбомной ориентации;
- экран после возврата в портретную ориентацию;
- вывод тестов в терминале;
- `run.log` и `clock_simple_test_results.log`.

При необходимости можно записать видео:

```bash
adb shell screenrecord /sdcard/l7_test.mp4
# остановить Ctrl+C
adb pull /sdcard/l7_test.mp4 screenshots/l7_test.mp4
```

## Ручное тестирование

Заполните [mobile_testing_checklist.md](mobile_testing_checklist.md) после фактической проверки Clock. В таблице результатов укажите ожидаемый результат, фактическое наблюдение, статус и комментарий.

Минимально зафиксируйте:

- запуск и закрытие приложения;
- тап и долгое нажатие;
- переход на вкладку Alarm;
- поворот портретная ↔ альбомная ориентация;
- блокировку/разблокировку экрана;
- переключение между приложениями;
- обработку уведомлений;
- поведение после очистки приложения из списка последних;
- стабильность приложения при длительной работе.

Не отмечайте пункт как пройденный, если он не проверялся на эмуляторе или устройстве.

## Диагностика

```bash
make logs
```

Логи сервисов:

- `appium.log` — Appium;
- `emulator.log` — запуск эмулятора;
- `run.log` — вывод команды `make test`;
- `clock_simple_test_results.log` — лог Python-сценария.

Если тест не находит Clock, убедитесь, что эмулятор находится на домашнем экране и значок `Clock` доступен. Сценарий запускает приложение через поиск значка в Launcher, а не через фиксированные `appPackage` и `appActivity`.

Остановить сервисы:

```bash
make stop-all
```

Очистить только локальные кэши и вывод запуска:

```bash
make clean
```

## Команды Makefile

```text
make help
make check-tools
make setup
make python
make appium
make run-emulator
make run-appium
make test
make run-tests
make status
make logs
make screenshot NAME=name.png
make stop-all
make clean
```
