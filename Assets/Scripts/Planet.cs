using System.Collections.Generic;
using UnityEngine;

public class Planet : GravityObject
{
    public ParticleSystem gravityWellParticleEmitter;
    public ParticleSystem reverseGravityWellParticleEmitter;
    private GameObject currentModel;
    private GameObject _goal;

    public void DeleteGoal()
    {
        _goal.transform.parent = null;
        Destroy(_goal);
    }

    public void setGoal(GameObject goal)
    {
        this._goal = goal;
    }
}