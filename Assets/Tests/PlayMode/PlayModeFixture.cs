using System;
using System.Collections;
using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace Arena.Tests.PlayMode
{
    public abstract class PlayModeFixture
    {
        private readonly List<GameObject> _owned = new List<GameObject>();
        private float _timeScale;
        private float _captureDeltaTime;

        [SetUp]
        public void SetUpClock()
        {
            _timeScale = Time.timeScale;
            _captureDeltaTime = Time.captureDeltaTime;
            Time.timeScale = 1f;
            // Fixed simulated frame duration makes timing checks independent of machine speed.
            Time.captureDeltaTime = 1f / 60f;
        }

        protected GameObject NewObject(string name)
        {
            var go = new GameObject(name);
            _owned.Add(go);
            return go;
        }

        protected T NewComponent<T>(string name) where T : Component => NewObject(name).AddComponent<T>();

        protected static IEnumerator Until(Func<bool> condition, string reason, float timeout = 5f)
        {
            float deadline = Time.realtimeSinceStartup + timeout;
            while (!condition())
            {
                Assert.That(Time.realtimeSinceStartup, Is.LessThan(deadline), "Timed out: " + reason);
                yield return null;
            }
        }

        protected static IEnumerator WaitGameSeconds(float seconds)
        {
            float deadline = Time.time + seconds;
            yield return Until(() => Time.time >= deadline, "waiting for game time");
        }

        [UnityTearDown]
        public IEnumerator TearDownObjectsAndClock()
        {
            foreach (GameObject go in _owned)
            {
                if (go == null) continue;
                go.SetActive(false); // Stops updates/coroutines before deferred destruction.
                UnityEngine.Object.Destroy(go);
            }
            _owned.Clear();
            Time.timeScale = _timeScale;
            Time.captureDeltaTime = _captureDeltaTime;
            yield return null;
        }
    }
}
