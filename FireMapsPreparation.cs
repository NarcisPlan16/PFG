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
    public const int MAX_FIRE_SPAN = 1000;
    public Dictionary<Color, ColorToVegetation> mappings;
    public string mappings_filename;


    private Texture2D actual_map;
    private MapManager map_manager = new MapManager();
    private Material map_material;
    private const int MAX_BURN_PRIO = 5;
    private const string JSON_Dir = "./Assets/Resources/JSON/";
    private FireSimulator fire_sim;

    [System.Serializable]
    public struct FireData {

        public FireData(int x, int y, float cost, Vector3 wind, int span, int pixels) {
            this.init_x = x;
            this.init_y = y;
            this.total_cost = cost;
            this.wind_dir = wind;
            this.max_span = span;
            this.pixels_burnt = pixels;
        }

        public int init_x;
        public int init_y;
        public float total_cost;
        public int pixels_burnt;
        public Vector3 wind_dir;
        public int max_span; // Maximum timespan simulated
    }

    public void PrepareFireCollection() {

        
        map_material = plane.GetComponent<MeshRenderer>().material;

        map_manager.plane = plane;
        map_manager.Preprocessing(input_vegetation_map, map_material); // TODO: Paralelitzar per fer-lo més ràpid
        actual_map = map_manager.GetMap();

        LoadMappings();

        List<FireData> fires_data = new List<FireData>();
        int n_fires = 1;
        for (int i = 0; i < n_fires; i++) {

            Vector3 wind = new Vector3();
            wind.x = UnityEngine.Random.Range(-60, 60);
            wind.y = UnityEngine.Random.Range(-60, 60);
            wind.z = UnityEngine.Random.Range(-60, 60);

            int x_ini = UnityEngine.Random.Range(0, actual_map.width);
            int y_ini = UnityEngine.Random.Range(0, actual_map.height);

            fire_sim = new FireSimulator(mappings, wind);
            List<int> init = fire_sim.InitRandomFire(map_manager, actual_map, map_material);

            bool fire_ended = false;
            while (!fire_ended) {
                fire_ended = fire_sim.ExpandFireRandom(MAX_FIRE_SPAN, height_map, map_manager, actual_map, map_material);
            }
            
            (float, int) res = CalcReward();
            FireData fire_data = new FireData(x_ini, y_ini, res.Item1, wind, MAX_FIRE_SPAN, res.Item2);

            string jsonData = JsonUtility.ToJson(fire_data);
            System.IO.File.WriteAllText(JSON_Dir+"SampleMaps/fire_"+i, jsonData);

            map_manager.ResetMap();
            actual_map = map_manager.GetMap();
            Debug.Log("Calulated Reward: " + res.Item1);
            Debug.Log("**********************************");
        }

    }

    public (float, int) CalcReward() {

        // Calculate rewards based on burnt pixels
        List<FireSimulator.Cell> burnt_pixels = fire_sim.BurntPixels();
        Debug.Log("Total pixels burnt: " + burnt_pixels.Count);

        // Cache pixel colors because we can't acess a Texture2D inside a parallel thread
        List<Color> pixel_colors = new List<Color>();
        for (int j = 0; j < burnt_pixels.Count; j++) {

            FireSimulator.Cell cell = burnt_pixels[j];
            pixel_colors.Add(input_vegetation_map.GetPixel(cell.x, cell.y)); // Get the original pixel color
            
        }

        /*List<float> results = new List<float>();
        for (int i = 0; i < burnt_pixels.Count; i++) {
            Color pixel_color = pixel_colors[i];
            ColorToVegetation mapping = ObtainMapping(pixel_color);

            results.Add(-mapping.burnPriority);
        }*/

        ConcurrentBag<float> results = new ConcurrentBag<float>();

        int batch_size = 200;
        int total_batches = (burnt_pixels.Count + batch_size - 1) / batch_size;

        for (int batch = 0; batch < total_batches; batch++) {

            int start = batch * batch_size;
            int end = Mathf.Min(start + batch_size, burnt_pixels.Count);

            Parallel.For(start, end, i => {

                Color pixel_color = pixel_colors[i];
                ColorToVegetation mapping = ObtainMapping(pixel_color);

                results.Add(-mapping.burnPriority);
                //if (i == 10) Debug.Log("Mapping color: " + mapping.color);
            });

            //Debug.Log("Sum: " + results.Sum());

        }

        // Sum the results after parallel processing

        float rew = results.Sum();
        float max_reward = actual_map.width*actual_map.height*MAX_BURN_PRIO;

        return (rew, burnt_pixels.Count);
    }

    public void CalculateColorMappings() {

        map_material = plane.GetComponent<MeshRenderer>().sharedMaterial;
        map_manager.plane = plane;
        mappings = map_manager.Preprocessing(input_vegetation_map, map_material); // TODO: Paralelitzar per fer-lo més ràpid

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
            col_mapper.colorVegetationMappings = mappings;

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
