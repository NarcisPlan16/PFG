using System.Collections;
using System.Collections.Generic;
using System;
using System.Collections.Concurrent;
using System.Linq;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

using UnityEngine;
using UnityEditor;
using UnityEngine.Events;

[ExecuteInEditMode]
public class FireMapsPreparation : MonoBehaviour {

    public Texture2D input_vegetation_map;
    public Texture2D height_map;
    public GameObject plane; // Reference to the terrain object
    public Material plane_material;
    public int MAX_FIRE_SPAN = 3000;
    public Dictionary<Color, ColorToVegetation> mappings;
    public string mappings_filename;
    public int n_normal_fires = 2;


    private Texture2D actual_map;
    private MapManager map_manager = new MapManager();
    private Material map_material;
    private const int MAX_BURN_PRIO = 5;
    private const string JSON_Dir = "./Assets/Resources/JSON/";
    private FireSimulator fire_sim;

    [System.Serializable]
    public struct FireData {

        public FireData(int x, int y, float cost, Vector3 wind, float humidity, float temperature, int span, int pixels) {
            this.init_x = x;
            this.init_y = y;
            this.total_cost = cost;
            this.wind_dir = wind;
            this.max_span = span;
            this.pixels_burnt = pixels;
            this.humidity_percentage = humidity;
            this.temperature = temperature;
        }

        public int init_x;
        public int init_y;
        public float total_cost;
        public int pixels_burnt;
        public Vector3 wind_dir;
        public float humidity_percentage;
        public float temperature;
        public int max_span; // Maximum timespan simulated
    }

    public void PrepareFireCollection() {

        
        map_material = plane.GetComponent<MeshRenderer>().material;

        map_manager.plane = plane;
        map_manager.Preprocessing(input_vegetation_map); // TODO: Paralelitzar per fer-lo més ràpid
        actual_map = map_manager.GetMap();
        map_material.mainTexture = actual_map;

        LoadMappings();

        int n_extreme_cases = (int) Mathf.Round(n_normal_fires * 0.02f);
        n_extreme_cases = (int) Mathf.Clamp(n_extreme_cases, 1f, n_normal_fires * 0.02f); // Ensure at least 1
        Debug.Log("AAAAAA: " + n_extreme_cases);
        (int, int)[] humidity_values = new (int, int)[] {(0, 30), (50, 100)}; // normality range: (30, 50)
        (int, int)[] temperature_values = new (int, int)[] {(0, 30), (40, 60)}; // normality range: (30, 40)

        int total_files = 0;
        foreach ((int, int) hum_range in humidity_values) {
            foreach ((int, int) temp_range in temperature_values) {

                PrepareNFires(total_files, 
                            n_extreme_cases, 
                            hum_range.Item1, 
                            hum_range.Item2, 
                            temp_range.Item1, 
                            temp_range.Item2);

                total_files += n_extreme_cases;
            }
        }

        PrepareNFires(total_files, n_normal_fires+total_files, 30, 50, 30, 40); // Generate n_normal_fires number of normality values simulations
    }

    public void PrepareNFires(int ini_i, int number_of_fires, int min_humidity, int max_humidity, int min_temp, int max_temp) {

        for (int i = ini_i; i < number_of_fires+ini_i; i++) {

            Vector3 wind = new Vector3();
            wind.x = UnityEngine.Random.Range(-60, 60);
            wind.y = UnityEngine.Random.Range(-60, 60); // TODO: Does vertical (height) wind affect?
            wind.z = UnityEngine.Random.Range(-60, 60);

            int x_ini = UnityEngine.Random.Range(0, actual_map.width);
            int y_ini = UnityEngine.Random.Range(0, actual_map.height);
            float humidity = UnityEngine.Random.Range(min_humidity, max_humidity);
            float temperature = UnityEngine.Random.Range(min_temp, max_temp);

            fire_sim = new FireSimulator(mappings, wind, humidity, temperature);
            Vector2 init = fire_sim.InitRandomFire(actual_map, map_material);

            bool fire_ended = false;
            while (!fire_ended) {
                fire_ended = fire_sim.ExpandFire(MAX_FIRE_SPAN, height_map, actual_map, map_material);
            }
            
            FireData fire_data = new FireData(x_ini, 
                                              y_ini, 
                                              fire_sim.GetReward(), 
                                              wind, 
                                              humidity, 
                                              temperature, 
                                              MAX_FIRE_SPAN, 
                                              fire_sim.TotalPixelsBurnt());

            string jsonData = JsonUtility.ToJson(fire_data);
            System.IO.File.WriteAllText(JSON_Dir+"SampleMaps/fire_"+i+".json", jsonData);

            map_manager.ResetMap();
            actual_map = map_manager.GetMap();
            Debug.Log("Calulated Reward: " + fire_sim.GetReward());
            Debug.Log("**********************************");
        }

    }

    public void CalculateColorMappings() {

        map_material = plane.GetComponent<MeshRenderer>().sharedMaterial;
        map_manager.plane = plane;
        mappings = map_manager.Preprocessing(input_vegetation_map);

        actual_map = map_manager.GetMap();

    }

    public void LoadMappings() {
        map_manager.LoadMappings(JSON_Dir + mappings_filename);
        mappings = map_manager.GetMappings();
    }

    private ColorToVegetation ObtainMapping(Color color) {

        ColorToVegetation mapping = new ColorToVegetation();
        if (mappings.ContainsKey(color)) {
            mapping = mappings[color];
        }
        else {

            ColorVegetationMapper col_mapper = new ColorVegetationMapper();
            col_mapper.mappings = mappings;

            mapping = col_mapper.FindClosestMapping(color);

        }

        return mapping;
    }
}

[CustomEditor(typeof(FireMapsPreparation))]
public class FireMapsPreparationEditor : Editor {
    public override void OnInspectorGUI() {

        DrawDefaultInspector();
        FireMapsPreparation myScript = (FireMapsPreparation)target;

        if (GUILayout.Button("Pregenerate fire simulations")) myScript.PrepareFireCollection();
        
    }
}
