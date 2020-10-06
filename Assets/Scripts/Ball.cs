using System;
using UnityEditor;
using UnityEngine;
using Random = UnityEngine.Random;

public class Ball : GravityObject
{
    public ParticleSystem movingParticleSystem;
    public ParticleSystem brakeParticleSystem;

    public bool dead;

    private Vector3 _savePosition;
    private Vector3 _saveVelocity;

    private Trajectory _trajectory = new Trajectory();
    private Trajectory _planTrajectory = new Trajectory();
    
    private float _dtSince;

    public World world;

    public float minBumpSpeed = 0.5f;
    public float maxBumpSpeed = 15;

    public AudioSource collisionSound;
    public AudioSource engageBrakeSound;
    public AudioSource bumpSound;

    private void Start()
    {
        world = GetComponentInParent<World>();
    }
    
    public void PlaceInOrbit(World inWorld)
    {
        world = inWorld;
        PlaceInOrbit();
    }
    
    public void PlaceInOrbit()
    {
        // v = sqrt(GM * (2/r - 1/a))
        // a = 1/2 of longest axis
        var startPlanet = world.startPlanet;
        var planet = startPlanet.GetComponent<GravityObject>();
        transform.position = startPlanet.transform.position;
        var distance = Random.Range(planet.radius * 1.5f + radius * 2, planet.radiusGravity - radius);
        transform.Translate(distance, 0, 0);
        var a = Random.Range((distance + startPlanet.radius * 1.5f + radius) / 2, (distance + planet.radiusGravity - startPlanet.radius - radius) / 2);
        var orbitModifier = (2 / distance - 1 / a);
        velocity = Vector3.forward * Mathf.Sqrt(G * startPlanet.mass * orbitModifier);
        frozen = false;

        movingParticleSystem.Clear();
        brakeParticleSystem.Clear();

        _savePosition = transform.position;
        _saveVelocity = velocity;
        
        RecalculateTrajectory();
    }

    public void OnCollided()
    {
        frozen = true;
        dead = true;
        collisionSound.Play();
        bumpSound.Stop();
    }
    
    void Update()
    {
        if (!frozen)
        {
            FollowTrajectory();
            _trajectory.Continue(Vector3.zero, this);
        }
        CheckStillInOrbit();
        UpdateParticleSystems();
        DrawTrajectory();
    }

    private void FollowTrajectory()
    {
        _dtSince += Time.deltaTime;

        if (_trajectory.CalculateNext(_dtSince, out var pos, out var v))
        {
            if (pos.sqrMagnitude == 0)
            {
                return;
            }
            transform.position = pos;
            velocity = v;
        }

        AnalyzeTrajectory();
    }

    public void AnalyzeTrajectory()
    {
        foreach (var planet in world.allPlanets)
        {
            var delta = transform.position - planet.transform.position;
            
                
            if (TrajectoryUtil.CheckCollided(delta, radiusGravity + planet.radiusGravity))
            {
                if (TrajectoryUtil.CheckCollided(delta, radius + planet.radius))
                {
                    OnCollided();
                    return;
                }
                
                inOrbitAround = planet;
                break;
            }
        }

        var wasAnalyzed = _trajectory.isAnalyzed;
        if (_trajectory.Analyze(inOrbitAround, this))
        {
            if (!wasAnalyzed)
            {
                _savePosition = transform.position;
                _saveVelocity = velocity;    
            }
        }    
    }

    private void CheckStillInOrbit()
    {
        if (inOrbitAround)
        {
            var delta = transform.position - inOrbitAround.transform.position;
            if (!TrajectoryUtil.CheckCollided(delta, radiusGravity + inOrbitAround.radiusGravity + radius))
            {
                inOrbitAround = null;
            }
        }
    }

    private void UpdateParticleSystems()
    {
        if (velocity != Vector3.zero)
        {
            movingParticleSystem.transform.LookAt(transform.position + velocity);
        }

        var emissionModule = movingParticleSystem.emission;
        emissionModule.enabled = !frozen && velocity.sqrMagnitude > 0.5;
    }

    public void Freeze()
    {
        frozen = true;
    }

    public void UnFreeze()
    {
        frozen = false;
    }

    public void SubmitPlan()
    {
        var newTrajectory = _planTrajectory;
        _planTrajectory = _trajectory.Reset();
        _trajectory = newTrajectory;
        _dtSince = 0;
        UnFreeze();
        bumpSound.Play();
    }

    public void CheatJumpTo(Vector3 pos)
    {
        ScrapPlan();
        transform.position = pos;
        velocity = Vector3.zero;
        frozen = true;
        _savePosition = pos;
        _saveVelocity = velocity;
        RecalculateTrajectory();
    }

    public void EngangeBrakes()
    {
        velocity *= 0.8f;
        brakeParticleSystem.Play();
        RecalculateTrajectory();
        engageBrakeSound.Play();
    }

    public void StartPlanning()
    {
        // If currently stable save position for later retry
        if (_trajectory.isStable)
        {
            _savePosition = transform.position;
            _saveVelocity = velocity;
        }
        // If dead revive at last savePoint
        if (dead)
        {
            Revive();
        }
        // Freeze while planning
        Freeze();
    }
    
