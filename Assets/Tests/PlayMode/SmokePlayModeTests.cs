using System.Collections;
using Arena.Combat;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace Arena.Tests.PlayMode
{
    /// <summary>
    /// Проверка проводки тестовой сборки, а не логики.
    /// Заодно образец уборки в PlayMode: Destroy отложен до конца кадра,
    /// поэтому ждём кадр в [UnityTearDown], иначе объект утечёт в следующий тест.
    /// </summary>
    public class SmokePlayModeTests
    {
        private GameObject _go;

        [UnityTearDown]
        public IEnumerator TearDown()
        {
            if (_go != null)
            {
                Object.Destroy(_go);
                yield return null;
            }
        }

        [UnityTest]
        public IEnumerator TestAssembly_IsWiredUp()
        {
            _go = new GameObject("Unit");
            var health = _go.AddComponent<Health>();
            health.Configure(50, 0f);

            yield return null;

            Assert.That(health.Current, Is.EqualTo(50));
            Assert.That(health.IsDead, Is.False);
        }
    }
}
