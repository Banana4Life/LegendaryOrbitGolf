using UnityEngine;

public class Planet : GravityObject
{
    protected override void Init()
    {
        placeRandomly();
        base.Init();
    }

    void placeRandomly()
    {
        radius = Random.Range(0.3f, 0.7f);
        mass = Random.Range(2000f, 5000f);
        radiusGravity = radius * mass / 500;
    }
}