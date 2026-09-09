using UnityEngine;

namespace Arena.Combat
{
    public struct DamageRequest
    {
        public int BaseDamage;
        public float BonusPercent;
        public int Armor;
        public bool IsCritical;
        public float CriticalMultiplier;
    }

    public static class DamageCalculator
    {
        public static int Calculate(DamageRequest request)
        {
            float raw = request.BaseDamage * (1f + request.BonusPercent / 100f);

            float armorFactor = 100f / (100f + request.Armor);
            float afterArmor = raw * armorFactor;

            if (request.IsCritical)
            {
                afterArmor *= request.CriticalMultiplier;
            }

            return Mathf.FloorToInt(afterArmor);
        }

        public static int Calculate(int baseDamage, int armor)
        {
            return Calculate(new DamageRequest
            {
                BaseDamage = baseDamage,
                BonusPercent = 0f,
                Armor = armor,
                IsCritical = false,
                CriticalMultiplier = 1f
            });
        }
    }
}
