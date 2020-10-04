using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

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
    public GameObject planetPrefab;
    public GameObject ballPrefab;
    public GameObject planets;
    private GameObject ball;
    public int planetCount = 5;
    public float minPlanetSize = 1;
    public float maxPlanetSize = 2;
    public float minMass = 20000f;
    public float maxMass = 50000f;
    public float cutOffGravitySpeed = 250;
    
    public List<GameObject> planetPrefabs = new List<GameObject>();
    
    public static List<Planet> allPlanets = new List<Planet>();
    
    // Start is called before the first frame update
    void Start()
    {
        ball = Instantiate(ballPrefab, transform);
        ball.name = "Ball";
        NewUniverse();
    }

    public void NewUniverse()
    {
        foreach (Transform oldPlanet in planets.transform)
        {
            oldPlanet.parent = null;
        }
        GetComponent<MouseController>().ball = ball.GetComponent<Ball>();
        for (var i = 0; i < planetCount; i++)
        {
            Planet planet;
            if (allPlanets.Count > i)
            {
                planet = allPlanets[i];
            }
            else
            {
                planet = Instantiate(planetPrefab).GetComponent<Planet>();
                allPlanets.Add(planet);
            }
            planet.PlaceRandomly(minPlanetSize, maxPlanetSize, minMass, maxMass, cutOffGravitySpeed);
            
            var planetObject = planet.gameObject;
            planetObject.transform.parent = planets.transform;
            planetObject.name = "Planet " + i; 
            
        }

        ball.GetComponent<Ball>().PlaceInOrbit();
    }

    // Update is called once per frame
    void Update()
    {
        
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
}
