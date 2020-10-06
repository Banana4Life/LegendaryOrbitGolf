using UnityEngine;

public class Planet : GravityObject
{
    public ParticleSystem gravityWellParticleEmitter;
    public ParticleSystem reverseGravityWellParticleEmitter;
    private GameObject _currentModel;
    private GameObject _goal;

    public void DeleteGoal()
    {
        _goal.transform.parent = null;
        Destroy(_goal);
    }

    public void SetGoal(GameObject goal)
    {
        _goal = goal;
    }
}