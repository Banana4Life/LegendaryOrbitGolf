using UnityEngine;

public static class Helper
{
    public static Vector3 ToVector3(Vector2 vec, float y = 0)
    {
        return new Vector3(vec.x, 0, vec.y);
    }
    
    public static Vector2 ToVector2(Vector3 vec)
    {
        return new Vector2(vec.x, vec.z);
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
        var distance = Vector3.Distance(c.transform.position, pos);
        return FrustumDimensions(c, distance);
    }
    
    public static Vector2 FrustumDimensions(Camera c, float distance)
    {
        float fov = c.fieldOfView * Mathf.Deg2Rad;
        float height = Mathf.Tan(fov / 2f) * distance * 2f;
        float width = height * c.aspect;
        
        return new Vector2(width,height);
    }

    public static float DistanceToFillFrustum(Camera c, Vector2 dimensions)
    {
        float fov = c.fieldOfView * Mathf.Deg2Rad;
        float width = dimensions.x;
        float height = Mathf.Max(dimensions.y, width / c.aspect);

        return height / (Mathf.Tan(fov / 2f) * 2);
    }

    public static Vector2 Min(Vector2 a, Vector2 b)
    {
        return new Vector2(Mathf.Min(a.x, b.x), Mathf.Min(a.y, b.y));
    }

    public static Vector2 Max(Vector2 a, Vector2 b)
    {
        return new Vector2(Mathf.Max(a.x, b.x), Mathf.Max(a.y, b.y));
    }

    public static Vector3 Min(Vector3 a, Vector3 b)
    {
        return new Vector3(Mathf.Min(a.x, b.x), Mathf.Min(a.y, b.y), Mathf.Min(a.z, b.z));
    }

    public static Vector3 Max(Vector3 a, Vector3 b)
    {
        return new Vector3(Mathf.Max(a.x, b.x), Mathf.Max(a.y, b.y), Mathf.Max(a.z, b.z));
    }

    public static Vector2 Clamp(Vector2 x)
    {
        return new Vector2(Mathf.Clamp(x.x, -1, 1), Mathf.Clamp(x.y, -1, 1));
    }
}