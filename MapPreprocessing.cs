using System.Collections;
using System.Collections.Generic;
using System;
using System.IO;
using BitMiracle.LibTiff.Classic;

using UnityEngine;
using UnityEditor;

public class MapPreprocessing : MonoBehaviour {

    private ColorVegetationMapper colorVegMapper = new ColorVegetationMapper();
    public List<ColorToVegetation> colorVegetationMappings = new List<ColorToVegetation>();

    public float threshold = 0.297f;
    public Texture2D vegetationMap; 


    void Start() {
        colorVegMapper = new ColorVegetationMapper() {colorThreshold = threshold};
    }

    public void CalculateColorMappings() {

        colorVegetationMappings.Clear();
        ObtainColorsFromJSON("Assets/Resources/JSON/unique_colors.json"); // Stores the colors into the colorVegetationMappings

        // Create a new texture with the same dimensions, but uncompressed
        /*
        Texture2D uncompressedTexture = new Texture2D(vegetationMap.width, vegetationMap.height, TextureFormat.RGBA32, false);
        uncompressedTexture.SetPixels(vegetationMap.GetPixels());
        uncompressedTexture.Apply();

        byte[] fileData = uncompressedTexture.EncodeToJPG(100);
        System.IO.File.WriteAllBytes(Application.dataPath + "/read_map.jpg", fileData);
        */

        colorVegMapper.MapToTargetColors(colorVegetationMappings, vegetationMap);
        colorVegetationMappings = colorVegMapper.ObtainMappings();

        Texture2D new_map = new Texture2D(vegetationMap.width, vegetationMap.height); // Create a new Texture2D to store the new Texture2D with the mapped colors

        for (int i = 0; i < vegetationMap.width; i++) {
            for (int j = 0; j < vegetationMap.height; j++) {

                Color original_color = vegetationMap.GetPixel(i, j);
                foreach (ColorToVegetation mapping in colorVegetationMappings) {
                    if (mapping.Contains(original_color)) {
                        new_map.SetPixel(i, j, mapping.color); // Replace the original color with the mapped color
                        break; // Exit the foreach loop once a mapping is found
                    }

                }

            }
        }

        new_map.Apply(); // Apply changes to the mapped texture
        byte[] fileData = new_map.EncodeToJPG(100);
        System.IO.File.WriteAllBytes(Application.dataPath + "/new_map.jpg", fileData);
        
    }

    private void ObtainColorsFromJSON(string file_path) {
        
        if (File.Exists(file_path)) {

            string jsonContent = File.ReadAllText(file_path);
            int[][] colors = ParseArrayString(jsonContent);

            foreach (int[] rgb_c in colors) {
                
                // Divide by 255 as the Texture2D color format in unity is between 0 and 1 and not between 0 and 255
                Color new_color = new Color(rgb_c[0] / 255, rgb_c[1] / 255, rgb_c[2] / 255); 
                ColorToVegetation aux = new ColorToVegetation() {color = new_color};
                aux.AddToMappedColors(new_color);

                colorVegetationMappings.Add(aux);
            }

        }

    }

    static int[][] ParseArrayString(string arrayString) {

        // Remove outer square brackets and split by "], ["
        string[] rows = arrayString.Trim('[', ']').Split(new[] { "], [" }, StringSplitOptions.None);
        
        int[][] result = new int[rows.Length][];
        for (int i = 0; i < rows.Length; i++) {

            string[] elements = rows[i].Split(new[] { ", " }, StringSplitOptions.None); // Split each row by ", "
            result[i] = new int[elements.Length];

            for (int j = 0; j < elements.Length; j++) {
                result[i][j] = int.Parse(elements[j]); // Parse each element to an integer
            }
        }

        return result;
    }

}

[CustomEditor(typeof(MapPreprocessing))]
public class MapPreprocessingEditor : Editor {
    public override void OnInspectorGUI() {
        DrawDefaultInspector();

        MapPreprocessing myScript = (MapPreprocessing)target;

        if (GUILayout.Button("Calculate Color Mappings")) {
            myScript.CalculateColorMappings();
        }
    }
}