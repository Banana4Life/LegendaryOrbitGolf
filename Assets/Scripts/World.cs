using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class World : MonoBehaviour
{
    public GameObject planetPrefab;
    public GameObject ballPrefab;
    public GameObject planets;
    
    public static List<GravityObject> allGravityObjects = new List<GravityObject>();
    
    // Start is called before the first frame update
    void Start()
    {
        var ball = Instantiate(ballPrefab, transform);
        allGravityObjects.Add(ball.GetComponent<GravityObject>());
        ball.name = "Ball";
        GetComponent<MouseController>().ball = ball.GetComponent<Ball>();
        var planet = Instantiate(planetPrefab, planets.transform);
        planet.GetComponent<Planet>().PlaceRandomly();
        allGravityObjects.Add(planet.GetComponent<GravityObject>());
        planet.name = "Planet 1";
        ball.GetComponent<Ball>().PlaceInOrbit();
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
