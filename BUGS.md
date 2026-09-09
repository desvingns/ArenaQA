# Расхождения со спецификацией ArenaQA

Источник: «Часть 2 ArenaQA-Тестовое-задание.pdf». Одна запись - одна причина. Несколько падений параметризованного теста не считаются отдельными багами. Игровой код не исправлен.

Статус: все 12 причин подтверждены тестами на Unity 6000.6.0f1 / UTF 1.8.0, включая повторный прогон с перемешанным порядком. 45 падений сгруппированы по причинам. Подробности и XML - в [TEST_RESULTS.md](TEST_RESULTS.md). На исходной Unity 6000.3.11f1 прогон не выполнен. Severity относится к боевой логике, не к демо-HUD; Critical-дефекты, блокирующие запуск, не установлены.

## 1. Последняя волна прогона не запускается

| Атрибут | Значение |
|---|---|
| **Где** | `WaveSpawner.Run` |
| **Пункт спецификации** | §6.1-4 и контрольный пример |
| **Ожидаемое** | Три волны имеют размеры 2, 3, 4; события идут с индексами 0, 1, 2; всего 9 врагов. |
| **Фактическое** | Условие wave < _waveCount - 1 запускает только волны 0 и 1: 5 врагов. При WaveCount=1 нет спавнов. |
| **Воспроизведение** | WaveCount=3, BaseEnemiesPerWave=2, EnemiesIncrementPerWave=1; фабрика создаёт живые Health; убивать их до завершения прогона. |
| **Тест** | ControlRun_WaveSizes_AreTwoThreeFour; ControlRun_WaveStarted_EmitsIndicesZeroOneTwo; ControlRun_WaveCompleted_EmitsIndicesZeroOneTwo; ControlRun_CreatesNineEnemies; SingleWave_RunActuallySpawnsAndCompletesItsOnlyWave |
| **Severity** | Major - прогон пропускает обязательную волну, а одиночная волна вообще не начинается. |

## 2. Снаряд наносит урон повторно и нескольким целям

| Атрибут | Значение |
|---|---|
| **Где** | `Projectile.HandleHit` |
| **Пункт спецификации** | §5.4-5 |
| **Ожидаемое** | После первого попадания повторные вызовы в текущем полёте игнорируются; ровно одна цель получает Damage. |
| **Фактическое** | Нет проверки HasHit перед TakeDamage. Каждый вызов по IDamageable повторно наносит урон. |
| **Воспроизведение** | Damage=17; HandleHit(A), затем HandleHit(A) или HandleHit(B). Цели реализуют IDamageable и записывают вызовы. |
| **Тест** | HandleHit_RepeatedSameTarget_AppliesDamageOnlyOnce; HandleHit_SecondTargetInSameFlight_DoesNotDamageSecondTarget |
| **Severity** | Major - один полёт умножает урон и поражает лишние цели. |

## 3. Попадание снаряда не вызывает Expired

| Атрибут | Значение |
|---|---|
| **Где** | `Projectile.HandleHit` |
| **Пункт спецификации** | §5.4 |
| **Ожидаемое** | После урона HasHit=true и Expired вызывается немедленно, до возврата HandleHit. |
| **Фактическое** | Урон и HasHit обновляются, но вызов Expired отсутствует. |
| **Воспроизведение** | Подписаться на Expired; вызвать HandleHit для IDamageable; проверить callback сразу после вызова. |
| **Тест** | HandleHit_FirstDamageable_EmitsExpiredSynchronously |
| **Severity** | Major - владелец не получает уведомление для возврата снаряда в пул после попадания. |

## 4. Снаряд из пула сохраняет время прошлого полёта

| Атрибут | Значение |
|---|---|
| **Где** | `Projectile.OnSpawn` |
| **Пункт спецификации** | §5.7; контракт §4.4 |
| **Ожидаемое** | При каждом Get Elapsed=0, новый полёт длится полный Lifetime. |
| **Фактическое** | OnSpawn сбрасывает только HasHit. _elapsed сохраняется; после полного первого полёта повторный истекает на ближайшем Update. |
| **Воспроизведение** | SimplePool.Get, накопить Elapsed>0, Release, Get того же экземпляра. Отдельно повторить после естественного истечения Lifetime. |
| **Тест** | Pool_ReusedProjectile_ResetsElapsedToZero; Pool_ThreeFlights_EachReceivesFullLifetime |
| **Severity** | Major - переиспользуемые снаряды теряют положенную длительность полёта. |

