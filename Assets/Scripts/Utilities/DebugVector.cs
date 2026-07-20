using Unity.Mathematics;
using UnityEngine;

public static class DebugVector
{
    public static void DrawArrow(float3 position, float3 vector, Color color,
        float headLength = 0.25f, float headAngle = 25f)
    {
        var end = position + vector;


        Debug.DrawLine(position, end, color);


        if (math.lengthsq(vector) < Mathf.Epsilon)
            return;

        var dir = math.normalize(vector);
        
        var up = math.abs(dir.y) < 0.99f ? new float3(0, 1, 0) : new float3(1, 0, 0);

        var right = math.normalize(math.cross(dir, up));
        var correctedUp = math.cross(right, dir);

        var rad = math.radians(headAngle);
        
        var headDir1 = math.normalize(
            math.rotate(quaternion.AxisAngle(correctedUp, rad), -dir)
        );

        var headDir2 = math.normalize(
            math.rotate(quaternion.AxisAngle(correctedUp, -rad), -dir)
        );

        var head1 = end + headDir1 * headLength;
        var head2 = end + headDir2 * headLength;

        Debug.DrawLine(end, head1, color);
        Debug.DrawLine(end, head2, color);
    }
}