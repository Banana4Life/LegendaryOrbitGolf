using System.Collections.Generic;
using UnityEngine;

public class Planet : GravityObject
{
    public void PlaceRandomly(float minPlanetSize, float maxPlanetSize, float minMass, float maxMass,
        float cutOffGravitySpeed, List<GameObject> planetPrefabs)
    {
        transform.position = new Vector3(Random.Range(-50, 50), 0, Random.Range(-50, 50));
        radius = Random.Range(minPlanetSize / 2, maxPlanetSize / 2);
        mass = Random.Range(minMass, maxMass);
        radiusGravity = 1 / ((cutOffGravitySpeed / mass) / G);

        var planetModel = Instantiate(planetPrefabs[Random.Range(0, planetPrefabs.Count)], transform);
        planetModel.AddComponent<PlanetRotate>().rotationSpeed = Random.Range(20, 50);
        planetModel.transform.localScale = Vector3.one * radius;
        GetComponentInChildren<ParticleSystem>().transform.localScale = Vector3.one * radiusGravity / 10;
    }
}