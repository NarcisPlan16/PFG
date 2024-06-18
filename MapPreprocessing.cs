using System.Collections;
using System.Collections.Generic;
using System;
using System.IO;
using BitMiracle.LibTiff.Classic;
using System.Linq;

using UnityEngine;

public class MapPreprocessing {

    private ColorVegetationMapper colorVegMapper = new ColorVegetationMapper();
    private Texture2D work_map;
    private Dictionary<Color, ColorToVegetation> colorVegetationMappings = new Dictionary<Color, ColorToVegetation>();
    private Texture2D vegetationMap; 

    public void Start(Texture2D input_map) {
        colorVegMapper = new ColorVegetationMapper();
        vegetationMap = input_map;
    }

    public void CalculateColorMappings() {

        colorVegetationMappings.Clear();
        ObtainColorsFromJSON("Assets/Resources/JSON/unique_colors.json"); // Stores the colors into the colorVegetationMappings
        colorVegMapper.MapToTargetColors(colorVegetationMappings, vegetationMap);
        colorVegetationMappings = colorVegMapper.ObtainMappings();

        Texture2D new_map = new Texture2D(vegetationMap.width, vegetationMap.height); // Create a new Texture2D to store the new Texture2D with the mapped colors

        for (int i = 0; i < vegetationMap.width; i++) {
            for (int j = 0; j < vegetationMap.height; j++) {

                Color original_color = vegetationMap.GetPixel(i, j);
                foreach (ColorToVegetation mapping in colorVegetationMappings.Values) {
                    if (mapping.Contains(original_color)) {
                        new_map.SetPixel(i, j, mapping.color); // Replace the original color with the mapped color
                        break; // Exit the foreach loop once a mapping is found
                    }

                }

            }
        }

        work_map = new_map;

        // Uncomment to see the readen file texture to check that the texture is stored correctly
        /*
        new_map.Apply(); // Apply changes to the mapped texture
        byte[] fileData = new_map.EncodeToJPG(100);
        System.IO.File.WriteAllBytes(Application.dataPath + "/new_map.jpg", fileData);
        */

        // Uncomment to generate a jog file with the readen color mappings (original colors) of the map
        /*
        Texture2D aux_map = new Texture2D(370, 30);
        int i_aux = 0;
        foreach (ColorToVegetation mapping in colorVegetationMappings) {
            for (int j = i_aux*10; j < i_aux*10 + 10; j++) {
                for (int k = 0; k < 30; k++) {
                    aux_map.SetPixel(j, k, mapping.color);
                }
            }
            i_aux++;
        }

        aux_map.Apply(); // Apply changes to the mapped texture
        byte[] file_data = aux_map.EncodeToJPG(100);
        System.IO.File.WriteAllBytes(Application.dataPath + "/aux_map.jpg", file_data);
        */
    }

    private void ObtainColorsFromJSON(string file_path) {
        
        if (File.Exists(file_path)) {

            string jsonContent = File.ReadAllText(file_path);
            float[][] colors = ParseArrayString(jsonContent);

            foreach (float[] rgb_c in colors) {
                
                // Divide by 255 as the Texture2D color format in unity is between 0 and 1 and not between 0 and 255
                Color new_color = new Color(rgb_c[0] / 255.0f, rgb_c[1] / 255.0f, rgb_c[2] / 255.0f); 
                ColorToVegetation aux = new ColorToVegetation() {color = new_color};
                aux.AddToMappedColors(new_color);

                colorVegetationMappings.Add(new_color, aux);
            }

        }

    }

    private static float[][] ParseArrayString(string arrayString) {

        // Remove outer square brackets and split by "], ["
        string[] rows = arrayString.Trim('[', ']').Split(new[] { "], [" }, StringSplitOptions.None);
        
        float[][] result = new float[rows.Length][];
        for (int i = 0; i < rows.Length; i++) {

            string[] elements = rows[i].Split(new[] { ", " }, StringSplitOptions.None); // Split each row by ", "
            result[i] = new float[elements.Length];

            for (int j = 0; j < elements.Length; j++) {
                result[i][j] = float.Parse(elements[j]); // Parse each element to an integer
            }
        }

        return result;
    }

    public Texture2D ObtainProcessedMap() {
        return work_map;
    }

    public Dictionary<Color, ColorToVegetation> ObtainMappings() {
        return colorVegetationMappings;
    }

    public List<ColorToVegetation> ObtainMappingsAsList() {
        return colorVegetationMappings.Values.ToList();
    }

}

// Helper class to make 2D float array serializable
[System.Serializable]
public class SerializableMapData {

    public string pixels;
    public int width;
    public int height;

    public SerializableMapData(string Pixels, int Width, int Height) {
        pixels = Pixels;
        width = Width;
        height = Height;
    }
}