using Arena.Combat;
using NUnit.Framework;

namespace Arena.Tests.EditMode
{
    public class DamageCalculatorTests
    {
        // §1: all nine rows of the specification, including all negative armor rows.
        [TestCase(100, 0f, 0, false, 1f, 100)]
        [TestCase(100, 0f, 100, false, 1f, 50)]
        [TestCase(100, 50f, 50, true, 2f, 200)]
        [TestCase(10, 0f, 900, false, 1f, 1)]
        [TestCase(5, 0f, 900, false, 1f, 1)]
        [TestCase(1, 0f, 400, false, 1f, 1)]
        [TestCase(100, 0f, -50, false, 1f, 100)]
        [TestCase(100, 0f, -100, false, 1f, 100)]
        [TestCase(100, 0f, -1000, false, 1f, 100)]
        public void Calculate_SpecificationRow_ReturnsExpectedDamage(
            int baseDamage, float bonus, int armor, bool critical, float multiplier, int expected)
        {
            var request = new DamageRequest
            {
                BaseDamage = baseDamage, BonusPercent = bonus, Armor = armor,
                IsCritical = critical, CriticalMultiplier = multiplier
            };
            Assert.That(DamageCalculator.Calculate(request), Is.EqualTo(expected));
        }

        // §1.1, §1.4-6: literal oracles, deliberately no copy of the production formula.
        [TestCase(10, 25f, 0, false, 9f, 12, TestName = "Calculate_Bonus25Percent_Floors12Point5To12")]
        [TestCase(11, 0f, 0, true, 1.5f, 16, TestName = "Calculate_Critical_Floors16Point5To16")]
        [TestCase(10, 0f, 0, false, 99f, 10, TestName = "Calculate_NonCritical_IgnoresCriticalMultiplier")]
        [TestCase(1, 0f, 100, true, 4f, 2, TestName = "Calculate_FractionBeforeCritical_DoesNotRoundOrClampEarly")]
        [TestCase(0, 0f, 0, false, 1f, 1, TestName = "Calculate_ZeroBaseDamage_ReturnsMinimumOne")]
        [TestCase(-10, 0f, 0, false, 1f, 1, TestName = "Calculate_NegativeBaseDamage_ReturnsMinimumOne")]
        [TestCase(-100, -200f, 0, false, 1f, 100, TestName = "Calculate_NegativeBaseAndBonus_AppliesMinimumOnlyAtEnd")]
        [TestCase(100, -100f, 0, false, 1f, 1, TestName = "Calculate_Minus100PercentBonus_ReturnsMinimumOne")]
        [TestCase(100, 0f, 0, true, 0f, 1, TestName = "Calculate_ZeroCriticalMultiplier_ReturnsMinimumOne")]
        [TestCase(100, 0f, int.MinValue, false, 1f, 100, TestName = "Calculate_MinIntArmor_IsClampedToZero")]
        [TestCase(16777217, 0f, 0, false, 1f, 16777217, TestName = "Calculate_LargeIntegerWithoutModifiers_PreservesDamage")]
        [TestCase(int.MaxValue, 0f, 100, false, 1f, 1073741823, TestName = "Calculate_MaxIntBaseWith100Armor_FloorsExactHalf")]
        public void Calculate_AdditionalBoundary_ReturnsExpectedDamage(
            int baseDamage, float bonus, int armor, bool critical, float multiplier, int expected)
        {
            Calculate_SpecificationRow_ReturnsExpectedDamage(baseDamage, bonus, armor, critical, multiplier, expected);
        }

        [TestCase(100, 0, 100)]
        [TestCase(100, 100, 50)]
        [TestCase(10, 900, 1)]
        [TestCase(5, 900, 1)]
        [TestCase(1, 400, 1)]
        [TestCase(100, -50, 100)]
        [TestCase(100, -100, 100)]
        [TestCase(100, -1000, 100)]
        public void Calculate_TwoArgumentOverload_UsesNoBonusAndNoCritical(int damage, int armor, int expected)
        {
            Assert.That(DamageCalculator.Calculate(damage, armor), Is.EqualTo(expected));
        }
    }
}
