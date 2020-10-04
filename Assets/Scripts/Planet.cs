using System.Collections.Generic;
using UnityEngine;

public class Planet : GravityObject
{
    public ParticleSystem gravityWellParticleEmitter;
    public ParticleSystem reverseGravityWellParticleEmitter;
    private GameObject currentModel;
    
    public void PlaceRandomly(float minPlanetSize, float maxPlanetSize, float minMass, float maxMass,
        float cutOffGravitySpeed, List<GameObject> planetPrefabs)
    {
        if (currentModel)
        {
            Destroy(currentModel);
        }
        transform.position = new Vector3(Random.Range(-50, 50), 0, Random.Range(-50, 50));
        radius = Random.Range(minPlanetSize / 2, maxPlanetSize / 2);
        mass = Random.Range(minMass, maxMass);
        radiusGravity = 1 / ((cutOffGravitySpeed / mass) / G);
        
        if (Random.Range(0, 5) == 4) // repulsing?
        {
            mass = -mass;
        }

        currentModel = Instantiate(planetPrefabs[Random.Range(0, planetPrefabs.Count)], transform);
        currentModel.AddComponent<PlanetRotate>().rotationSpeed = Random.Range(20, 50);
        currentModel.transform.localScale = Vector3.one * radius;

        foreach (var ps in currentModel.GetComponentsInChildren<ParticleSystem>())
        {
            ps.transform.localScale *= radius;
        }

        if (mass > 0)
        {
            gravityWellParticleEmitter.gameObject.SetActive(true);
            reverseGravityWellParticleEmitter.gameObject.SetActive(false);
            gravityWellParticleEmitter.transform.localScale = Vector3.one * radiusGravity / 10;
        }
        else
        {
            gravityWellParticleEmitter.gameObject.SetActive(false);
            reverseGravityWellParticleEmitter.gameObject.SetActive(true);
            reverseGravityWellParticleEmitter.transform.localScale = Vector3.one * radiusGravity / 10;
        }
        gravityWellParticleEmitter.GetComponent<ParticleSystem>().Clear();
        reverseGravityWellParticleEmitter.GetComponent<ParticleSystem>().Clear();
        
    }
}