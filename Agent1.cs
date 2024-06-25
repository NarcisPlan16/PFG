using System.Collections;
using System.Collections.Generic;
using System.Collections.Concurrent;
using System.Linq;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using System;

using UnityEngine;
using UnityEditor;
using UnityEngine.Events;

using Unity.MLAgents;
using Unity.MLAgents.Actuators;
using Unity.MLAgents.Sensors;

public class Agent1 : Agent {

    private GameObject plane; // Reference to the terrain object
    private EnviromentManager enviroment_manager;
    private MapManager map_manager = new MapManager();
    private FireSimulator fire_simulation;
    private Texture2D map;
    private Material map_material;
    private const string JSON_Dir = "./Assets/Resources/JSON/";
    private Color FIRETRENCH_COLOR = new Color(1.0f, 1.0f, 1.0f);
    private UnityEvent on_sim_end = new UnityEvent();
    private const int MAX_BURN_PRIO = 5;
    private const int MAX_FIRE_SPAN = 1000;
    private System.Random random = new System.Random();
    private Dictionary<Color, ColorToVegetation> mappings_dict;
    private Dictionary<int, FireMapsPreparation.FireData> fires_data;

    // Called when the Agent is initialized (only one time)
    public override void Initialize() {

        plane = transform.parent.Find("Plane").gameObject; 
        // transform.parent.Find("Plane")?.gameObject; --> The ? prevents it throwing NullReferenceError and instead returns null if the error is thrown
        enviroment_manager = GameObject.Find("EnviromentManager").GetComponent<EnviromentManager>();

        // Prevent modifying the same material as the other agents
        map_material = new Material(enviroment_manager.MapMaterial());
        plane.GetComponent<MeshRenderer>().material = map_material;

        map_manager.plane = plane;
        map_manager.Preprocessing(enviroment_manager.VegetationMapTexture()); // TODO: Paralelitzar per fer-lo més ràpid

        map = new Texture2D(map_manager.GetMap().width, map_manager.GetMap().height, TextureFormat.RGBA32, false);
        map.SetPixels(map_manager.GetMap().GetPixels());
        map_material.mainTexture = map;

        ColorToVegetation white_mapping = new ColorToVegetation();
        white_mapping.color = FIRETRENCH_COLOR;
        white_mapping.ICGC_id = -1;
        white_mapping.expandCoefficient = 0.1f;
        white_mapping.burnPriority = 1;

        mappings_dict = enviroment_manager.Mappings();
        mappings_dict.Add(FIRETRENCH_COLOR, white_mapping);

        fires_data = new Dictionary<int, FireMapsPreparation.FireData>();
        GetFiresData();

        Academy.Instance.AutomaticSteppingEnabled = true;
        SetReward(0);
        
    }

    private void GetFiresData() {

        string[] files = Directory.GetFiles(JSON_Dir + "SampleMaps");
        
        for (int i = 0; i < (int) files.Length / 2; i++) {

            string json_content = File.ReadAllText(JSON_Dir + "SampleMaps/fire_" + i);
            FireMapsPreparation.FireData fire_data = JsonUtility.FromJson<FireMapsPreparation.FireData>(json_content);

            fires_data.Add(i, fire_data);

        }
        

    }

    public override void CollectObservations(VectorSensor sensor) {

        //sensor.AddObservation();

    }

    // Called when the Agent requests a decision
    public override void OnActionReceived(ActionBuffers actions) {

        Vector2 origin = new Vector2();
        origin.x = actions.DiscreteActions[0];
        origin.y = actions.DiscreteActions[1];

        Vector2 destination = new Vector2();
        destination.x = actions.DiscreteActions[2];
        destination.y = actions.DiscreteActions[3];

        Color color = FIRETRENCH_COLOR;
        LineDrawer line_drawer = new LineDrawer();

        line_drawer.DrawLine(origin, destination, color, map, map_material, map_manager);
        Debug.Log(origin.x + ", " + origin.y + " ----> " + destination.x + ", " + destination.y);

        FinishEpoch();
    }

    // Called when the Agent resets. Here is where we reset everything after the reward is given
    public override void OnEpisodeBegin() {

        Debug.Log("-----------------------------EPOCH " + Academy.Instance.EpisodeCount + "-----------------------------");
        //Debug.Log("REWARD: " + + GetCumulativeReward()); // DEBUG

    }

    public void FinishEpoch() {

        Academy.Instance.AutomaticSteppingEnabled = false;
        on_sim_end.AddListener(() => {
            Debug.Log("REWARD: " + GetCumulativeReward());
            //action_taken = false;
            Academy.Instance.AutomaticSteppingEnabled = true;
        });

        StartCoroutine(SimulateFireAndCalcReward());
    }

    public IEnumerator SimulateFireAndCalcReward() {

        fire_simulation = new FireSimulator(mappings_dict, 
                                            fires_data[4].wind_dir, 
                                            fires_data[4].humidity_percentage, 
                                            fires_data[4].temperature
                                            );

        yield return StartCoroutine(SimulateFire());
        yield return StartCoroutine(CalcReward());

        // Reset the map
        map_manager.ResetMap();
        map_material = enviroment_manager.MapMaterial();

        Texture2D tex = enviroment_manager.VegetationMapTexture();
        map = new Texture2D(tex.width, tex.height, TextureFormat.RGBA32, false);
        map.SetPixels(tex.GetPixels());
        map.Apply();

        on_sim_end.Invoke(); //Fire the event as the coroutine has ended and thus we can continue 

    }

    public IEnumerator SimulateFire() {

        bool fire_ended = false;
        fire_simulation.InitFireWithData(fires_data[4], map_manager, map, map_material);
        while (!fire_ended) {
            fire_ended = fire_simulation.ExpandFire(MAX_FIRE_SPAN, 
                                                    enviroment_manager.HeightMap(), 
                                                    map_manager, 
                                                    map, 
                                                    map_material
                                                    );
            yield return null;
        }

    }

    public IEnumerator CalcReward() {

        // Calculate rewards based on burnt pixels
        List<FireSimulator.Cell> burnt_pixels = fire_simulation.BurntPixels();
        Debug.Log("Total pixels burnt: " + burnt_pixels.Count);

        // Cache pixel colors because we can't acess a Texture2D inside a parallel thread
        List<Color> pixel_colors = new List<Color>();
        for (int i = 0; i < burnt_pixels.Count; i++) {

            FireSimulator.Cell cell = burnt_pixels[i];
            Texture2D veg_map_texture = enviroment_manager.VegetationMapTexture();
            pixel_colors.Add(veg_map_texture.GetPixel(cell.x, cell.y)); // Get the original pixel color
            
            yield return null;
        }

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
            });

            yield return null; // Prevent unity scene from freezing
        }

        // Sum the results after parallel processing
        float penalization = results.Sum();
        float max_pen = fires_data[4].total_cost;

        SetReward(penalization - max_pen);
    }

    private ColorToVegetation ObtainMapping(Color color) {

        ColorToVegetation mapping = new ColorToVegetation();
        if (mappings_dict.ContainsKey(color)) {
            mapping = mappings_dict[color];
        }
        else {

            ColorVegetationMapper col_mapper = new ColorVegetationMapper();
            col_mapper.mappings = mappings_dict;

            mapping = col_mapper.FindClosestMapping(color);

        }

        return mapping;
    }
    
}

[CustomEditor(typeof(Agent1))]
public class Agent1Editor : Editor {
    public override void OnInspectorGUI() {
        DrawDefaultInspector();

        Agent1 myScript = (Agent1)target;

    }

}