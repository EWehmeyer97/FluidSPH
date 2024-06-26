using UnityEngine;
using Unity.Mathematics;
using System.Runtime.InteropServices;
using System;

[System.Serializable]
[StructLayout(LayoutKind.Sequential, Size = 44)]
public struct Particle //44 bytes total
{
    public float2 density; //8 bytes
    public Vector3 velocity; //12 bytes
    public Vector3 predictedPosition;
    public Vector3 position;
}

[System.Serializable]
[StructLayout(LayoutKind.Sequential, Size = 16)]
public struct Sphere //16 bytes total
{
    public Vector3 pos; //12 bytes
    public float radius; //4 bytes
}

[System.Serializable]
[StructLayout(LayoutKind.Sequential, Size = 60)]
public struct Cube //60 bytes total
{
    public Vector3 pos; //12 bytes
    public Vector3 size;
    public Vector3 xLocal;
    public Vector3 yLocal;
    public Vector3 zLocal;
};

public class Simulation3D : MonoBehaviour
{
    public event System.Action SimulationStepCompleted;

    [Header("Settings")]
    public int iterationsPerFrame;
    public float gravity = -10;
    [Range(0, 1)] public float collisionDamping = 0.05f;
    public float radius = 0.2f;
    public float targetDensity;
    public float pressureMultiplier;
    public float nearPressureMultiplier;
    public float viscosityStrength;

    [Header("References")]
    public ComputeShader compute;
    public Spawner3D spawner;
    public ParticleDisplay3D display;

    [Header("Colliders")]
    public Transform[] spheres;
    public Transform[] cubes;

    // Buffers
    public ComputeBuffer particleBuffer { get; private set; }
    ComputeBuffer spatialIndices;
    ComputeBuffer spatialOffsets;

    // Collision Buffers
    ComputeBuffer sphereBuffer;
    ComputeBuffer cubeBuffer;

    // Kernel IDs
    const int externalForcesKernel = 0;
    const int spatialHashKernel = 1;
    const int densityKernel = 2;
    const int pressureKernel = 3;
    const int viscosityKernel = 4;
    const int updatePositionsKernel = 5;

    GPUSort gpuSort;

    Spawner3D.SpawnData spawnData;

    void Start()
    {
        float deltaTime = 1 / 60f;
        Time.fixedDeltaTime = deltaTime;

        spawnData = spawner.GetSpawnData();

        // Create Buffers
        int numParticles = spawnData.points.Length;
        particleBuffer = ComputeHelper.CreateStructuredBuffer<Particle>(numParticles);
        
        spatialIndices = ComputeHelper.CreateStructuredBuffer<uint3>(numParticles);
        spatialOffsets = ComputeHelper.CreateStructuredBuffer<uint>(numParticles);
        
        // Create Collision Buffers
        sphereBuffer = ComputeHelper.CreateStructuredBuffer<Sphere>(spheres.Length);
        cubeBuffer = ComputeHelper.CreateStructuredBuffer<Cube>(cubes.Length);

        // Set buffer data
        SetInitialBufferData(spawnData);

        // Init compute
        ComputeHelper.SetBuffer(compute, particleBuffer, "particles", externalForcesKernel, spatialHashKernel, densityKernel, pressureKernel, viscosityKernel, updatePositionsKernel);        
        ComputeHelper.SetBuffer(compute, spatialIndices, "SpatialIndices", spatialHashKernel, densityKernel, pressureKernel, viscosityKernel);
        ComputeHelper.SetBuffer(compute, spatialOffsets, "SpatialOffsets", spatialHashKernel, densityKernel, pressureKernel, viscosityKernel);

        // Init collision compute
        ComputeHelper.SetBuffer(compute, sphereBuffer, "spheres", externalForcesKernel, spatialHashKernel, densityKernel, pressureKernel, viscosityKernel, updatePositionsKernel);
        ComputeHelper.SetBuffer(compute, cubeBuffer, "cubes", externalForcesKernel, spatialHashKernel, densityKernel, pressureKernel, viscosityKernel, updatePositionsKernel);


        compute.SetInt("numParticles", particleBuffer.count);

        compute.SetInt("numSpheres", sphereBuffer.count);
        compute.SetInt("numCubes", cubeBuffer.count);

        gpuSort = new();
        gpuSort.SetBuffers(spatialIndices, spatialOffsets);


        // Init display
        display.Init(this);
    }

