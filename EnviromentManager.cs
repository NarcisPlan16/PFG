using System.Collections;
using System.Collections.Generic;
using System.Linq;

using UnityEngine;
using UnityEditor;

public class EnviromentManager : MonoBehaviour {

    public List<ColorToVegetation> mappings;
    public Material plane_material;
    public string mappings_filename;
    public Texture2D height_map;
    public Texture2D vegetation_map;

    private Color[] map_pixels;
    private Dictionary<Color, ColorToVegetation> mappings_dict;
    private const string JSON_Dir = "./Assets/Resources/JSON/";

    // Start is called before the first frame update
    void Start() {
        
    }

    public Texture2D HeightMap() {
        return height_map;
    }

    public Material MapMaterial() {
        return plane_material;
    }

    public Texture2D VegetationMapTexture() {
        return vegetation_map;
    }

    public Dictionary<Color, ColorToVegetation> Mappings() {

        Dictionary<Color, ColorToVegetation> dict = new Dictionary<Color, ColorToVegetation>();
        foreach (ColorToVegetation mapping in mappings) {
            dict.Add(mapping.color, mapping);
        }

        return dict;
    }

    public void CalculateColorMappings() {

        MapManager map_manager = new MapManager();
        mappings_dict = map_manager.Preprocessing(vegetation_map); // TODO: Paralelitzar per fer-lo més ràpid
        mappings = mappings_dict.Values.ToList();

        vegetation_map = map_manager.GetMap();
        plane_material.mainTexture = vegetation_map;
    }

    public void StoreMappings() {

        MapManager map_manager = new MapManager();

        map_manager.SaveMappings(mappings_dict);
        map_manager.StoreMappings(JSON_Dir + mappings_filename);
    }

    public void LoadMappings() {

        MapManager map_manager = new MapManager();

        map_manager.LoadMappings(JSON_Dir + mappings_filename);
        mappings_dict = map_manager.GetMappings();

        mappings = mappings_dict.Values.ToList();
    }
}

[CustomEditor(typeof(EnviromentManager))]
public class EnviromentManagerEditor : Editor {
    public override void OnInspectorGUI() {
        DrawDefaultInspector();

        EnviromentManager myScript = (EnviromentManager)target;
        if (GUILayout.Button("Calculate Color Mappings")) {
            myScript.CalculateColorMappings();
        }
        if (GUILayout.Button("Save mappings")) {
            myScript.StoreMappings();
        }
        if (GUILayout.Button("Load mappings")) {
            myScript.LoadMappings();
        }
    }

}