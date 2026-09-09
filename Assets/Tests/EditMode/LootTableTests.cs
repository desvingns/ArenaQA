using System;
using Arena.Economy;
using NUnit.Framework;

namespace Arena.Tests.EditMode
{
    public class LootTableTests
    {
        private static LootTable EqualWeights() => new LootTable(new[]
        {
            new LootEntry("A", 1), new LootEntry("B", 1), new LootEntry("C", 1)
        });
        private static LootTable LeadingZero() => new LootTable(new[]
        {
            new LootEntry("A", 0), new LootEntry("B", 1)
        });
        private static LootTable Rarities() => new LootTable(new[]
        {
            new LootEntry("common", 70), new LootEntry("rare", 25), new LootEntry("epic", 5)
        });

        // §3: complete control tables, with exceptions as separate cases.
        [TestCase(0, "A")]
        [TestCase(1, "B")]
        [TestCase(2, "C")]
        public void Pick_EqualWeights_ControlRowReturnsExpectedId(int roll, string expected)
        {
            Assert.That(EqualWeights().Pick(roll), Is.EqualTo(expected));
        }

        [TestCase(3)]
        [TestCase(-1)]
        [TestCase(int.MinValue)]
        [TestCase(int.MaxValue)]
        public void Pick_EqualWeights_OutOfRangeThrows(int roll)
        {
            Assert.Throws<ArgumentOutOfRangeException>(() => EqualWeights().Pick(roll));
        }

        [Test]
        public void Pick_LeadingZeroWeight_RollZeroReturnsB()
        {
            Assert.That(LeadingZero().Pick(0), Is.EqualTo("B"));
        }

        [Test]
        public void Pick_LeadingZeroWeight_RollEqualToTotalThrows()
        {
            Assert.Throws<ArgumentOutOfRangeException>(() => LeadingZero().Pick(1));
        }

        [TestCase(0, "common")]
        [TestCase(69, "common")]
        [TestCase(70, "rare")]
        [TestCase(94, "rare")]
        [TestCase(95, "epic")]
        [TestCase(99, "epic")]
        public void Pick_RarityWeights_ControlRowReturnsExpectedId(int roll, string expected)
        {
            Assert.That(Rarities().Pick(roll), Is.EqualTo(expected));
        }

        [TestCase(-1)]
        [TestCase(100)]
        public void Pick_RarityWeights_OutOfRangeThrows(int roll)
        {
            Assert.Throws<ArgumentOutOfRangeException>(() => Rarities().Pick(roll));
        }

        [TestCase("equal", 3)]
        [TestCase("zero", 1)]
        [TestCase("rarity", 100)]
        public void TotalWeight_ControlTable_EqualsSum(string table, int expected)
        {
            LootTable sut = table == "equal" ? EqualWeights() : table == "zero" ? LeadingZero() : Rarities();
            Assert.That(sut.TotalWeight, Is.EqualTo(expected));
        }

        [TestCase(0, "A")]
        [TestCase(1, "B")]
        public void Pick_ZeroWeightsInMiddleAndEnd_NeverSelectsZeroEntry(int roll, string expected)
        {
            var table = new LootTable(new[]
            {
                new LootEntry("A", 1), new LootEntry("zero-middle", 0),
                new LootEntry("B", 1), new LootEntry("zero-end", 0)
            });
            Assert.That(table.Pick(roll), Is.EqualTo(expected));
        }
    }
}
