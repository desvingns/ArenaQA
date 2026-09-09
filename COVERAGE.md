# Покрытие спецификации

Источник: «Часть 2 ArenaQA-Тестовое-задание.pdf». Ожидаемые значения взяты из спецификации, а не из текущей реализации. Ниже указаны методы; параметры NUnit раскрывает в отдельные тест-кейсы.

## EditMode

| Требование | Тесты |
|---|---|
| §1: все 9 строк контрольной таблицы | `DamageCalculatorTests.Calculate_SpecificationRow_ReturnsExpectedDamage` - 9 случаев |
| §1.1: бонус в процентах | контрольная строка с бонусом 50%; `Calculate_Bonus25Percent_Floors12Point5To12` |
| §1.2: отрицательная броня считается нулём | три контрольные строки; `Calculate_MinIntArmor_IsClampedToZero` |
| §1.3: снижение бронёй | контрольные строки с Armor 100, 50, 900, 400 |
| §1.4: крит после брони | контрольная строка с критом; `Calculate_Critical_Floors16Point5To16`, `Calculate_NonCritical_IgnoresCriticalMultiplier` |
| §1.5: округление вниз только после множителей | `Calculate_FractionBeforeCritical_DoesNotRoundOrClampEarly`, случаи 12.5 и 16.5 |
| §1.6: минимум 1 только в конце | контрольные строки 5/900 и 1/400; случаи нулевого/отрицательного BaseDamage, бонуса -100%, множителя крита 0, сочетания отрицательных BaseDamage и BonusPercent |
| §1: большие значения с представимым итогом | `Calculate_LargeIntegerWithoutModifiers_PreservesDamage`, `Calculate_MaxIntBaseWith100Armor_FloorsExactHalf` |
| §1: перегрузка `(int, int)` | `Calculate_TwoArgumentOverload_UsesNoBonusAndNoCritical` - 8 некритических строк таблицы |
| §3.1: A(1), B(1), C(1), roll 0/1/2 | `Pick_EqualWeights_ControlRowReturnsExpectedId` |
| §3.2: A(0), B(1), roll 0 | `Pick_LeadingZeroWeight_RollZeroReturnsB` |
| §3.1: common(70), rare(25), epic(5), все 6 строк | `Pick_RarityWeights_ControlRowReturnsExpectedId` |
| §3.3: все 3 исключения из контрольных таблиц | `Pick_EqualWeights_OutOfRangeThrows` (3, -1); `Pick_LeadingZeroWeight_RollEqualToTotalThrows` (1) |
| §3.3: дополнительные внешние границы | `Pick_EqualWeights_OutOfRangeThrows` (int.MinValue, int.MaxValue); `Pick_RarityWeights_OutOfRangeThrows` (-1, 100) |
| §3.4: TotalWeight 3/1/100 | `TotalWeight_ControlTable_EqualsSum` - 3 случая |
| §3.2: нулевые веса в середине/конце | `Pick_ZeroWeightsInMiddleAndEnd_NeverSelectsZeroEntry` |

Контрольные таблицы перенесены полностью: DamageCalculator - 9 строк; LootTable - 5 + 2 + 6 = 13 строк; отдельно 3 проверки TotalWeight.

## PlayMode

