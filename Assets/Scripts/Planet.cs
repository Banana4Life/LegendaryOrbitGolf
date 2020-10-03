using System.Collections.Generic;
using UnityEngine;

public class Planet : GravityObject
{
    public void PlaceRandomly()
    {
        transform.position = new Vector3(Random.Range(-50, 50), 0, Random.Range(-50, 50));
        radius = Random.Range(0.3f, 0.7f);
        mass = Random.Range(20000f, 50000f);
        radiusGravity = radius * mass / 1000;
    }
}