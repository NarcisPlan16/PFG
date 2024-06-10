using System.Collections;
using System.Collections.Generic;
using System.Collections.Concurrent;
using System.Linq;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

using UnityEngine;
using UnityEditor;
using UnityEngine.Events;


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
    private Color FIRETRENCH_COLOR = new Color(1.0f, 0.588f, 0.196f);
    private UnityEvent on_sim_end = new UnityEvent();

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
        SetReward(100000f);
        
        //EnvironmentParameters a = Academy.Instance.EnvironmentParameters;

    }

    public void Update() {

        

        if (episode_start) {
            Debug.Log("-----------------------------EPOCH " + Academy.Instance.EpisodeCount + "-----------------------------");
            Academy.Instance.EnvironmentStep();
            episode_start = false;
            SetReward(100000f);
            this.RequestDecision();
            Debug.Log("R1: " + + GetCumulativeReward()); // DEBUG
        }
        else if (!finishing) FinishEpoch();
        

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

        //map_manager.SetPixel(Random.Range(0, 512), Random.Range(0, 512), new Color(0.8f, 0, 0), map, map_material);
        episode_start = false;
    }

    // Called when the Agent resets. Here is where we reset everything after the reward is given
    public override void OnEpisodeBegin() {
        
    }

    public void FinishEpoch() {

        finishing = true;
        
    }

    public IEnumerator SimulateFireAndCalcReward() {

        yield return StartCoroutine(SimulateFire());
        yield return StartCoroutine(CalcReward());

        // Reset the map
        map_manager.ResetMap();
        map_material = original_map_material;

        map.SetPixels(original_map_pixels);
        map.Apply();

        episode_start = true;
        finishing = false;
        fire_simulation = new FireSimulator(mappings, wind_direction); // TODO: Comprovar que no ocupa més memòria
        on_sim_end.Invoke(); //Fire the event as the coroutine has ended and thus we can continue 

    }

    public IEnumerator SimulateFire() {

        bool fire_ended = false;
        fire_simulation.InitRandomFire(map_manager, map, map_material);
        while (!fire_ended) {
            fire_ended = fire_simulation.ExpandFireRandom(height_map, map_manager, map, map_material);
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
            pixel_colors.Add(input_vegetation_map.GetPixel(cell.x, cell.y)); // Get the original pixel color
            
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
        float reward = results.Sum();
        //SetReward(100000f + reward);
        AddReward(reward);
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
        //Debug.Log("Mappings count: " + mappings.Count);
        while (!found && i < mappings.Count) {

            //Debug.Log("Color to search: " + color);
            //Debug.Log("Mapping color actual: "+ mappings[i].color);


            if (mappings[i].color == color) {
                mapping = mappings[i];
                found = true;
            }
            i++;
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