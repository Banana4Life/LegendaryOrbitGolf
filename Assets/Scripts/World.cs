using System.Collections;
using System.Collections.Generic;
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
    }
}
public class World : MonoBehaviour
{
    public GameObject planetPrefab;
    public GameObject ballPrefab;
    public GameObject planets;
    private GameObject ball;
    public int planetCount = 5;
    
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
            planet.PlaceRandomly();
            
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
}
