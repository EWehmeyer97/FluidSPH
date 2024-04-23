using Unity.Mathematics;
using UnityEngine;

public class Spawner3D : MonoBehaviour
{
    public Vector3Int particleToSpawn;
    public Vector3 centre;
    public Vector3 size;
    public float3 initialVel;
    public float jitterStrength;
    public bool showSpawnBounds;

    [Header("Info")]
    public int debug_numParticles;

    public SpawnData GetSpawnData()
    {
        int numPoints = particleToSpawn.x * particleToSpawn.y * particleToSpawn.z;
        float3[] points = new float3[numPoints];
        float3[] velocities = new float3[numPoints];

        int i = 0;

        for (int x = 0; x < particleToSpawn.x; x++)
        {
            for (int y = 0; y < particleToSpawn.y; y++)
            {
                for (int z = 0; z < particleToSpawn.z; z++)
                {
                    float tx = x / (particleToSpawn.x - 1f);
                    float ty = y / (particleToSpawn.y - 1f);
                    float tz = z / (particleToSpawn.z - 1f);

                    float px = (tx - 0.5f) * size.x + centre.x;
                    float py = (ty - 0.5f) * size.y + centre.y;
                    float pz = (tz - 0.5f) * size.z + centre.z;
                    float3 jitter = UnityEngine.Random.insideUnitSphere * jitterStrength;
                    points[i] = new float3(px, py, pz) + jitter;
                    velocities[i] = initialVel;
                    i++;
                }
            }
        }

        return new SpawnData() { points = points, velocities = velocities };
    }

    public struct SpawnData
    {
        public float3[] points;
        public float3[] velocities;
    }

    void OnValidate()
    {
        debug_numParticles = particleToSpawn.x * particleToSpawn.y * particleToSpawn.z;
    }

    void OnDrawGizmos()
    {
        if (showSpawnBounds && !Application.isPlaying)
        {
            Gizmos.color = new Color(1, 1, 0, 0.5f);
            Gizmos.DrawWireCube(centre, size);
        }
    }
}
