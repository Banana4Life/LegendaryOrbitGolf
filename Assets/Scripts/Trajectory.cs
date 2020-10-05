using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

static class TrajectoryUtil
{
    public static bool GetAcceleration(World world, Vector3 ballPos, float ballRadius, float ballMass,
        out Vector3 acceleration)
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

    public static bool CheckCollided(Vector3 delta, float radii)
    {
        var distanceSquared = delta.sqrMagnitude;
        var distanceBorder = Math.Pow(radii, 2);
        return distanceBorder > distanceSquared;
    }

    public static Vector3 CalcGravityAcceleration(Vector3 delta, float mass, GravityObject other)
    {
        var distanceSquared = delta.sqrMagnitude;
        if (!(distanceSquared < Mathf.Pow(other.radiusGravity, 2)))
        {
            return Vector3.zero;
        }

        var forceMagnitude = (GravityObject.G * mass * other.mass) / distanceSquared;
        var distance = Mathf.Sqrt(distanceSquared);
        var dax = forceMagnitude * delta.x / distance / mass;
        var daz = forceMagnitude * delta.z / distance / mass;

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
                if ((orbitPoint - intersection).sqrMagnitude < orbitPointSize)
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

        points.Prepend(previous);
        return true;
    }


    public Trajectory Continue(Vector3 bumpSpeed, Ball ball)
    {
        if (points.Length == 0)
        {
            points.Append(new Tuple<Vector3, Vector3, float>(ball.transform.position, ball.velocity - bumpSpeed, 0f));
        }

        var initialLength = points.Length;
        for (int i = 0; i < points.Capacity - initialLength; i++)
        {
            var lastBufferItem = points.Tail;
            if (!TrajectoryUtil.GetAcceleration(ball.world, lastBufferItem.Item1, ball.radius, ball.mass,
                out var acceleration))
            {
                return this;
            }

            var dT = 0.25f / lastBufferItem.Item2.magnitude;
            var newSpeed = lastBufferItem.Item2 + acceleration * dT;
            var newPosition = lastBufferItem.Item1 + newSpeed * dT;
            var newDT = lastBufferItem.Item3 + dT;

            points.Append(new Tuple<Vector3, Vector3, float>(newPosition, newSpeed, newDT));
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