using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Piece : MonoBehaviour
{
    public string colour;
    public string type;

    void Start() {
        string[] nameSplit = gameObject.name.Split('_');
        colour = nameSplit[0];
        type = nameSplit[1];
    }

    public string getColour() {
        return colour;
    }
}
