using System.Collections;
using System.Collections.Generic;  

using UnityEngine;

[System.Serializable]
public class ColorToVegetation {
    public Color color; // Color in the vegetation map
    public GameObject vegetationPrefab; // Prefab of the vegetation
    
    [Range(0f, 1f)]
    public float spawnChance = 1f; // Probability of spawning this vegetation at a given location
    private HashSet<Color> mapped_colors = new HashSet<Color>(); // Colors mapped to this color 

    public int CompareToColor(Color c) {

        // Compare the RGB values of the colors
        if (color.g != c.g)
            return color.g.CompareTo(c.g); // Compare green components first
        else if (color.b != c.b)
            return color.b.CompareTo(c.b); // Compare blue components next
        else
            return color.r.CompareTo(c.r); // Compare red components last

    }

    public bool Contains(Color c) {

        bool res = false;
        foreach (Color mapped_color in mapped_colors) {
            if (c == mapped_color) {
                res = true;
                break;
            }
        }

        return res;
    }

    public void AddToMappedColors(Color c) {
        if (!mapped_colors.Contains(c)) mapped_colors.Add(c);
    }

    public HashSet<Color> MappedColors() {
        return mapped_colors;
    }

}    