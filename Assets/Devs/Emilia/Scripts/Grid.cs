using System.Collections;
using System.Collections.Generic;
using UnityEngine;

//CodeMonkey Tutorial used: https://youtu.be/waEsGu--9P8?si=ZLuCPjr35OC_M_Er

public class Grid
{
    private int width;
    private int height;
    private int[,] gridArray;

    public Grid(int width, int height)
    {
        this.width = width;
        this.height = height;

        gridArray = new int[width, height];

        for (int x = 0; x < gridArray.GetLength(0); x++)
        {
            for (int y = 0; y < gridArray.GetLength(1); y++)
            {
                Debug.Log(x + "," + y);
            }
        }
    }
}
