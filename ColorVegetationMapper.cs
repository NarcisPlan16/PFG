using System.Collections;
using System.Collections.Generic;

using UnityEngine;

public class ColorVegetationMapper {

    public List<ColorToVegetation> colorVegetationMappings = new List<ColorToVegetation>();
    public float colorThreshold = 1.0f;

    public List<ColorToVegetation> ObtainMappings() {
        return colorVegetationMappings;
    }

    public void MapToTargetColors(List<ColorToVegetation> target_mappings, Texture2D vegetation_map) {
    
        colorVegetationMappings = target_mappings;
        for (int x = 0; x < 20 /*vegetation_map.width*/; x++) {
            for (int y = 0; y < 20 /*vegetation_map.height*/; y++) {

                Color pixel_color = vegetation_map.GetPixel(x, y);
                ColorToVegetation closest_target = FindClosestMapping(pixel_color); // Find the closest target color to the pixel

                closest_target.AddToMappedColors(pixel_color);

            }
        }

        foreach (ColorToVegetation mapping in colorVegetationMappings) 
           if (mapping.MappedColors().Count > 1) Debug.Log(mapping + " HAS " + mapping.MappedColors().Count + " || Mapping " + mapping.color);

    }

    private ColorToVegetation FindClosestMapping(Color color) {

        ColorToVegetation closest_mapping = new ColorToVegetation();
        float closest_distance = float.MaxValue;

        foreach (ColorToVegetation target in colorVegetationMappings) {

            float distance = ColorDistance(color, target.color);
            //Debug.Log("Color: " + color + "Target Color: "+ target.color);
            if (distance < closest_distance) {      
                closest_distance = distance;
                closest_mapping = target;
            } 

        }

        return closest_mapping;
    }

    public void ExtractColorMappings(Texture2D vegetationMap) {
            
        Color[] pixels = vegetationMap.GetPixels();
        HashSet<Color> uniqueColors = new HashSet<Color>(pixels);

        colorVegetationMappings.Clear();
        foreach (Color color in uniqueColors) {
            AddColorMapping(color);
        }

    }

    public void GenerateColorMappings(List<Color> uniqueColors) {

        foreach (Color color in uniqueColors) {
            AddColorMapping(color);
        }

    }

    public void AddColorMapping(Color newColor) {

        bool foundSimilarColor = false;
        foreach (ColorToVegetation mapping in colorVegetationMappings) {
            
            if (ColorDistance(newColor, mapping.color) < colorThreshold) { // the two colors are similar
                mapping.AddToMappedColors(newColor); // Add the color to the mapped ones that are similar
                foundSimilarColor = true;
                break;
            }
            
        }

        if (!foundSimilarColor) {

            ColorToVegetation new_mapping = new ColorToVegetation() {color = newColor};
            int pos = FindColorToVegetationPos(new_mapping); // Find its position on the list
            colorVegetationMappings.Insert(pos, new_mapping); // add the same color to the ones mapped. The list is ordered by colors
        }

    }

    public int FindColorToVegetationPos(ColorToVegetation m) {

        int pos = 0;
        foreach (ColorToVegetation mapping in colorVegetationMappings) {

            if (mapping.CompareToColor(m.color) < 0) break;
            else pos++;

        }

        return pos;
    }

    public ColorToVegetation GetColorMapping(Color pixelColor) {

        ColorToVegetation result = new ColorToVegetation();
        foreach (ColorToVegetation mapping in colorVegetationMappings) {

            if (mapping.color == pixelColor || mapping.Contains(pixelColor)) {
                result = mapping;
                break;
            }

        } 

        return result;
    }

    public float ColorDistance(Color c1, Color c2) {
        return Mathf.Sqrt(Mathf.Pow(c1.r - c2.r, 2) + Mathf.Pow(c1.g - c2.g, 2) + Mathf.Pow(c1.b - c2.b, 2));
    }
    

}

