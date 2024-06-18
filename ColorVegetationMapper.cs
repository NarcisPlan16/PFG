using System.Collections;
using System.Collections.Generic;
using System.Linq;

using UnityEngine;

public class ColorVegetationMapper {

    public Dictionary<Color, ColorToVegetation> mappings = new Dictionary<Color, ColorToVegetation>();
    public float colorThreshold = 1.0f;

    public Dictionary<Color, ColorToVegetation> ObtainMappings() {
        return mappings;
    }

    public void MapToTargetColors(Dictionary<Color, ColorToVegetation> target_mappings, Texture2D vegetation_map) {
    
        mappings = target_mappings;
        for (int x = 0; x < vegetation_map.width; x++) {
            for (int y = 0; y < vegetation_map.height; y++) {

                Color pixel_color = vegetation_map.GetPixel(x, y);
                ColorToVegetation closest_target = FindClosestMapping(pixel_color); // Find the closest target color to the pixel

                closest_target.AddToMappedColors(pixel_color);

            }
        }
        
    }

    public ColorToVegetation FindClosestMapping(Color color) {

        ColorToVegetation closest_mapping = mappings.Values.First();
        float last_distance = ColorDistance(color, closest_mapping.color);;

        foreach (ColorToVegetation target in mappings.Values) {

            float distance = ColorDistance(color, target.color);
            if (distance < last_distance) {      
                last_distance = distance;
                closest_mapping = target;
            } 

        }

        return closest_mapping;
    }

    public void ExtractColorMappings(Texture2D vegetationMap) {
            
        Color[] pixels = vegetationMap.GetPixels();
        HashSet<Color> uniqueColors = new HashSet<Color>(pixels);

        mappings.Clear();
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
        foreach (ColorToVegetation mapping in mappings.Values) {
            
            if (ColorDistance(newColor, mapping.color) < colorThreshold) { // the two colors are similar
                mapping.AddToMappedColors(newColor); // Add the color to the mapped ones that are similar
                foundSimilarColor = true;
                break;
            }
            
        }

        if (!foundSimilarColor) {
            ColorToVegetation new_mapping = new ColorToVegetation() {color = newColor};
            mappings.Add(newColor, new_mapping); // add the same color to the ones mapped. The list is ordered by colors
        }

    }

    public ColorToVegetation GetColorMapping(Color pixelColor) {
        return mappings[pixelColor];
    }

    public float ColorDistance(Color c1, Color c2) {
        return Mathf.Sqrt(Mathf.Pow(c1.r - c2.r, 2) + Mathf.Pow(c1.g - c2.g, 2) + Mathf.Pow(c1.b - c2.b, 2));
    }
    

}

