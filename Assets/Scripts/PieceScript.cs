using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PieceScript : MonoBehaviour
{
    public string colour;
    public string type;
    public int width = 1; // squares wide piece currently is

    void Start() {
        string[] nameSplit = gameObject.name.Split('_');
        colour = nameSplit[0];
        type = nameSplit[1];
    }

    public string getColour() {
        return colour;
    }

    public int getWidth() {
        return width;
    }

    public void increaseWidth() {
        width++;
    }
}
