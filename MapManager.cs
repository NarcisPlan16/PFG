using System.Collections;
using System.Collections.Generic;
using System.IO;

using UnityEngine;

public class MapManager {

    private MapPreprocessing map_preprocessing = new MapPreprocessing();
    private Texture2D map;
    private Color[] original_map_pixels;
    private Dictionary<Color, ColorToVegetation> mappings;
    public GameObject plane;

    public Dictionary<Color, ColorToVegetation> Preprocessing(Texture2D input_map, Material mat) {

        map_preprocessing.Start(input_map);
        map_preprocessing.CalculateColorMappings();

        map = map_preprocessing.ObtainProcessedMap();

        original_map_pixels = map.GetPixels(); // Save the original so we can restore the map
        mat.mainTexture = map;
        mappings = map_preprocessing.ObtainMappings();
        
        return mappings;
    }

    public Dictionary<Color, ColorToVegetation> GetMappings() {
        return mappings;
    }

    public Texture2D GetMap() {
        return map;
    }

    public Color GetPixel(int x, int y) {
        return map.GetPixel(x, y);
    }

    public void SetPixel(int x, int y, Color c, Texture2D mapa, Material mat) {
        mapa.SetPixel(x, y, c);
        mapa.Apply();
        mat.mainTexture = mapa;
    }

    public void ResetMap() {
        map.SetPixels(original_map_pixels);
        map.Apply();
    }

    public void SaveMappings(Dictionary<Color, ColorToVegetation> m) {
        mappings = m;
    }

    public void StoreMappings(string file_path) {

        ColorToVegetationListWrapper list_w = new ColorToVegetationListWrapper(mappings); 

        string json_content = JsonUtility.ToJson(list_w);
        File.WriteAllText(file_path, json_content);

    }

    public void LoadMappings(string file_path) {

        string json_content = File.ReadAllText(file_path);
        ColorToVegetationListWrapper data = JsonUtility.FromJson<ColorToVegetationListWrapper>(json_content);

        mappings = data.ListToDict();
    }
   
}