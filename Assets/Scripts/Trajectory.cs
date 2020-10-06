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
    public float rApoapsis;
    public float rPeriapsis;
    public Vector3 e;
    
    public RingBuffer<Tuple<Vector3, Vector3, float>> points = new RingBuffer<Tuple<Vector3, Vector3, float>>(Capacity);

    public Trajectory Reset()
    {
        points = new RingBuffer<Tuple<Vector3, Vector3, float>>(Capacity);
        isAnalyzed = false;
        isStable = false;
        return this;
    }

    public bool Analyze(Planet orbitAround, Ball ball)
    {
        if (isAnalyzed || !orbitAround) return isStable;
        
        // v = sqrt(GM * (2/r - 1/a))
        // v² = GM * (2r -1/a)
        // v²/GM = 2/r-1/a
        // (v²/GM -2/r) = -1/a
        // a = -1 / (v²/GM -2/r)
        
        
        var planetPos = orbitAround.transform.position;

        var r = (points.Head.Item1 - planetPos);
        var v = points.Head.Item2;

        // h = Vector3.Cross(r, v);
        // var nhat = Vector3.Cross(Vector3.forward, h);

        var mue = (GravityObject.G * orbitAround.mass);
        e = ((v.sqrMagnitude - mue / r.magnitude) * r - (Vector3.Dot(r, v) * v)) / mue;
        var eMagnitude = e.magnitude;
        // omega = Mathf.Acos(Vector3.Dot(nhat, e) / nhat.magnitude * eMagnitude);

        
        var semiMajoral = -1f / ((points.Head.Item2.sqrMagnitude / mue) -
                              2f / r.magnitude);
        rApoapsis = semiMajoral * (1 + eMagnitude);
        rPeriapsis = semiMajoral * (1 - eMagnitude);
        if (eMagnitude < 1)
        {
            isStable = rApoapsis < orbitAround.radiusGravity - ball.radius &&
                       rPeriapsis > orbitAround.radius * 1.5 + ball.radius;
        }
        else
        {
            isStable = false;
        }
        
        // e = 1 - (2/ ((ra/rp)+1))
        // e = 1 - (2/ (((a*(1-e))/(a*(1+e)))+1))
        // Debug.Log("e: " + Math.Round(eMagnitude, 3) + " a:" +Math.Round(semiMajoral, 3) + " rMax:" + orbitAround.radiusGravity + " stable: " + isStable);

        isAnalyzed = true;
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
                throw new InvalidDataException($"Trajectory had no previous data for {dtSince}");
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
                throw new InvalidDataException($"Trajectory had no previous data for {dtSince}");
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
                if (TrajectoryUtil.CheckCollided(delta, planet.radius * 1.5f + ball.radius))
                {
                    atmosphere = 0.3f;
                }
                acceleration = -TrajectoryUtil.CalcGravityAcceleration(delta, ball.mass, planet);
                
                if (acceleration.sqrMagnitude == 0)
                {
                    dT = 0.7f / lastSpeed;
                }
                else
                {
                    // ((a * t) +v) * t = d
                    // x = sqrt(4ad+v²) + v / 2a
                    // or when a = 0 => d / v
                    var accelerationMagnitude = acceleration.magnitude;
                    dT = (float) ((Math.Sqrt((4 * accelerationMagnitude * 0.25f) + Math.Pow(lastSpeed, 2)) - lastSpeed) / (2 * accelerationMagnitude));

                    Vector3 planetPos = planet.transform.position;
                    var orbitRadius = (lastBallPos - planetPos).magnitude;
                    // dT *= Math.Min((float) Math.Pow(magnitude / orbitAroundRadius, 2), 1);
                    // var mul = (float) Math.Pow(magnitude / orbitAroundRadius, 2); // radius ratio squared
                    // var mul = (float) magnitude / orbitAroundRadius; // radius ratio
                    var mul = (float) (1 / orbitRadius) + 0.1f; // reverse radius ratio 
                    dT *= Math.Max(Math.Min(mul, 1), 0.1f);
                    // dT *= Math.Min((float) magnitude / orbitAroundRadius * 5, 1);
                }
            }
            else
            {
                dT = 0.7f / lastSpeed;
            }
            
            if (lastSpeed == 0 && acceleration.sqrMagnitude == 0)
            {
                return this;
            }

            var newVelocity = (lastBufferItem.Item2 + acceleration * dT) * (float) Math.Pow(atmosphere, dT);
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

    public bool IsEmpty()
    {
        return points.Length == 0;
    }
}