using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

static class TrajectoryUtil
{
    public static bool CheckCollided(Vector3 delta, float radii)
    {
        var distanceSquared = delta.sqrMagnitude;
        var distanceBorder = Math.Pow(radii, 2);
        return distanceBorder > distanceSquared;
    }

    public static Vector3 CalcGravityAcceleration(Vector3 delta, float mass, GravityObject other)
    {
        var distanceSquared = delta.sqrMagnitude;
        if (distanceSquared >= Mathf.Pow(other.radiusGravity, 2))
        {
            return Vector3.zero;
        }

        var forceMagnitude = ((GravityObject.G * mass * other.mass) / distanceSquared);
        var acceleration = forceMagnitude / mass;
        var distance = Mathf.Sqrt(distanceSquared);
        var dax = acceleration * delta.x / distance;
        var daz = acceleration * delta.z / distance;

        return new Vector3(dax, 0, daz);
    }
}

public class Trajectory
{
    private const int Capacity = 2000;

    public bool isAnalyzed;
    public bool isStable;
    public Vector2 orbitPoint;
    
    public RingBuffer<Tuple<Vector3, Vector3, float>> points = new RingBuffer<Tuple<Vector3, Vector3, float>>(Capacity);

    public Trajectory Reset()
    {
        points = new RingBuffer<Tuple<Vector3, Vector3, float>>(Capacity);
        isAnalyzed = false;
        isStable = false;
        orbitPoint = Vector2.zero;
        return this;
    }

    public bool Analyze(Planet orbitAround, float orbitPointSize)
    {
        if (isAnalyzed || !orbitAround) return isStable;
        
        var planetPos = orbitAround.transform.position;
        var planetPos2 = new Vector2(planetPos.x, planetPos.z);
        var planetPos2Line = new Vector2(planetPos.x, planetPos.z + orbitAround.radiusGravity);

        Vector2 oldPos = new Vector2(points.Head.Item1.x, points.Head.Item1.z);
        orbitPoint = oldPos;

        int revolutions = 0;
        for (int i = 0; i < points.Length; i++)
        {
            var newPos = new Vector2(points[i].Item1.x, points[i].Item1.z);

            if (LinesUtil.LineSegmentsIntersection(oldPos, newPos, planetPos2, planetPos2Line,
                out Vector2 intersection))
            {
                if ((orbitPoint - intersection).sqrMagnitude < orbitPointSize * orbitPointSize)
                {
                    revolutions++;
                    if (revolutions > 1)
                    {
                        isStable = true;
                        isAnalyzed = true;
                        break;
                    }
                }
                else
                {
                    isStable = false;
                    orbitPoint = intersection;
                    revolutions = 0;
                }
            }

            oldPos = newPos;
        }

        return isStable;
    }

    public bool CalculateNext(float dtSince, out Vector3 position, out Vector3 velocity)
    {
        position = Vector3.zero;
        velocity = Vector3.zero;
        if (points.Length == 0)
        {
            return false;
        }

        Tuple<Vector3, Vector3, float> previous = null;
        while (points.Length > 0 && points.Head.Item3 < dtSince)
        {
            previous = points.PopHead();
        }

        if (points.Length != 0)
        {
            if (previous == null)
            {
                throw new InvalidDataException("Trajectory had no previous data for " + dtSince);
            }

            var next = points.Head;
            if (previous.Item3 < dtSince && next.Item3 > dtSince)
            {
                var trajectoryDiff = next.Item3 - previous.Item3;
                var realDiff = dtSince - previous.Item3;
                var diffFactor = realDiff / trajectoryDiff;

                position = diffFactor == 0
                    ? previous.Item1
                    : previous.Item1 + (next.Item1 - previous.Item1) * diffFactor;
                velocity = diffFactor == 0
                    ? previous.Item2
                    : previous.Item2 * diffFactor + next.Item2 * (1 - diffFactor);
            }
        }
        else
        {
            if (previous == null)
            {
                throw new InvalidDataException("Trajectory had no previous data for " + dtSince);
            }
            position = previous.Item1;
            velocity = previous.Item2;
            Debug.Log("Trajectory is Empty");
        }

        points.Prepend(previous);
        return true;
    }