| Требование | Тесты |
|---|---|
| §2: Configure | `Configure_SetsMaxAndCurrentHealth_UnitIsAlive` |
| §2.1 | `TakeDamage_NonPositive_DoesNotChangeHealthOrEmitEvents` (0, -1, int.MinValue) |
| §2.2 | `TakeDamage_AlreadyDead_IsCompletelyIgnored` |
| §2.3 | `TakeDamage_InsideWindow_IgnoresDamageAndEvents_ThenAcceptsAfterExpiry`, `TakeDamage_IgnoredHitDoesNotRestartWindow_AcceptedHitDoes` |
| §2.4 | `TakeDamage_NewUnitInCreationFrame_IsImmediatelyVulnerable` (0, 0.5, 100000 секунд) |
| §2.5-6 | `TakeDamage_Lethal_ClampsToZeroAndReportsOnlyRemainingHp` (точный, избыточный, int.MaxValue урон); события обычного урона проверяются в тесте окна |
| §2.7 | `TakeDamage_MultipleHitsAfterDeath_EmitsDiedExactlyOnce`; lethal-тест проверяет Current/IsDead внутри обработчика Died |
| §2.8 | `Heal_NonPositive_IsIgnored` |
| §2.9 | `Heal_DeadUnit_DoesNotResurrect` |
| §2.10 и смысл положительного лечения | `Heal_PositiveAmount_RestoresHpUpToMaximum` (недолечивание, точно до максимума, избыток, int.MaxValue) |
| §2.11 | `Heal_DuringInvulnerability_DoesNotShortenOrExtendWindow` |
| §5.1 | `Flight_RotatedProjectile_MovesAlongLocalZAtSpeedAndAccumulatesTime` - позиция и Elapsed на каждом из 6 кадров |
| §5.2 | `Lifetime_ExpiresAtFirstFrameReachingLimit_NotBefore` |
| §5.3 | прямые вызовы HandleHit; `PhysicsTrigger_DamageableCollider_DelegatesHitToDamageable` - настоящий физический trigger |
| §5.4 | `HandleHit_Damageable_ReceivesConfiguredDamageAndSetsHasHit`, `HandleHit_FirstDamageable_EmitsExpiredSynchronously` |
| §5.4-5 | `HandleHit_RepeatedSameTarget_AppliesDamageOnlyOnce`, `HandleHit_SecondTargetInSameFlight_DoesNotDamageSecondTarget` |
| §5.6 | `HandleHit_NonDamageable_IsIgnoredAndDoesNotConsumeFlight` |
| §5.7 через контракт §4 | `Pool_ReusedProjectile_ResetsElapsedToZero`, `Pool_ReusedProjectile_ResetsHasHitAndCanDamageAgain`, `Pool_ThreeFlights_EachReceivesFullLifetime` |
| §6.1 | `ControlRun_WaveStarted_EmitsIndicesZeroOneTwo`; фабрика проверяет, что событие пришло раньше спавна |
| §6.2 | `ControlRun_WaveSizes_AreTwoThreeFour`, `ControlRun_CreatesNineEnemies`, `SpawnInterval_SeparatesFactoryCallsWithinWave` |
| §6.3 | `ControlRun_WaveCompleted_EmitsIndicesZeroOneTwo`, `AliveCount_TracksSpawnAndDeaths_CompletionWaitsForLastEnemy`; каждый обработчик WaveCompleted проверяет смерть врагов |
| §6.4 | `NextWave_StartsOnlyAfterCompletionAndInterWaveDelay` |
| §6: конец прогона | `ControlRun_AllCompletedOnce_AndIsRunningIsFalseAtNotification` |
| §6: AliveCount | `AliveCount_TracksSpawnAndDeaths_CompletionWaitsForLastEnemy` - спавн, первая смерть, ожидание живого, последняя смерть |
| §6: нижняя положительная граница WaveCount | `SingleWave_RunActuallySpawnsAndCompletesItsOnlyWave` |

## Границы вывода

- `Arena.Game` не тестируется и не изменяется. Отдельных тестов `SimplePool<T>` нет: пул используется только для проверки контракта снаряда.
- Точное совпадение времени callback с произвольной дробной секундой невозможно на кадровом движке. Для кадрозависимых проверок зафиксирован шаг 1/60 секунды; допускается округление ожидания до кадра. Для неуязвимости проверки выполняются с запасом по обе стороны границы, без требования конкретного FPS устройства.
- Невалидные конфигурации, NaN/Infinity, null-записи/отрицательные веса, отрицательные времена и итоговый урон вне int требуют уточнения контракта; произвольные ожидаемые исключения для них не добавлены.
- Число Expired после истечения Lifetime без возврата владельцем в пул не проверяется: §5.2 явно оставляет его неопределённым.
