using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;
using Random = UnityEngine.Random;

[CustomEditor(typeof(World))]
class WorldEditor : Editor
{
    public override void OnInspectorGUI()
    {
        base.OnInspectorGUI();
        if (GUILayout.Button("New Universe"))
        {
            ((World) target).NewUniverse();
        }

        if (GUILayout.Button("Load Planet Prefabs"))
        {
            ((World) target).LoadPlanetPrefabs();
        }
    }
}

public class World : MonoBehaviour
{
    public Camera playerCamera;
    public GameObject planetPrefab;
    public GameObject ballPrefab;
    public GameObject planets;
    private GameObject ballObject;
    public float minPlanetSize = 1;
    public float maxPlanetSize = 2;
    public float minMass = 20000f;
    public float maxMass = 50000f;
    public float cutOffGravitySpeed = 250;

    public List<GameObject> planetPrefabs = new List<GameObject>();

    public List<Planet> allPlanets = new List<Planet>();

    private const float GridCellSize = 5f;
    private readonly HashSet<long> usedGridSlots = new HashSet<long>();

    // Start is called before the first frame update
    void Start()
    {
        NewUniverse();
    }

    public void NewUniverse()
    {
        foreach (Planet p in allPlanets)
        {
            Destroy(p.gameObject);
        }

        allPlanets.Clear();
        usedGridSlots.Clear();

        GeneratePlanets();
    }

    void GenerateRandomPlanetAt(int x, int z, float radius, float mass, float gravityRadius)
    {
        Planet planet = Instantiate(planetPrefab).GetComponent<Planet>();

        planet.radius = radius;
        planet.mass = mass;
        planet.radiusGravity = gravityRadius;

        var planetObject = planet.gameObject;
        planetObject.transform.parent = planets.transform;
        planetObject.name = $"Planet {x} {z}";
        Vector3 pos = Helper.WorldPosition(x, 0, z, GridCellSize);
        planetObject.transform.position = pos + Helper.ToVector3(Random.insideUnitCircle * Random.Range(0, GridCellSize));

        var currentModel = Instantiate(planetPrefabs[UnityEngine.Random.Range(0, planetPrefabs.Count)], planet.transform);
        currentModel.AddComponent<PlanetRotate>().rotationSpeed = UnityEngine.Random.Range(20, 50);
        currentModel.transform.localScale = Vector3.one * planet.radius;

        if (UnityEngine.Random.Range(0, 5) == 4)
        {
            planet.mass *= -1;
            planet.gravityWellParticleEmitter.gameObject.SetActive(false);
            planet.reverseGravityWellParticleEmitter.gameObject.SetActive(true);
            planet.reverseGravityWellParticleEmitter.transform.localScale = Vector3.one * planet.radiusGravity / 10;
        }
        else
        {
            planet.gravityWellParticleEmitter.gameObject.SetActive(true);
            planet.gravityWellParticleEmitter.transform.localScale = Vector3.one * planet.radiusGravity / 10;
            planet.reverseGravityWellParticleEmitter.gameObject.SetActive(false);
        }

        allPlanets.Add(planet);
    }

    public void PlaceBall()
    {
        if (!ballObject)
        {
            ballObject = Instantiate(ballPrefab, transform);
            ballObject.name = "Ball";
        }
        
        GetComponent<MouseController>().ball = ballObject.GetComponent<Ball>();
        ballObject.GetComponent<Ball>().PlaceInOrbit(this);
    }

    public void LoadPlanetPrefabs()
    {
        planetPrefabs.Clear();
        foreach (var file in Directory.GetFiles("Assets/Prefabs/Worlds", "*.prefab"))
        {
            var planet = AssetDatabase.LoadAssetAtPath<GameObject>(file);
            planetPrefabs.Add(planet);
        }
    }

    void GeneratePlanets()
    {
        Vector3 cameraPos = playerCamera.transform.position;
        var groundPosition = new Vector3(cameraPos.x, 0, cameraPos.z);
        var frustumDimension = Helper.ToVector3(Helper.FrustumDimensions(playerCamera, groundPosition));

        Vector3 frustumStartCornerInWorld = groundPosition - frustumDimension / 2;
        Vector3 frustumEdnCornerInWorld = frustumStartCornerInWorld + frustumDimension;

        IterateGridPositionsInWorldRect(frustumStartCornerInWorld, frustumEdnCornerInWorld, (x, z) =>
        {
            var radius = UnityEngine.Random.Range(minPlanetSize / 2, maxPlanetSize / 2);
            var mass = UnityEngine.Random.Range(minMass, maxMass);
            var gravityRadius = Mathf.Max(1 / (cutOffGravitySpeed / mass / GravityObject.G), 2 * radius);

            var clearanceRadius = gravityRadius + (GridCellSize / 2);
            var worldPos = Helper.WorldPosition(x, 0, z, GridCellSize);
            var worldRectCornerOffset = new Vector3(clearanceRadius, 0, clearanceRadius);

            Vector3 worldRectStart = worldPos - worldRectCornerOffset;
            Vector3 worldRectEnd = worldPos + worldRectCornerOffset;

            if (CheckAllGridSlotsUnused(worldRectStart, worldRectEnd))
            {
                GenerateRandomPlanetAt(x, z, radius, mass, gravityRadius);

                IterateGridPositionsInWorldRect(worldRectStart, worldRectEnd, (xx, zz) =>
                {
                    var inWorld = Helper.WorldPosition(xx, 0, zz, GridCellSize);
                    if ((inWorld - worldPos).sqrMagnitude < clearanceRadius * clearanceRadius)
                    {
                        usedGridSlots.Add(SlotId(xx, zz));
                    }

                    return true;
                });
            }

            return true;
        });
    }

    static bool IterateGridPositionsInWorldRect(Vector3 worldA, Vector3 worldB, Func<int, int, bool> f)
    {
        var gridA = Helper.GridPosition(worldA, GridCellSize);
        var gridB = Helper.GridPosition(worldB, GridCellSize);

        var startX = Math.Min(gridA.x, gridB.x);
        var startZ = Math.Min(gridA.z, gridB.z);

        var endX = Math.Max(gridA.x, gridB.x);
        var endZ = Math.Max(gridA.z, gridB.z);

        for (var x = startX; x <= endX; ++x) // yep, <=, include in more
        {
            for (int z = startZ; z <= endZ; z++) // yep, <=, include in more
            {
                if (!f(x, z))
                {
                    return false;
                }
            }
        }

        return true;
    }

    bool CheckAllGridSlotsUnused(Vector3 a, Vector3 b)
    {
        return IterateGridPositionsInWorldRect(a, b, (i, i1) => !usedGridSlots.Contains(SlotId(i, i1)));
    }

    static long SlotId(int x, int z)
    {
        return (x & 0xFFFFFFFF) << 32 | (z & 0xFFFFFFFF);
    }
}