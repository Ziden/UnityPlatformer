using System;
using UnityEngine;

public enum Direction
{
    UP, DOWN, LEFT, RIGHT, UP_RIGHT, DOWN_RIGHT, UP_LEFT, DOWN_LEFT
}

public static class DirectionHelper
{
    public static Direction GetDirection(Vector2 t1, Vector2 t2)
    {
        var horizDistance = t1.x - t2.x;
        var vertDistance = t1.y - t2.y;

        if (Math.Abs(horizDistance) > Math.Abs(vertDistance))
            if (horizDistance > 0)
                return Direction.RIGHT;
            else
                return Direction.LEFT;
        else
        {
            if (vertDistance > 0)
                return Direction.UP;
            else
                return Direction.DOWN;
        }
    }
}
