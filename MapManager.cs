using System.Collections;
using System.Collections.Generic;
using System.IO;

using UnityEngine;

public class MapManager {

    private MapPreprocessing map_preprocessing = new MapPreprocessing();
    private Texture2D map;
    private List<ColorToVegetation> mappings = new List<ColorToVegetation>();

    void Start() {

    }

    public void Preprocessing(Texture2D input_map) {

        map_preprocessing.Start(input_map);
        map_preprocessing.CalculateColorMappings();

        map = map_preprocessing.ObtainProcessedMap();
        mappings = map_preprocessing.ObtainMappings();
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

    public void SetPixel(int x, int y, Color c) {
        map.SetPixel(x, y, c);
    }

    public void StoreMappings(List<ColorToVegetation> new_mappings) {
        mappings = new_mappings;
    }
    
}