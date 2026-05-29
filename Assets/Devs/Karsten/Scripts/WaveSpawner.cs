using UnityEngine;
using System.Collections;

public class WaveSpawner : MonoBehaviour
{
    public GameObject SpawnPoint;
    public float spawnRadius = 5f;
    public GameObject enemyPrefab;
    public int enemiesPerWave = 5;
    public float timeBetweenWaves = 10f;
    public float nextWaveTime = 0f;
    

    void Start()
    {
        StartCoroutine(SpawnWave());
    }

    IEnumerator SpawnWave()
    {
        for (int i = 0; i < enemiesPerWave; i++)
        {
            SpawnEnemy();
        }
        yield return new WaitForSeconds(timeBetweenWaves);
    }

    void SpawnEnemy()
    {
        Vector3 randomOffset = Random.insideUnitSphere * spawnRadius;
        randomOffset.y = 0.19f; 
        Instantiate(enemyPrefab, SpawnPoint.transform.position + randomOffset, Quaternion.identity);
    }

    void OnDrawGizmosSelected()
    {
        if (SpawnPoint != null)
        {
            Gizmos.color = Color.red;
            Gizmos.DrawWireSphere(SpawnPoint.transform.position, spawnRadius);
        }
    }

    void Update()
    {
        
    }
}
