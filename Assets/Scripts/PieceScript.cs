using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PieceScript : MonoBehaviour
{
    public string colour;
    public string type;
    public int width = 1; // squares wide piece currently is
    // public float left_x_edge = 0;
    // private double square_size = 1.1;
    // private double piece_size = 3;

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

    // void Update() {
    //     double current_piece_size = piece_size * width;
    //     left_x_edge = transform.position.x - (float)((square_size * width) / 2);
    // }
}
