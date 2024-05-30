using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Threading;

using UnityEngine;
using UnityEditor;

using Unity.MLAgents;
using Unity.MLAgents.Actuators;
using Unity.MLAgents.Sensors;

public class Agent1 : Agent {

    private FireSimulator fire_simulation;
    public MapManager map_manager = new MapManager();
    private Texture2D map;
    public GameObject plane; // Reference to the terrain object
    public Material plane_material;
    public Vector3 wind_direction;
    public List<ColorToVegetation> mappings;
    public string mappings_filename;
    public Texture2D input_vegetation_map;
    public Texture2D height_map;

    private bool episode_start;
    private bool finishing;
    private Material map_material;
    private Material original_map_material;
    private Color[] original_map_pixels;
    private const string JSON_Dir = "";

    // Called when the Agent is initialized (only one time)
    public override void Initialize() {

        this.MaxStep = 1; // Maximum number of iterations for each epoch

        map_material = plane.GetComponent<MeshRenderer>().material;
        original_map_material = map_material;

        map_manager.plane = plane;
        map_manager.Preprocessing(input_vegetation_map, map_material); // TODO: Paralelitzar per fer-lo més ràpid

        map = map_manager.GetMap();
        original_map_pixels = map.GetPixels();

        fire_simulation = new FireSimulator(mappings, wind_direction);
        episode_start = true;
        finishing = false;
        Academy.Instance.AutomaticSteppingEnabled = false;
        
        //EnvironmentParameters a = Academy.Instance.EnvironmentParameters;

    }

    public void Update() {

        if (episode_start) {
            episode_start = false;
            Academy.Instance.EnvironmentStep();
            this.EndEpisode();
        }
        else if (!finishing) FinishEpoch();
        

    }

    // Called when the Agent requests a decision
    public override void OnActionReceived(ActionBuffers actions) {

        Debug.Log("aaaa"/*actions.DiscreteActions[1]*/);

        //map_manager.SetPixel(Random.Range(0, 512), Random.Range(0, 512), new Color(0.8f, 0, 0), map, map_material);
        episode_start = false;
    }

    // Called when the Agent resets. Here is where we reset everything after the reward is given
    public override void OnEpisodeBegin() {
        
    }

    public void FinishEpoch() {
        finishing = true;
        StartCoroutine(ResetEnviroment());
    }

    public IEnumerator ResetEnviroment() {

        bool fire_ended = false;
        fire_simulation.InitRandomFire(map_manager, map, map_material);
        while (!fire_ended) {
            fire_ended = fire_simulation.ExpandFireRandom(height_map, map_manager, map, map_material);
            yield return null;
        }

        // Calculate rewards based on burned pixels
        List<FireSimulator.Cell> burnt_pixels = fire_simulation.BurntPixels();
        Debug.Log("Total pixels burnt: " + burnt_pixels.Count);
        float reward = 0.0f;
        //StartCoroutine(CalcReward(burnt_pixels));
        foreach (FireSimulator.Cell cell in burnt_pixels) {
            Color pixel_color = map_manager.GetPixel(cell.x, cell.y);
            ColorToVegetation mapping = ObtainMapping(pixel_color);

            reward -= mapping.burnPriority;
            yield return null;
        }

        Debug.Log("Reward: " + reward);
        
        map_manager.ResetMap();
        map_material = original_map_material;

        map.SetPixels(original_map_pixels);
        map.Apply();

        episode_start = true;
        finishing = false;
        fire_simulation = new FireSimulator(mappings, wind_direction); // TODO: Comprovar que no ocupa més memòria
    }

    public IEnumerator CalcReward(List<FireSimulator.Cell> burnt_pixels) {

        float reward = 0.0f;
        foreach (FireSimulator.Cell cell in burnt_pixels) {
            Color pixel_color = map_manager.GetPixel(cell.x, cell.y);
            ColorToVegetation mapping = ObtainMapping(pixel_color);

            reward -= mapping.burnPriority;
            yield return null;
        }

        Debug.Log("Rew " + reward);

    }

    public void CalculateColorMappings() {

        map_material = plane.GetComponent<MeshRenderer>().sharedMaterial;
        map_manager.plane = plane;
        mappings = map_manager.Preprocessing(input_vegetation_map, map_material); // TODO: Paralelitzar per fer-lo més ràpid

        map = map_manager.GetMap();

    }

    public void StoreMappings() {
        map_manager.SaveMappings(mappings);
        map_manager.StoreMappings(JSON_Dir + mappings_filename);
    }

    public void LoadMappings() {
        map_manager.LoadMappings(JSON_Dir + mappings_filename);
        mappings = map_manager.GetMappings();
    }

    private ColorToVegetation ObtainMapping(Color color) {

        bool found = false;
        int i = 0;
        ColorToVegetation mapping = new ColorToVegetation();
        while (!found) {
            if (mappings[i].color == color) {
                mapping = mappings[i];
                found = true;
            }
        }

        return mapping;
    }
    
}

[CustomEditor(typeof(Agent1))]
public class Agent1Editor : Editor {
    public override void OnInspectorGUI() {
        DrawDefaultInspector();

        Agent1 myScript = (Agent1)target;
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