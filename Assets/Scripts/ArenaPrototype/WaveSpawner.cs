using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace ArenaPrototype
{
    public class WaveSpawner : MonoBehaviour
    {
        private readonly List<EnemyController> activeEnemies = new List<EnemyController>();
        private ArenaGame game;
        private bool spawning;

        public int CurrentWave { get; private set; }
        public bool WaveActive { get; private set; }
        public int AliveCount
        {
            get { return activeEnemies.Count; }
        }

        public void Setup(ArenaGame owner)
        {
            game = owner;
        }

        public void StartNextWave()
        {
            if (WaveActive || spawning || game.Player == null || !game.Player.Health.IsAlive)
            {
                return;
            }

            CurrentWave++;
            WaveActive = true;
            StartCoroutine(SpawnWaveRoutine(CurrentWave));
        }

        public void NotifyEnemyKilled(EnemyController enemy)
        {
            activeEnemies.Remove(enemy);
            game.OnEnemyKilled(enemy);

            if (!spawning && activeEnemies.Count == 0 && WaveActive)
            {
                CompleteWave();
            }
        }

        public void StopWave()
        {
            spawning = false;
            WaveActive = false;
        }

        private IEnumerator SpawnWaveRoutine(int wave)
        {
            spawning = true;
            int totalEnemies = 6 + wave * 3;
            float baseInterval = Mathf.Lerp(0.95f, 0.45f, Mathf.Clamp01(wave / 6f));

            for (int i = 0; i < totalEnemies; i++)
            {
                EnemyArchetype type = PickType(wave, i);
                Vector3 point = RandomSpawnPoint();
                EnemyController enemy = game.SpawnEnemy(type, point, wave);
                activeEnemies.Add(enemy);

                float interval = Mathf.Max(0.15f, baseInterval / Mathf.Max(0.1f, game.Tuning.spawnRateMultiplier));
                yield return new WaitForSeconds(interval);
            }

            spawning = false;
            if (activeEnemies.Count == 0 && WaveActive)
            {
                CompleteWave();
            }
        }

        private EnemyArchetype PickType(int wave, int index)
        {
            if (wave >= 3 && index % 7 == 0)
            {
                return EnemyArchetype.Bulwark;
            }

            if (wave >= 2 && Random.value < 0.32f)
            {
                return EnemyArchetype.Skirmisher;
            }

            return EnemyArchetype.Striker;
        }

        private Vector3 RandomSpawnPoint()
        {
            Vector2 circle = Random.insideUnitCircle.normalized * Random.Range(game.ArenaRadius * 0.62f, game.ArenaRadius * 0.9f);
            return new Vector3(circle.x, 0.6f, circle.y);
        }

        private void CompleteWave()
        {
            WaveActive = false;
            game.OnWaveComplete(CurrentWave);
        }
    }
}
