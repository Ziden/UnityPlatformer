using System;
using UnityEngine;

public static class TransformUtils
{
    public static void Flip(PlayerBhv player)
    {
         player.facingRight = !player.facingRight;
         Vector3 theScale = player.transform.localScale;
         theScale.x *= -1;
         player.transform.localScale = theScale;
    }

    public static void Flip(Transform t)
    {
        Vector3 theScale = t.transform.localScale;
        theScale.x *= -1;
        t.transform.localScale = theScale;
    }

    public static void FaceRight(PlayerBhv bhv, bool right)
    {
        if(bhv.facingRight != right)
        {
            Flip(bhv);
        }
    }
}
