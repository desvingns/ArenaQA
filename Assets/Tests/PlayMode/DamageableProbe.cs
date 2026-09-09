using System.Collections.Generic;
using Arena.Combat;
using UnityEngine;

namespace Arena.Tests.PlayMode
{
    // Records the interface contract without Health's invulnerability/death behavior masking hits.
    public sealed class DamageableProbe : MonoBehaviour, IDamageable
    {
        public bool IsDead => false;
        public readonly List<int> ReceivedDamage = new List<int>();
        public void TakeDamage(int amount) => ReceivedDamage.Add(amount);
    }
}
