using UnityEngine;
using System.Collections;
using UnityEngine.Tilemaps;
using System.Collections.Generic;

public class CharacterCollider : MonoBehaviour
{
    public PlayerBhv player;
    public BodyPart part;
    public Transform body;

    private Rect debug;

    private ContactPoint2D[] contacts = new ContactPoint2D[1];
    private Tilemap map;

    // Use this for initialization
    void Start()
    {
        map = GameObject.FindGameObjectWithTag("Map").GetComponent<Tilemap>();
    }

    private void OnCollisionEnter2D(Collision2D col)
    {
        var grid = map.layoutGrid;

        var contact = col.contacts[0];

        var mapCell = grid.WorldToCell(transform.position);

        Direction collisionDirection = DirectionHelper.GetDirection((Vector3)mapCell, transform.position);

        player.OnCollide(part, col, mapCell);
    }

    private void OnGUI()
    {
        if(debug!=null)
            PlayerDebug.DrawQuad(debug, Color.red);
    }

    private void OnCollisionExit2D(Collision2D col)
    {
        //Debug.Log("CollisionExit");
        player.OnCollisionLeave(part, col);
    }
}

public enum BodyPart
{
    FEET, BODY, HEAD
}
