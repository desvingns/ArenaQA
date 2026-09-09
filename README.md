# ArenaQA - решение тестового задания

Проект с тестами по исходной спецификации. **Часть тестов намеренно красная:** проверяемые требования расходятся с реализацией, исправления игры не вносились.

## Состав

- `Assets/Tests/EditMode/` - DamageCalculator и LootTable.
- `Assets/Tests/PlayMode/` - Health, Projectile, WaveSpawner и вспомогательные тестовые классы.
- [BUGS.md](BUGS.md) - 12 причин расхождений, воспроизведение и связанные тесты.
- [REPORT.md](REPORT.md) - процесс, инструменты, участие AI, ограничения.
- [COVERAGE.md](COVERAGE.md) - покрытие каждого пункта и контрольных таблиц.
- [TEST_RESULTS.md](TEST_RESULTS.md), `Evidence/` - подтверждённые результаты и XML.
- `run-tests.ps1` - последовательный batchmode-прогон двух тестовых сборок.
- `.github/workflows/arena-tests.yml` - GitHub Actions + GameCI для EditMode и PlayMode.

## Запуск

По заданию используйте **Unity 6000.3.11f1**. Откройте эту папку как проект, затем `Window > General > Test Runner`, отдельно запустите EditMode и PlayMode.

Либо из PowerShell в корне проекта:

```powershell
.\run-tests.ps1
.\run-tests.ps1 -Mode PlayMode
.\run-tests.ps1 -RandomOrderSeed 20260909
```

Если Editor установлен в другом месте:

```powershell
.\run-tests.ps1 -UnityPath 'D:\Unity\6000.3.11f1\Editor\Unity.exe'
```

XML и логи каждого запуска сохраняются в новой папке `TestResults/`. Скрипт возвращает ненулевой код при красных тестах; это не скрывается. Ошибки запуска/отсутствие XML останавливают скрипт. `-quit` не используется, чтобы Editor не завершился раньше тестового раннера.

## Проверенный результат

На установленной **Unity 6000.6.0f1 / UTF 1.8.0** в отдельной копии: 96 кейсов, 51 passed, 45 failed, 0 skipped. Повтор с перемешанным порядком дал тот же результат для каждого кейса. Сдаваемые настройки сохраняют Unity 6000.3.11f1 / UTF 1.6.0; прогон на этой точной паре версий ещё не выполнен.

Если проверяете на более новой Unity, используйте отдельную копию: Editor может обновить ProjectSettings и зависимости. Исходные игровые скрипты и файлы сборок тестов в этой поставке сохранены.

## GitHub Actions / GameCI

Workflow запускается на `push`, `pull_request` и вручную через `workflow_dispatch`. Два режима тестов выполняются параллельно, `Library` кэшируется, XML и логи загружаются как artifacts, а результаты отображаются в GitHub Checks.

Перед первым запуском в настройках репозитория добавьте secrets `UNITY_LICENSE`, `UNITY_EMAIL` и `UNITY_PASSWORD` для Unity Personal. `unityVersion: auto` берёт версию из `ProjectSettings/ProjectVersion.txt`.

Текущий набор содержит намеренно красные проверки найденных дефектов, поэтому workflow завершится с ошибкой до исправления причин из `BUGS.md`; при этом artifacts сохраняются.