## 5. Урон мёртвому повторно вызывает события смерти

| Атрибут | Значение |
|---|---|
| **Где** | `Health.TakeDamage` |
| **Пункт спецификации** | §2.2, §2.7 |
| **Ожидаемое** | Урон по мёртвому полностью игнорируется; Died один раз за смерть, повторного Damaged нет. |
| **Фактическое** | Нет выхода для IsDead. При нулевой неуязвимости повторный удар даёт Damaged(0) и ещё один Died. |
| **Воспроизведение** | Configure(3,0), TakeDamage(3), TakeDamage(10). |
| **Тест** | TakeDamage_AlreadyDead_IsCompletelyIgnored; TakeDamage_MultipleHitsAfterDeath_EmitsDiedExactlyOnce |
| **Severity** | Major - подписчики повторно обрабатывают одну смерть; возможны повторные побочные действия. |

## 6. Новый Health неуязвим в кадре создания

| Атрибут | Значение |
|---|---|
| **Где** | `Health.Awake / TakeDamage` |
| **Пункт спецификации** | §2.4 |
| **Ожидаемое** | Первый урон проходит в кадре создания при любой длительности неуязвимости. |
| **Фактическое** | Awake записывает Time.time в _lastDamageTime, как будто урон уже получен. Первый удар блокируется при положительной длительности. |
| **Воспроизведение** | Создать Health, Configure(100,0.5), в том же кадре TakeDamage(10): ожидается 90 HP, остаётся 100. |
| **Тест** | TakeDamage_NewUnitInCreationFrame_IsImmediatelyVulnerable (0.5 и 100000 секунд) |
| **Severity** | Major - новый юнит игнорирует допустимый первый удар. |

## 7. Большое положительное лечение переполняет HP

| Атрибут | Значение |
|---|---|
| **Где** | `Health.Heal` |
| **Пункт спецификации** | §2, Heal; §2.10 (ограничение MaxHealth) |
| **Ожидаемое** | При Current=80, MaxHealth=100 и Heal(int.MaxValue) здоровье восстанавливается до 100. |
| **Фактическое** | Current + amount переполняет int до Min: в unchecked режиме получается -2147483569 и IsDead=true. |
| **Воспроизведение** | Configure(100,0), TakeDamage(20), Heal(2147483647). |
| **Тест** | Heal_PositiveAmount_RestoresHpUpToMaximum (int.MaxValue,100) |
| **Severity** | Major - положительное лечение переводит живого юнита в состояние смерти. Верхняя граница положительного amount в контракте не задана. |

## 8. Отрицательная броня меняет урон вместо ограничения нулём

| Атрибут | Значение |
|---|---|
| **Где** | `DamageCalculator.Calculate(DamageRequest)` |
| **Пункт спецификации** | §1.2; контрольные строки с отрицательной Armor |
| **Ожидаемое** | Calculate(100,-50), Calculate(100,-100), Calculate(100,-1000) возвращают 100. |
| **Фактическое** | Armor используется без max(0,Armor): -50 даёт 200, -100 приводит к делению на ноль в float, -1000 даёт -12. В выполненном прогоне Armor=-100 дал int.MinValue; этот результат не принят за ожидаемое поведение теста. |
| **Воспроизведение** | Передать любой из трёх наборов; отрицательная броня допустима как результат пробивания. |
| **Тест** | Calculate_SpecificationRow_ReturnsExpectedDamage (отрицательная Armor); Calculate_TwoArgumentOverload_UsesNoBonusAndNoCritical; Calculate_MinIntArmor_IsClampedToZero |
| **Severity** | Major - штатный эффект пробивания даёт увеличенный, отрицательный или непредусмотренный урон. |

## 9. Итоговый урон не ограничен минимумом 1