    void FixedUpdate()
    {
        UpdateSettings();

        RunSimulationFrame(Time.fixedDeltaTime);
    }

    void RunSimulationFrame(float frameTime)
    {
        float timeStep = frameTime / iterationsPerFrame;

        for (int i = 0; i < iterationsPerFrame; i++)
        {
            RunSimulationStep();
            SimulationStepCompleted?.Invoke();
        }
    }

    void RunSimulationStep()
    {
        ComputeHelper.Dispatch(compute, particleBuffer.count, kernelIndex: externalForcesKernel);
        ComputeHelper.Dispatch(compute, particleBuffer.count, kernelIndex: spatialHashKernel);
        gpuSort.SortAndCalculateOffsets();
        ComputeHelper.Dispatch(compute, particleBuffer.count, kernelIndex: densityKernel);
        ComputeHelper.Dispatch(compute, particleBuffer.count, kernelIndex: pressureKernel);
        ComputeHelper.Dispatch(compute, particleBuffer.count, kernelIndex: viscosityKernel);
        ComputeHelper.Dispatch(compute, particleBuffer.count, kernelIndex: updatePositionsKernel);
    }

    void UpdateSettings()
    {
        compute.SetVector("boundsSize", transform.localScale);
        compute.SetVector("centre", transform.position);

        compute.SetMatrix("localToWorld", transform.localToWorldMatrix);
        compute.SetMatrix("worldToLocal", transform.worldToLocalMatrix);

        sphereBuffer.SetData(GetSpheres());
        cubeBuffer.SetData(GetCubes());
    }

    private Sphere[] GetSpheres()
    {
        Sphere[] allSphere = new Sphere[spheres.Length];
        for (int i = 0; i < allSphere.Length; i++)
            allSphere[i] = new Sphere { pos = spheres[i].position, radius = spheres[i].localScale.x / 2f };

        return allSphere;
    }

    private Cube[] GetCubes()
    {
        Cube[] allCubes = new Cube[cubes.Length];
        for (int i = 0; i < allCubes.Length; i++)
            allCubes[i] = new Cube { pos = cubes[i].position, size = cubes[i].localScale / 2f, 
                xLocal = cubes[i].right.normalized, yLocal = cubes[i].up.normalized, zLocal = cubes[i].forward.normalized };

        return allCubes;
    }

    void SetInitialBufferData(Spawner3D.SpawnData spawnData)
    {
        Particle[] allPoints = new Particle[spawnData.points.Length];
        for(int i = 0; i < allPoints.Length; i++)
        {
            Particle p = new Particle { position = spawnData.points[i], predictedPosition = spawnData.points[i], velocity = spawnData.velocities[i] };
            allPoints[i] = p;
        }

        particleBuffer.SetData(allPoints);

        compute.SetFloat("deltaTime", Time.fixedDeltaTime / iterationsPerFrame);
        compute.SetFloat("gravity", gravity);
        compute.SetFloat("collisionDamping", collisionDamping);

        compute.SetFloat("radius", radius);
        compute.SetFloat("radius2", radius * radius);
        compute.SetFloat("radius5", Mathf.Pow(radius, 5));
        compute.SetFloat("radius6", Mathf.Pow(radius, 6));
        compute.SetFloat("radius9", Mathf.Pow(radius, 9));

        compute.SetFloat("targetDensity", targetDensity);
        compute.SetFloat("pressureMultiplier", pressureMultiplier);
        compute.SetFloat("nearPressureMultiplier", nearPressureMultiplier);
        compute.SetFloat("viscosityStrength", viscosityStrength);

        UpdateSettings();
    }

    void OnDestroy()
    {
        ComputeHelper.Release(particleBuffer, spatialIndices, spatialOffsets);
    }

    //Draw Bounds
    void OnDrawGizmos()
    {
        var m = Gizmos.matrix;
        Gizmos.matrix = transform.localToWorldMatrix;
        Gizmos.color = new Color(0, 1, 0, 0.5f);
        Gizmos.DrawWireCube(Vector3.zero, Vector3.one);
        Gizmos.matrix = m;
    }
}
