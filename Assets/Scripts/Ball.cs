using System;
using System.IO;
using UnityEditor;
using UnityEngine;
using Random = UnityEngine.Random;

[CustomEditor(typeof(Ball))]
class BallEditor : Editor
{
    public override void OnInspectorGUI()
    {
        base.OnInspectorGUI();
        if (GUILayout.Button("Place in Orbit"))
        {
            ((Ball) target).PlaceInOrbit();
        }
    }
}

public class Ball : GravityObject
{
    public ParticleSystem movingParticleSystem;
    public ParticleSystem breakParticleSystem;

    public bool dead;

    private Vector3 savePosition;
    private Vector3 saveVelocity;

    public bool inStableOrbit;


    private Trajectory _trajectory = new Trajectory();
    private Trajectory _planTrajectory = new Trajectory();
    
    private float dtSince;

    private float orbitPointSize = 0.25f;

    public World world;

    public float minBumpSpeed = 0.5f;
    public float maxBumpSpeed = 15;

    public AudioSource engageBreaksSound;

    private void Start()
    {
        world = GetComponentInParent<World>();
    }
    
    public void PlaceInOrbit(World world)
    {
        this.world = world;
        PlaceInOrbit();
    }
    public void PlaceInOrbit()
    {
        var planet = world.startPlanet;
        var gravityObject = planet.GetComponent<GravityObject>();
        transform.position = planet.transform.position;
        var distance = Random.Range(gravityObject.radius + radius * 2, gravityObject.radiusGravity - radius);
        transform.Translate(distance, 0, 0);
        var a = Random.Range((distance + planet.radius + radius) / 2, (distance + gravityObject.radiusGravity - planet.radius - radius) / 2);
        var orbitModifier = (2 / distance - 1 / a);
        velocity = Vector3.forward * Mathf.Sqrt(G * planet.mass * orbitModifier);
        frozen = false;
        inStableOrbit = true;

        movingParticleSystem.Clear();
        breakParticleSystem.Clear();

        savePosition = transform.position;
        saveVelocity = velocity;
        
        RecalculateTrajectory();
    }

    public void OnCollided()
    {
        frozen = true;
        dead = true;
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
    }

    private void FollowTrajectory()
    {
        dtSince += Time.deltaTime;

        if (_trajectory.CalculateNext(dtSince, out var pos, out var v))
        {
            transform.position = pos;
            velocity = v;
        }

        if (!_trajectory.isAnalyzed)
        {
            AnalyzeTrajectory();
        }
    }

    public void AnalyzeTrajectory()
    {
        foreach (var planet in world.allPlanets)
        {
            var delta = transform.position - planet.transform.position;
            // if (CheckCollided(delta, radius + planet.radius))
            // {
            //     OnCollided(); // TODO cannot call it here anymore
            //     return;
            // }
                
            if (TrajectoryUtil.CheckCollided(delta, radiusGravity + planet.radiusGravity))
            {
                inOrbitAround = planet;
                break;
            }
        }

        if (!_trajectory.isAnalyzed)
        {
            if (_trajectory.Analyze(inOrbitAround, orbitPointSize))
            {
                savePosition = transform.position;
                saveVelocity = velocity;
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
        dtSince = 0;
        UnFreeze();
    }

    public void CheatJumpTo(Vector3 pos)
    {
        ScrapPlan();
        transform.position = pos;
        velocity = Vector3.zero;
        frozen = true;
        savePosition = pos;
        saveVelocity = velocity;
        RecalculateTrajectory();
    }

    public void EngangeBreaks()
    {
        velocity *= 0.8f;
        breakParticleSystem.Play();
        RecalculateTrajectory();
        engageBreaksSound.Play();
        // TODO sounds
    }

    public void StartPlanning()
    {
        // If currently stable save position for later retry
        if (inStableOrbit)
        {
            savePosition = transform.position;
            saveVelocity = velocity;
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
        _planTrajectory.Reset().Continue(bumpSpeed, this).Analyze(inOrbitAround, orbitPointSize);
    }

    private void RecalculateTrajectory()
    {
        _trajectory.Reset().Continue(Vector3.zero, this);
        dtSince = 0;
        AnalyzeTrajectory();
    }

    public static Vector3 CalcBumpSpeed(Vector3 ballPos, Vector3 ballVelocity, float minSpeed, float maxSpeed, Vector3 hover, float holdingTime)
    {
        var playerControlledDirection = -(hover - ballPos).normalized;
        var ballControlledDirection = -ballVelocity.normalized;

        // Linear curve
        var linearMagnitude = holdingTime * maxSpeed;

        // Sinus curve with min speed
        var p = 8;
        var triangleMagnitude = (float) (2 * Math.Abs(2 * ((holdingTime / p) - Math.Floor((holdingTime / p) + 0.5)))) * (maxSpeed - minSpeed) / 2 + minSpeed;
        var sinusMagnitude = (float) (Math.Sin(0.6 * holdingTime - 1.57) * (maxSpeed - minSpeed) / 2 + (maxSpeed / 2 + minSpeed));

        // var magnitude = Math.Min(linearMagnitude, 5);
        if (ballVelocity.sqrMagnitude == 0)
        {
            return playerControlledDirection * Math.Min(minSpeed + linearMagnitude, maxSpeed);
        }

        return ballControlledDirection * Math.Min(triangleMagnitude, maxSpeed);
    }
    
    
    private void OnDrawGizmos()
    {
        DrawTrajectoryGizmos(_trajectory, Color.gray, Color.blue);
        DrawTrajectoryGizmos(_planTrajectory, Color.white, Color.green);
    }

    private void DrawTrajectoryGizmos(Trajectory trajectory, Color color, Color colorStable)
    {
        if (trajectory == null || trajectory.isEmpty())
        {
            return;
        }

        var prevPosition = trajectory.points.Head.Item1;
        foreach (var position in trajectory.Positions(1))
        {
            Gizmos.color = color;
            if (trajectory.isStable)
            {
                Gizmos.color = colorStable;
            }
            // Handles.DrawWireDisc(position, Vector3.up, 0.1f);
            Gizmos.DrawLine(prevPosition, position);
            prevPosition = position;
        }

        if (!trajectory.isEmpty() && trajectory.IsInterupted())
        {
            Handles.color = Color.red;
            Handles.DrawWireDisc(trajectory.points.Tail.Item1, Vector3.up, radius);
        }
        
        if (trajectory.isAnalyzed)
        {
            Handles.color = Color.magenta;
            Handles.DrawWireDisc(new Vector3(trajectory.orbitPoint.x, 0, trajectory.orbitPoint.y),  Vector3.up, orbitPointSize);    
        }
        
            
    }

    public bool HasPlan()
    {
        return !_planTrajectory.isEmpty();
    }

    public void ScrapPlan()
    {
        _planTrajectory.Reset();
        UnFreeze();
    }

    public void Revive()
    {
        dead = false;
        transform.position = savePosition;
        velocity = saveVelocity;
        RecalculateTrajectory();
        UnFreeze();
    }
}