| Атрибут | Значение |
|---|---|
| **Где** | `DamageCalculator.Calculate(DamageRequest)` |
| **Пункт спецификации** | §1.6 |
| **Ожидаемое** | После всех вычислений и округления итог не меньше 1, включая BaseDamage<=0. |
| **Фактическое** | FloorToInt возвращается без финального ограничения: Calculate(5,900) и Calculate(1,400) дают 0, Calculate(-10,0) даёт -10. |
| **Воспроизведение** | Calculate(5,900), Calculate(1,400), Calculate(0,0), Calculate(-10,0). |
| **Тест** | Calculate_SpecificationRow_ReturnsExpectedDamage (5/900,1/400); Calculate_ZeroBaseDamage_ReturnsMinimumOne; Calculate_NegativeBaseDamage_ReturnsMinimumOne; случаи нулевого крита и бонуса -100% |
| **Severity** | Major - попадание не гарантирует обязательное снятие хотя бы 1 HP. |

## 10. Граница диапазона лута включена в предыдущую запись

| Атрибут | Значение |
|---|---|
| **Где** | `LootTable.Pick` |
| **Пункт спецификации** | §3.1-2 |
| **Ожидаемое** | Для 1/1/1 roll=1 даёт B; для 70/25/5 roll=70 даёт rare, roll=95 даёт epic; запись веса 0 не выпадает. |
| **Фактическое** | Сравнение roll <= cumulative включает верхнюю границу. В примерах возвращаются A, common, rare; для A(0),B(1) roll=0 возвращает A. |
| **Воспроизведение** | Создать указанную таблицу и передать граничный roll. |
| **Тест** | Pick_EqualWeights_ControlRowReturnsExpectedId; Pick_RarityWeights_ControlRowReturnsExpectedId; Pick_LeadingZeroWeight_RollZeroReturnsB; Pick_ZeroWeightsInMiddleAndEnd_NeverSelectsZeroEntry |
| **Severity** | Major - искажено распределение наград, может выпасть запись нулевого веса. Одна ошибка сравнения объединяет эти проявления. |

## 11. Недопустимый roll не вызывает исключение

| Атрибут | Значение |
|---|---|
| **Где** | `LootTable.Pick` |
| **Пункт спецификации** | §3.3 |
| **Ожидаемое** | При roll<0 и roll>=TotalWeight бросается ArgumentOutOfRangeException. |
| **Фактическое** | Валидации нет: для A(1),B(1),C(1) Pick(-1) возвращает A, Pick(3) возвращает C; большой roll попадает в fallback последней записи. |
| **Воспроизведение** | A(1),B(1),C(1): Pick(-1), Pick(3), Pick(int.MaxValue); A(0),B(1): Pick(1). |
| **Тест** | Pick_EqualWeights_OutOfRangeThrows; Pick_LeadingZeroWeight_RollEqualToTotalThrows; Pick_RarityWeights_OutOfRangeThrows |
| **Severity** | Major - вместо явной ошибки входа молча выдаётся награда. Исправление границы диапазона не заменяет требуемую валидацию. |

## 12. Большие целые теряют точность до итогового округления

| Атрибут | Значение |
|---|---|
| **Где** | `DamageCalculator.Calculate(DamageRequest)` |
| **Пункт спецификации** | §1.1-5: вычисления и округление в конце |
| **Ожидаемое** | Без модификаторов 16777217 сохраняется; BaseDamage=int.MaxValue, Armor=100 даёт floor(2147483647/2)=1073741823. |
| **Фактическое** | Промежуточный float округляет исходный int: первый пример даёт 16777216, второй - 1073741824. |
| **Воспроизведение** | Указанные BaseDamage/Armor; BonusPercent=0, IsCritical=false. Оба ожидаемых результата помещаются в int. |
| **Тест** | Calculate_LargeIntegerWithoutModifiers_PreservesDamage; Calculate_MaxIntBaseWith100Armor_FloorsExactHalf |
| **Severity** | Minor - погрешность больших значений при допустимом входе; достижимость таких значений в игровом балансе требует уточнения. |
