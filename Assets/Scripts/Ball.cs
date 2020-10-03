using UnityEngine;

public class Ball : GravityObject
{
    protected override void Init()
    {
        radius = 0.25f;
        mass = 10;
        gravityAffected = true;
        moving = true;
        base.Init();
    }
}