using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;
using Random = UnityEngine.Random;

public class World : MonoBehaviour
{
    public Camera playerCamera;
    public GameObject planetPrefab;
    public GameObject ballPrefab;
    public GameObject planets;
    public Hud hud;
    private GameObject ballObject;
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

    public GameObject goalParticlePrefab;

    public AudioSource engageBreaksSound;


    public LineRenderer lr1;
    public LineRenderer lr2;
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
        _usedGridSlots.Clear();
        
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

        var extraArea = Vector3.Distance(goalPos, startPos) * 0.3f;

        GeneratePlanets(new Vector3(minX - extraArea, 0, minZ - extraArea),
            new Vector3(maxX + extraArea, 0, maxZ + extraArea));
        
        CollectParPlanets(startPlanet, goalPlanet);
        SetParCount();
    }
    
    public static float DistancePointToLineSegment(Vector2 p, Vector2 a, Vector2 b)
    {
        float sqrMagnitude = (b - a).sqrMagnitude;
        if ((double) sqrMagnitude == 0.0)
            return (p - a).magnitude;
        float num = Vector2.Dot(p - a, b - a) / sqrMagnitude;
        if ((double) num < 0.0)
            return (p - a).magnitude;
        if ((double) num > 1.0)
            return (p - b).magnitude;
        Vector2 vector2 = a + num * (b - a);
        return (p - vector2).magnitude;
    }

    void CollectParPlanets(Planet start, Planet goal)
    {
        var goalParticleInstance = Instantiate(goalParticlePrefab, goal.gameObject.transform);
        goal.setGoal(goalParticleInstance);
        
        var startPos = Helper.ToVector2(start.transform.position);
        var goalPos3d = goal.transform.position;
        var goalPos = Helper.ToVector2(goalPos3d);

        parPlanets.Clear();
        foreach (var p in allPlanets)
        {
            if (DistancePointToLineSegment(Helper.ToVector2(p.transform.position), startPos, goalPos) < p.radiusGravity)
            {
                parPlanets.Add(p);
            }
        }
        
        parPlanets.Sort((a, b) =>
        {
            var distA = (goalPos3d - a.transform.position).sqrMagnitude;
            var distB = (goalPos3d - b.transform.position).sqrMagnitude;
            if (distA < distB)
            {
                return -1;
            }

            if (distA > distB)
            {
                return 1;
            }

            return 0;
        });
    }

    void SetParCount()
    {
        int parCount = 0;
        foreach (Planet planet in parPlanets)
        {
            if (planet.mass > 0)
            {
                // attractor count 1
                parCount++;
            }
            else
            {
                // repulsor count 2
                parCount += 2;
            }
        }
        
        hud.SetNewPaarCount(parCount);
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

    public void PlaceBall()
    {
        if (!ballObject)
        {
            ballObject = Instantiate(ballPrefab, transform);
            ballObject.name = "Ball";
        }

        var ball = ballObject.GetComponent<Ball>();
        GetComponent<MouseController>().ball = ball;
        ball.PlaceInOrbit(this);
        ball.engageBreaksSound = engageBreaksSound;
    }

#if UNITY_EDITOR
    public void LoadPlanetPrefabs()
    {
        planetPrefabs.Clear();
        foreach (var file in Directory.GetFiles("Assets/Prefabs/Worlds", "*.prefab"))
        {
            var planet = AssetDatabase.LoadAssetAtPath<GameObject>(file);
            planetPrefabs.Add(planet);
        }
    }
#endif

    void GeneratePlanetsInFrustum()
    {
        Vector3 cameraPos = playerCamera.transform.position;
        var groundPosition = new Vector3(cameraPos.x, 0, cameraPos.z);
        var frustumDimension = Helper.ToVector3(Helper.FrustumDimensions(playerCamera, 200));

        Vector3 frustumStartCornerInWorld = groundPosition - frustumDimension / 2;
        Vector3 frustumEndCornerInWorld = frustumStartCornerInWorld + frustumDimension;
        
        GeneratePlanets(frustumStartCornerInWorld, frustumEndCornerInWorld);
    }

    void GeneratePlanets(Vector3 startCorner, Vector3 endCorner)
    {
        IterateGridPositionsInWorldRect(startCorner, endCorner, (x, z) =>
        {
            var radius = Random.Range(minPlanetSize / 2, maxPlanetSize / 2);
            var mass = Random.Range(minMass, maxMass);
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

    static long SlotId(int x, int z)
    {
        return (x & 0xFFFFFFFF) << 32 | (z & 0xFFFFFFFF);
    }

#if UNITY_EDITOR
    private void OnDrawGizmos()
    {
        if (!(startPlanet != null && goalPlanet != null))
        {
            return;
        }
        Gizmos.color = Color.yellow;
        Gizmos.DrawLine(startPlanet.transform.position, goalPlanet.transform.position);
        var last = parPlanets[0];
        Gizmos.color = Color.green;
        for (var i = 1; i < this.parPlanets.Count; i++)
        {
            var current = parPlanets[i];
            Gizmos.DrawLine(last.transform.position, current.transform.position);
            last = current;
        }
    }
#endif
    public void NewGoal()
    {
        var list = allPlanets.FindAll(p =>p.mass > 0 && p != goalPlanet);
        var planet = list[Random.Range(0, list.Count)];
        goalPlanet.DeleteGoal();
        goalPlanet = planet;
        
        CollectParPlanets(ballObject.GetComponent<Ball>().inOrbitAround, goalPlanet);
        SetParCount();
    }
}