using UnityEngine;

public struct PathPoint
{
    public Vector2 Position;
    public Quaternion Rotation;
    public float DistanceFromLast; 
    public bool IsTeleport;         

    public PathPoint(Vector2 pos, Quaternion rot, float dist, bool isTeleport = false)
    {
        Position = pos;
        Rotation = rot;
        DistanceFromLast = dist;
        IsTeleport = isTeleport;
    }
}