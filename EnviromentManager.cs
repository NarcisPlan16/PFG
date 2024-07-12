using System.Collections;
using System.Collections.Generic;
using System.Collections.Concurrent;
using System.Linq;

using Unity.MLAgents;
using UnityEngine;
using UnityEditor;

public class EnviromentManager : MonoBehaviour {

    public List<ColorToVegetation> mappings;
    public Material plane_material;
    public string mappings_filename;
    public Texture2D height_map;
    public Texture2D vegetation_map;
    public int n_enviroments = 1;

    private Texture2D preprocessed_map;
    private Color[] map_pixels;
    private Dictionary<Color, ColorToVegetation> mappings_dict;
    private const string JSON_Dir = "./Assets/Resources/JSON/";
    private MapPreprocessing map_preprocessing = new MapPreprocessing();
    private ConcurrentBag<int> agents_ready = new ConcurrentBag<int>();
    private ConcurrentBag<int> n_agents = new ConcurrentBag<int>();
    private bool preprocessing_done = false;

    void Start() {
        Academy.Instance.AutomaticSteppingEnabled = false;
        //Preprocessing();
    }

    public void AddAgent() {
        n_agents.Add(1);
    }

    public void AddAgentReady() {
        agents_ready.Add(1);
    }

    private void Update() {

        if (agents_ready.Sum() == n_agents.Sum() && n_agents.Sum() == n_enviroments) {

            agents_ready.Clear();
            Academy.Instance.EnvironmentStep();

        }

    }

    public void Preprocessing() {

        map_preprocessing.Start(vegetation_map);
        map_preprocessing.CalculateColorMappings();

        preprocessed_map = map_preprocessing.ObtainProcessedMap();
        map_pixels = preprocessed_map.GetPixels(); // Save the original so we can restore the map
        
        preprocessing_done = true;
    }

    public void WaitEnviromentInit() {
        while (!preprocessing_done);
    }

    public Texture2D HeightMap() {
        return height_map;
    }

    public Material MapMaterial() {
        return new Material(plane_material);
    }

    public Color GetPixel(int x, int y) {
        return preprocessed_map.GetPixel(x, y);
    }

    public Texture2D VegetationMapTexture() {
        Texture2D new_tex = new Texture2D(preprocessed_map.width, preprocessed_map.height, TextureFormat.RGBA32, false);
        new_tex.SetPixels(preprocessed_map.GetPixels());
        new_tex.Apply();

        return new_tex;
    }

    public Dictionary<Color, ColorToVegetation> ObtainEditorMappingsDict() {

        Dictionary<Color, ColorToVegetation> dict = new Dictionary<Color, ColorToVegetation>();
        foreach (ColorToVegetation mapping in mappings) {
            dict.Add(mapping.color, mapping);
        }

        return dict;
    }

    public void CalculateColorMappings() {

        Preprocessing(); // TODO: Paralelitzar per fer-lo més ràpid
        mappings = map_preprocessing.ObtainMappingsAsList();

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