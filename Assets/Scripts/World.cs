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
    private GameObject ball;
    public float minPlanetSize = 1;
    public float maxPlanetSize = 2;
    public float minMass = 20000f;
    public float maxMass = 50000f;
    public float cutOffGravitySpeed = 250;
    public float repulsorProbability = 0.05f;
    
    public List<GameObject> planetPrefabs = new List<GameObject>();
    
    public List<Planet> allPlanets = new List<Planet>();
    public List<Planet> parPlanets = new List<Planet>();
    public Planet startPlanet;
    public Planet goalPlanet;
    
    private const float GridCellSize = 5f;
    private const float PlanetClearance = GridCellSize / 2f;
    private readonly HashSet<long> _usedGridSlots = new HashSet<long>();
        
    public int attractorsPlaced;
    public int repulsorsPlaced;
    public int goalDistance = 20;
    public float goalMass = 50000f;
    public float goalSize = 2;
    
    // Start is called before the first frame update
    void Start()
    {
        ball = Instantiate(ballPrefab, transform);
        ball.name = "Ball";
        NewUniverse();
    }

    public void NewUniverse()
    {
        foreach (Planet p in allPlanets)
        {
            Destroy(p.gameObject);
        }

        allPlanets.Clear();
        _usedGridSlots.Clear();

        GetComponent<MouseController>().ball = ball.GetComponent<Ball>();

        attractorsPlaced = 0;
        repulsorsPlaced = 0;

        startPlanet = GenerateStartPlanet();
        var startPos = startPlanet.transform.position;

        goalPlanet = GenerateGoalPlanet();
        var goalPos = goalPlanet.transform.position;

        var minX = Mathf.Min(startPos.x, goalPos.x);
        var minZ = Mathf.Min(startPos.z, goalPos.z);

        var maxX = Mathf.Max(startPos.x, goalPos.x);
        var maxZ = Mathf.Max(startPos.z, goalPos.z);

        var extraArea = Vector3.Distance(goalPos, startPos) * 0.5f;

        GeneratePlanets(new Vector3(minX - extraArea, 0, minZ - extraArea),
            new Vector3(maxX + extraArea, 0, maxZ + extraArea));
        
        CollectParPlanets(startPlanet, goalPlanet);

        ball.GetComponent<Ball>().PlaceInOrbit(this);
    }

    void CollectParPlanets(Planet start, Planet goal)
    {
        var startPos = start.transform.position;
        var goalPos = goal.transform.position;
        var dir = goalPos - startPos;

        bool Touches(Vector3 point, float radius)
        {
            return Vector3.Cross(dir, point - startPos).sqrMagnitude < radius * radius;
        }

        parPlanets.Clear();
        foreach (var p in allPlanets)
        {
            if (Touches(p.transform.position, p.radiusGravity))
            {
                parPlanets.Add(p);
            }
        }
    }

    Planet GenerateStartPlanet()
    {
        var radius = goalSize / 2f;
        return GenerateRandomPlanetAt("start", 0, 0, radius, goalMass, CalculateGravityRadius(goalMass, radius), false);
    }


    Planet GenerateGoalPlanet()
    {
        var goalPosition = Helper.GridPosition(Random.insideUnitCircle * GridCellSize * goalDistance, GridCellSize);
        var radius = goalSize / 2f;
        return GenerateRandomPlanetAt("goal", goalPosition.x, goalPosition.y, radius, goalMass, CalculateGravityRadius(goalMass, radius), false);
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

    void GeneratePlanetsInFrustum()
    {
        Vector3 cameraPos = playerCamera.transform.position;
        var groundPosition = new Vector3(cameraPos.x, 0, cameraPos.z);
        var frustumDimension = Helper.ToVector3(Helper.FrustumDimensions(playerCamera, 500));

        Vector3 frustumStartCornerInWorld = groundPosition - frustumDimension / 2;
        Vector3 frustumEndCornerInWorld = frustumStartCornerInWorld + frustumDimension;
        
        GeneratePlanets(frustumStartCornerInWorld, frustumEndCornerInWorld);
    }

    void GeneratePlanets(Vector3 startCorner, Vector3 endCorner)
    {
        IterateGridPositionsInWorldRect(startCorner, endCorner, (x, z) =>
        {
            var radius = UnityEngine.Random.Range(minPlanetSize / 2, maxPlanetSize / 2);
            var mass = UnityEngine.Random.Range(minMass, maxMass);
            var gravityRadius = CalculateGravityRadius(mass, radius);

            var worldPos = Helper.WorldPosition(x, 0, z, GridCellSize);
            var clearanceRadius = gravityRadius + PlanetClearance;

            if (CheckAllGridSlotsUnused(worldPos, clearanceRadius))
            {
                GenerateRandomPlanetAt("filler", x, z, radius, mass, gravityRadius, true);
            }

            return true;
        });
    }

    bool IterateGridPositionsInWorldRect(Vector3 worldA, Vector3 worldB, Func<int, int, bool> f)
    {
        var gridA = Helper.GridPosition(worldA, GridCellSize);
        var gridB = Helper.GridPosition(worldB, GridCellSize);
        
        var startX = Math.Min(gridA.x, gridB.x);
        var startZ = Math.Min(gridA.z, gridB.z);
        
        var endX= Math.Max(gridA.x, gridB.x);
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
    
    bool IterateGridPositionsInWorldAround(Vector3 world, float radius, Func<int, int, bool> f)
    {
        var worldRectCornerOffset = new Vector3(radius, 0, radius);

        Vector3 worldRectStart = world - worldRectCornerOffset;
        Vector3 worldRectEnd = world + worldRectCornerOffset;

        var radiusSquare = radius * radius;

        return IterateGridPositionsInWorldRect(worldRectStart, worldRectEnd, (xx, zz) =>
        {
            var inWorld = Helper.WorldPosition(xx, 0, zz, GridCellSize);
            if ((inWorld - world).sqrMagnitude < radiusSquare)
            {
                if (!f(xx, zz))
                {
                    return false;
                }
            }

            return true;
        });
    }

    Planet GenerateRandomPlanetAt(string kind, int x, int z, float radius, float mass, float gravityRadius, bool allowRepulsor)
    {
        Planet planet = Instantiate(planetPrefab).GetComponent<Planet>();
        
        planet.radius = radius;
        planet.mass = mass;
        planet.radiusGravity = gravityRadius;
        
        var planetObject = planet.gameObject;
        planetObject.transform.parent = planets.transform;
        planetObject.name = $"Planet {kind} at ({x}, {z}) in grid";
        Vector3 pos = Helper.WorldPosition(x, 0, z, GridCellSize);
        planetObject.transform.position = pos + Helper.ToVector3(Random.insideUnitCircle * Random.Range(0,  GridCellSize));

        var currentModel = Instantiate(planetPrefabs[UnityEngine.Random.Range(0, planetPrefabs.Count)], planet.transform);
        currentModel.AddComponent<PlanetRotate>().rotationSpeed = UnityEngine.Random.Range(20, 50);
        currentModel.transform.localScale = Vector3.one * planet.radius;

        if (Random.value < repulsorProbability && allowRepulsor)
        {
            repulsorsPlaced++;
            planet.mass *= -1;
            planet.gravityWellParticleEmitter.gameObject.SetActive(false);
            planet.reverseGravityWellParticleEmitter.gameObject.SetActive(true);
            planet.reverseGravityWellParticleEmitter.transform.localScale = Vector3.one * planet.radiusGravity / 10;
        }
        else
        {
            attractorsPlaced++;
            planet.gravityWellParticleEmitter.gameObject.SetActive(true);
            planet.gravityWellParticleEmitter.transform.localScale = Vector3.one * planet.radiusGravity / 10;
            planet.reverseGravityWellParticleEmitter.gameObject.SetActive(false);
        }
        
        allPlanets.Add(planet);

        IterateGridPositionsInWorldAround(pos, gravityRadius + PlanetClearance, (xx, zz) =>
        {
            _usedGridSlots.Add(SlotId(xx, zz));
            return true;
        });

        return planet;
    }

    private float CalculateGravityRadius(float mass, float radius)
    {
        return Mathf.Max(1 / (cutOffGravitySpeed / mass / GravityObject.G), 2 * radius);
    }

    bool CheckAllGridSlotsUnused(Vector3 a, Vector3 b)
    {
        return IterateGridPositionsInWorldRect(a, b, (i, i1) => !_usedGridSlots.Contains(SlotId(i, i1)));
    }

    bool CheckAllGridSlotsUnused(Vector3 a, float b)
    {
        return IterateGridPositionsInWorldAround(a, b, (i, i1) => !_usedGridSlots.Contains(SlotId(i, i1)));
    }

    long SlotId(int x, int z)
    {
        return (x & 0xFFFFFFFF) << 32 | (z & 0xFFFFFFFF);
    }
}
