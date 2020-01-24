using UnityEngine;
using System.Collections;
using UnityEngine.UI;

public class PlayerDebug : MonoBehaviour
{
    public PlayerBhv player;
    public Text text;

    // Use this for initialization
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        var states = player.states.GetAll();
        var str = "";
        foreach(var state in states)
        {
            str += state.ToString() + " ";
        }
        text.text = str;

    }

    public static void DrawQuad(Rect position, Color color)
    {
        Texture2D texture = new Texture2D(1, 1);
        texture.SetPixel(0, 0, color);
        texture.Apply();
        GUI.skin.box.normal.background = texture;
        GUI.Box(position, GUIContent.none);
    }
}
