using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class WaveSpawner : MonoBehaviour
{
    public GameObject SpawnPoint;
    public float spawnRadius = 5f;
    public GameObject enemyPrefab;
    public int enemiesPerWave = 5;
    [Tooltip("Delay before the next wave spawns after all enemies are dead")]
    public float timeBetweenWaves = 5f;

    private List<GameObject> _activeEnemies = new List<GameObject>();

    void Start()
    {
        StartCoroutine(SpawnWave());
    }

    IEnumerator SpawnWave()
    {
        while (true)
        {
            yield return new WaitUntil(AllEnemiesDead);
            yield return new WaitForSeconds(timeBetweenWaves);

            _activeEnemies.Clear();
            for (int i = 0; i < enemiesPerWave; i++)
            {
                SpawnEnemy();
            }
        }
    }

    bool AllEnemiesDead()
    {
        _activeEnemies.RemoveAll(e => e == null);
        return _activeEnemies.Count == 0;
    }

    void SpawnEnemy()
    {
        Vector3 randomOffset = Random.insideUnitSphere * spawnRadius;
        randomOffset.y = 0.19f;
        _activeEnemies.Add(Instantiate(enemyPrefab, SpawnPoint.transform.position + randomOffset, Quaternion.identity));
    }

    void OnDrawGizmosSelected()
    {
        if (SpawnPoint != null)
        {
            Gizmos.color = Color.red;
            Gizmos.DrawWireSphere(SpawnPoint.transform.position, spawnRadius);
        }
    }
}