    public void PlanTrajectory(Vector3 hoverPosition, float holdingTime)
    {
        Vector3 ballPos = transform.position;
        var bumpSpeed = CalcBumpSpeed(ballPos, velocity, minBumpSpeed, maxBumpSpeed, hoverPosition, holdingTime);
        _planTrajectory.Reset().Continue(bumpSpeed, this).Analyze(inOrbitAround, this);
    }

    private void RecalculateTrajectory()
    {
        _trajectory.Reset().Continue(Vector3.zero, this);
        _dtSince = 0;
        AnalyzeTrajectory();
    }

    public static Vector3 CalcBumpSpeed(Vector3 ballPos, Vector3 ballVelocity, float minSpeed, float maxSpeed, Vector3 hover, float holdingTime)
    {
        var playerControlledDirection = -(hover - ballPos).normalized;
        var ballControlledDirection = -ballVelocity.normalized;

        // Sinus curve with min speed
        var magnitude = ballVelocity.magnitude;
        var sawToothMagniture = minSpeed + Math.Abs(holdingTime % maxSpeed) * (magnitude == 0 ? 1 : magnitude / 15);
        // var magnitude = Math.Min(linearMagnitude, 5);
        if (magnitude == 0)
        {
            return playerControlledDirection * Math.Min(minSpeed + sawToothMagniture, maxSpeed);
        }

        return ballControlledDirection * Math.Min(sawToothMagniture, maxSpeed * magnitude  / 15);
    }
    
    private void DrawTrajectory()
    {
        DrawTrajectory(_trajectory, Color.gray, Color.blue, world.lr1);
        DrawTrajectory(_planTrajectory, Color.white, Color.green, world.lr2);
    }

    private void DrawTrajectory(Trajectory trajectory, Color color, Color colorStable, LineRenderer go)
    {
        LineRenderer lr = go.GetComponent<LineRenderer>();
        
        if (trajectory == null || trajectory.IsEmpty())
        {
            lr.enabled = false;
            return;
        }

        lr.enabled = true;
        
        lr.material = new Material(Shader.Find("Sprites/Default"));
        lr.startColor = trajectory.isStable ? colorStable : color;
        lr.endColor = trajectory.isStable ? colorStable : color;
        lr.startWidth = 0.1f;
        lr.endWidth = 0.1f;
        
        var list = trajectory.Positions(1);
        lr.positionCount = 0;
        for (var i = 0; i < list.Count; i++)
        {
            var position = list[i];
            lr.positionCount = i +1;
            lr.SetPosition(i, position);
        }

    }
    
#if UNITY_EDITOR

    private void OnDrawGizmos()
    {
        DrawTrajectoryGizmos(_trajectory, Color.gray, Color.blue);
        DrawTrajectoryGizmos(_planTrajectory, Color.white, Color.green);
        Handles.color = Color.cyan;
        Handles.DrawWireDisc(_savePosition, Vector3.up, radius);
    }

    private void DrawTrajectoryGizmos(Trajectory trajectory, Color color, Color colorStable)
    {
        if (trajectory == null || trajectory.IsEmpty())
        {
            return;
        }
        
        //
        //
        // var prevPosition = trajectory.points.Head.Item1;
        // foreach (var position in trajectory.Positions(1))
        // {
        //     Gizmos.color = color;
        //     if (trajectory.isStable)
        //     {
        //         Gizmos.color = colorStable;
        //     }
        //     // Handles.DrawWireDisc(position, Vector3.up, 0.1f);
        //     Gizmos.DrawLine(prevPosition, position);
        //     prevPosition = position;
        // }

        if (!trajectory.IsEmpty() && trajectory.IsInterupted())
        {
            Handles.color = Color.red;
            Handles.DrawWireDisc(trajectory.points.Tail.Item1, Vector3.up, radius);
        }
        
        if (trajectory.isAnalyzed)
        {
            Handles.color = Color.magenta;
            if (inOrbitAround)
            {
                var eNormalized = _trajectory.e.normalized;
                var planetPos = inOrbitAround.transform.position;
                Gizmos.color = Color.green;
                Gizmos.DrawLine(planetPos, planetPos + eNormalized * _trajectory.rPeriapsis);
                Gizmos.color = Color.red;
                Gizmos.DrawLine(planetPos, planetPos + eNormalized * -_trajectory.rApoapsis);
            }
        }
    }
#endif

    public bool HasPlan()
    {
        return !_planTrajectory.IsEmpty();
    }

    public void ScrapPlan()
    {
        _planTrajectory.Reset();
        UnFreeze();
        _trajectory.isAnalyzed = false;
        _trajectory.Analyze(inOrbitAround, this);
    }

    public void Revive()
    {
        dead = false;
        transform.position = _savePosition;
        velocity = _saveVelocity;
        if (velocity.sqrMagnitude == 0)
        {
            PlaceInOrbit();
        }
        RecalculateTrajectory();
        UnFreeze();
    }

    public bool IsInStableOrbit()
    {
        return _trajectory.isStable;
    }
}