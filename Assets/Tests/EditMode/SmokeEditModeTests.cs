using Arena.Combat;
using NUnit.Framework;

namespace Arena.Tests.EditMode
{
    /// <summary>
    /// Проверка проводки тестовой сборки, а не логики.
    /// Значения намеренно взяты вне контрольных таблиц спецификации —
    /// свои проверки по таблицам пиши отдельно, все до одной.
    /// </summary>
    public class SmokeEditModeTests
    {
        [Test]
        public void TestAssembly_IsWiredUp()
        {
            int damage = DamageCalculator.Calculate(200, 100);
            Assert.That(damage, Is.EqualTo(100));
        }
    }
}