    public Planet FindPlanetAround(Planet current, World world, float ballRadius, Vector3 pos, out Vector3 delta)
    {
        delta = Vector3.zero;
        if (current != null)
        {
            delta = pos - current.transform.position;
            if (TrajectoryUtil.CheckCollided(delta, current.radiusGravity))
            {
                return current;
            }
        }
        
        foreach (var planet in world.allPlanets)
        {
            delta = pos - planet.transform.position;
            if (TrajectoryUtil.CheckCollided(delta, planet.radiusGravity))
            {
                return planet;
            }
        }

        return current;
    }
    
    public Trajectory Continue(Vector3 bumpSpeed, Ball ball)
    {
        if (points.Length == 0)
        {
            points.Append(new Tuple<Vector3, Vector3, float>(ball.transform.position, ball.velocity - bumpSpeed, 0f));
        }
        var lastBufferItem = points.Tail;

        Planet planet = FindPlanetAround(null, ball.world, ball.radius, lastBufferItem.Item1, out _);

        var initialLength = points.Length;
        for (int i = 0; i < points.Capacity - initialLength; i++)
        {
            lastBufferItem = points.Tail;
            var acceleration = Vector3.zero;
            var lastBallPos = lastBufferItem.Item1;
            planet = FindPlanetAround(planet, ball.world, ball.radius, lastBallPos, out var delta);
            var lastSpeed = lastBufferItem.Item2.magnitude;
            float dT;
            var atmosphere = 1f;
            if (planet != null)
            {
                if (TrajectoryUtil.CheckCollided(delta, planet.radius + ball.radius))
                {
                    return this;
                }
                if (TrajectoryUtil.CheckCollided(delta, planet.radius * 2 + ball.radius))
                {
                    atmosphere = 0.9f;
                }
                acceleration = -TrajectoryUtil.CalcGravityAcceleration(delta, ball.mass, planet);
                
                // ((a * t) +v) * t = d
                // x = sqrt(4ad+v²) + v / 2a
                // or when a = 0 => d / v
                var accelerationMagnitude = acceleration.magnitude;
                dT = (float) ((Math.Sqrt((4 * accelerationMagnitude * 0.25f) + Math.Pow(lastSpeed, 2)) - lastSpeed) /  (2 * accelerationMagnitude));

                Vector3 planetPos = planet.transform.position;
                var gravityWellRadius = planet.radiusGravity;
                var orbitRadius = (lastBallPos - planetPos).magnitude;
                // dT *= Math.Min((float) Math.Pow(magnitude / orbitAroundRadius, 2), 1);
                // var mul = (float) Math.Pow(magnitude / orbitAroundRadius, 2); // radius ratio squared
                // var mul = (float) magnitude / orbitAroundRadius; // radius ratio
                var mul = (float) (1 / orbitRadius) + 0.1f; // reverse radius ratio 
                dT *= Math.Max(Math.Min(mul, 1), 0.1f); 
                // dT *= Math.Min((float) magnitude / orbitAroundRadius * 5, 1);
                if (acceleration.sqrMagnitude == 0)
                {
                    dT = 0.7f / lastSpeed;
                }
            }
            else
            {
                dT = 0.7f / lastSpeed;
            }

            var newVelocity = (lastBufferItem.Item2 + acceleration * dT) * atmosphere;
            var newPosition = lastBufferItem.Item1 + newVelocity * dT;
            var newDT = lastBufferItem.Item3 + dT;
            
            if ((lastBallPos - newPosition).sqrMagnitude > 1)
            {
                Debug.Log("WTF");
            }

            points.Append(new Tuple<Vector3, Vector3, float>(newPosition, newVelocity, newDT));
        }

        return this;
    }


    public List<Vector3> Positions(int modulo)
    {
        var list = new List<Vector3>();
        for (int i = 0; i < points.Length; i++)
        {
            if (i % modulo == 0 || i == points.Length - 1)
            {
                list.Add(points[i].Item1);
            }
        }

        return list;
    }

    public bool IsInterupted()
    {
        return points.Length < points.Capacity;
    }

    public bool isEmpty()
    {
        return points.Length == 0;
    }
}