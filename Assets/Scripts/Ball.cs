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

    private const int Capacity = 2000;    
    public RingBuffer<Tuple<Vector3, Vector3, float>> trajectory;

    private float dtSince;
    public void PlaceInOrbit(World world)
    {
        this.world = world;
        PlaceInOrbit();
    }
    public void PlaceInOrbit()
    {
        var planets = world.allPlanets.FindAll(p => p.mass > 0);
        if (planets.Count == 0)
        {
            return;
        }

        var planet = planets[Random.Range(0, planets.Count)];
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
    }

    private Vector2 oldPos = Vector2.zero;
    private Vector2 orbitPoint = Vector2.zero;
    public int revolutions = 0;

    public void OnCollided()
    {
        frozen = true;
        dead = true;
    }

    // Update is called once per frame
    void Update()
    {
        if (frozen)
        {
            return;
        }

        dtSince += Time.deltaTime;
        if (trajectory == null)
        {
            return;
        }

        Tuple<Vector3, Vector3, float> previous = null;
        while (trajectory.Length > 0 && trajectory.Head.Item3 < dtSince)
        {
            previous = trajectory.PopHead();
        }

        if (trajectory.Length != 0)
        {
            if (previous == null)
            {
                throw new InvalidDataException("Trajectory had no previous data for " + dtSince);
            }
            var next = trajectory.Head;
            if (previous.Item3 < dtSince && next.Item3 > dtSince)
            {
                var trajectoryDiff = next.Item3 - previous.Item3;
                var realDiff = dtSince - previous.Item3;
                var diffFactor = realDiff / trajectoryDiff;

                transform.position = previous.Item1 + (next.Item1 - previous.Item1) * diffFactor;
                velocity = previous.Item2 * diffFactor + next.Item2 * (1 - diffFactor);
            }    
        }
        trajectory.Prepend(previous);
        ContinueTrajectory(Vector3.zero);        
        // var acceleration = Vector3.zero;
        // if (gravityAffected)
        // {
        //     
        //     foreach (var planet in world.allPlanets)
        //     {
        //         var delta = transform.position - planet.transform.position;
        //         if (CheckCollided(delta, radius + planet.radius))
        //         {
        //             if (this is Ball ball)
        //             {
        //                 ball.OnCollided();
        //             }
        //
        //             return;
        //         }
        //
        //         if (CheckCollided(delta, radiusGravity + planet.radiusGravity))
        //         {
        //             inOrbitAround = planet;
        //         }
        //
        //         acceleration -= CalcGravityAcceleration(delta, mass, planet);
        //     }
        //
        //     if (inOrbitAround)
        //     {
        //         var delta = transform.position - inOrbitAround.transform.position;
        //         if (!CheckCollided(delta, radiusGravity + inOrbitAround.radiusGravity))
        //         {
        //             inOrbitAround = null;
        //         }
        //     }
        // }
        //
        // if (moving)
        // {
        //     // ApplyGravity();
        //     var dt = (float) Math.Round(Time.deltaTime, 3);
        //     velocity += acceleration * dt;
        //     transform.Translate(velocity * dt);
        // }

        OnUpdate();
    }

    private void OnUpdate()
    {
        if (velocity != Vector3.zero)
        {
            movingParticleSystem.transform.LookAt(transform.position + velocity);
        }

        var emissionModule = movingParticleSystem.emission;
        emissionModule.enabled = !frozen && velocity.sqrMagnitude > 0.5;

        var nearbyPlanets = world.allPlanets.FindAll(p => (p.transform.position - transform.position).sqrMagnitude < Math.Pow(p.radiusGravity, 2));
        var newPos = new Vector2(transform.position.x, transform.position.z);
        foreach (var nearbyPlanet in nearbyPlanets)
        {
            var planetPos = nearbyPlanet.transform.position;

            if (LinesUtil.LineSegmentsIntersection(oldPos, newPos, new Vector2(planetPos.x, planetPos.z), new Vector2(planetPos.x, planetPos.z + nearbyPlanet.radiusGravity), out Vector2 intersection))
            {
                if ((orbitPoint - intersection).sqrMagnitude < 0.1)
                {
                    revolutions++;
                    if (revolutions > 2)
                    {
                        if (!inStableOrbit)
                        {
                            savePosition = transform.position;
                            saveVelocity = velocity;
                        }

                        inStableOrbit = true;
                    }
                }
                else
                {
                    inStableOrbit = false;
                    orbitPoint = intersection;
                    revolutions = 0;
                }
            }
        }

        oldPos = newPos;
    }

    public void Freeze()
    {
        frozen = true;
    }

    public void UnFreeze()
    {
        frozen = false;
    }

    public void Bump(Vector3 dv)
    {
        orbitPoint = oldPos;
        inStableOrbit = false;
        frozen = false;
        velocity += dv;
    }

    public void CheatJumpTo(Vector3 pos)
    {
        transform.position = pos;
        velocity = Vector3.zero;
        frozen = true;
        savePosition = pos;
        saveVelocity = velocity;
    }

    public void EngangeBreaks()
    {
        velocity *= 0.8f;
        breakParticleSystem.Play();
        ResetTrajectory();
        ContinueTrajectory(Vector3.zero);
        // TODO sounds
    }

    public void PrepareBump()
    {
        Freeze();
        if (dead)
        {
            dead = false;
            transform.position = savePosition;
            velocity = saveVelocity;
        }
        else if (inStableOrbit)
        {
            savePosition = transform.position;
            saveVelocity = velocity;
        }
    }
    
    public RingBuffer<Tuple<Vector3, Vector3, float>> GenerateTrajectory(float maxBumpSpeed, Vector3 hoverPosition, float holdingTime)
    {
        Vector3 ballPos = transform.position;
        var bumpSpeed = BumpSpeed(ballPos, velocity, maxBumpSpeed, hoverPosition, holdingTime);
        ResetTrajectory();
        ContinueTrajectory(bumpSpeed);
        return trajectory;
    }

    private void ResetTrajectory()
    {
        trajectory = new RingBuffer<Tuple<Vector3, Vector3, float>>(Capacity);
        dtSince = 0;
    }

    public void ContinueTrajectory(Vector3 bumpSpeed)
    {
        if (trajectory.Length == 0)
        {
            trajectory.Append(new Tuple<Vector3, Vector3, float>(transform.position, velocity - bumpSpeed, 0f));    
        }

        for (int i = 0; i < trajectory.Capacity - trajectory.Length; i++)
        {
            var lastBufferItem = trajectory.Tail;
            if (!GetAcceleration(world, lastBufferItem.Item1, radius, mass, out var acceleration))
            {
                return;
            }

            var dT = 0.25f / lastBufferItem.Item2.magnitude;
            var newSpeed = lastBufferItem.Item2 + acceleration * dT;
            var newPosition = lastBufferItem.Item1 + newSpeed * dT;
            var newDT = lastBufferItem.Item3 + dT;

            trajectory.Append(new Tuple<Vector3, Vector3, float>(newPosition, newSpeed, newDT));
        }
    }

    private static bool GetAcceleration(World world, Vector3 ballPos, float ballRadius, float ballMass, out Vector3 acceleration)
    {
        acceleration = Vector3.zero;
        foreach (var planet in world.allPlanets)
        {
            var delta = ballPos - planet.transform.position;
            if (CheckCollided(delta, planet.radius + ballRadius))
            {
                return false;
            }

            acceleration -= CalcGravityAcceleration(delta, ballMass, planet);
        }

        return true;
    }

    public static Vector3 BumpSpeed(Vector3 ballPos, Vector3 ballVelocity, float maxSpeed, Vector3 hover, float holdingTime)
    {
        var minSpeed = 1.2f;

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
}