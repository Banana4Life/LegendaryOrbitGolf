using System.Collections.Generic;
using UnityEngine;

public class Planet : GravityObject
{
    public ParticleSystem gravityWellParticleEmitter;
    public ParticleSystem reverseGravityWellParticleEmitter;
    private GameObject currentModel;
}