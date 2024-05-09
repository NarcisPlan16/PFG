using System.Collections;
using System.Collections.Generic;
using System.IO;

using UnityEngine;

public class MapManager {

    private MapPreprocessing map_preprocessing = new MapPreprocessing();
    private Texture2D map;
    private Texture2D original_map;
    private List<ColorToVegetation> mappings = new List<ColorToVegetation>();
    public GameObject plane;

    public List<ColorToVegetation> Preprocessing(Texture2D input_map, Material mat) {

        map_preprocessing.Start(input_map);
        map_preprocessing.CalculateColorMappings();

        map = map_preprocessing.ObtainProcessedMap();

        original_map = map; // Save the original so we can restore the map
        mat.mainTexture = map;
        mappings = map_preprocessing.ObtainMappings();
        
        return mappings;
    }

    public List<ColorToVegetation> GetMappings() {
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

    public void StoreMappings(List<ColorToVegetation> new_mappings) {
        mappings = new_mappings;
    }

    public void ResetMap() {
        map = original_map;
        //map_material.mainTexture = original_map;
    }
   
}