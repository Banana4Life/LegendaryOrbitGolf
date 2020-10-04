using UnityEngine;

public static class Helper
{
    public static Vector3 ToVector3(Vector2 vec)
    {
        return new Vector3(vec.x, 0, vec.y);
    }

    public static Vector3Int Floor(Vector3 v)
    {
        return new Vector3Int((int) Mathf.Floor(v.x), (int) Mathf.Floor(v.y), (int) Mathf.Floor(v.z));
    }

    public static Vector3 GridAlign(Vector3 v, float cellSize)
    {
        return Floor(v / cellSize);
    }

    public static Vector3Int GridPosition(Vector3 v, float cellSize)
    {
        return Floor(v / cellSize);
    }

    public static Vector3 WorldPosition(Vector3Int v, float cellSize)
    {
        return WorldPosition(v.x, v.y, v.z, cellSize);
    }

    public static Vector3 WorldPosition(int x, int y, int z, float cellSize)
    {
        return new Vector3(x * cellSize, y * cellSize, z * cellSize);
    }
    
    public static Vector2 FrustumDimensions(Camera c, Vector3 pos)
    {
        var distance = Vector3.Distance(c.transform.position, pos) / c.farClipPlane;
        return FrustumDimensions(c, distance);
    }
    public static Vector2 FrustumDimensions(Camera c, float distance)
    {
        var height = Mathf.Tan(c.fieldOfView * Mathf.Deg2Rad * 0.5f) * Mathf.Lerp(c.nearClipPlane, c.farClipPlane, distance) * 2f;
        var width = height * c.aspect;
        return new Vector2(width,height);
    }
}