using System.Collections;
using System.Collections.Generic;

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
    public Texture2D input_vegetation_map;
    public Texture2D height_map;

    private Material map_material;

    void Start() {

        map_material = plane.GetComponent<MeshRenderer>().material;
        map_manager.plane = plane;
        map_manager.Preprocessing(input_vegetation_map, map_material); // TODO: Paralelitzar per fer-lo més ràpid

        map = map_manager.GetMap();
        map_manager.StoreMappings(mappings);
        map_manager.SaveMappings("Assets/Resources/JSON/VegMappings.json");

        fire_simulation = new FireSimulator(mappings, wind_direction);
        fire_simulation.InitRandomFire(map_manager, map, map_material);

    }

    // Called when the Agent is initialized (at the beginning of each epoch).
    //public override void Initialize() {}

    // Called when the Agent requests a decision
    public override void OnActionReceived(ActionBuffers actions) {

        //Debug.Log(actions.DiscreteActions[0]);

        //map_manager.SetPixel(Random.Range(0, 512), Random.Range(0, 512), new Color(0.8f, 0, 0), map, map_material);
        fire_simulation.ExpandFireRandom(height_map, map_manager, map, map_material);

    }

    // Called when the Agent's observations need to be updated
    //public override void CollectObservations(VectorSensor sensor) {}

    // Called when the Agent resets. Here is where we reset everything after the reward is given
    public override void OnEpisodeBegin() {
        //map_manager.ResetMap();
    }

    // Called when heuristic method is requested (Behaviour=Heuristic). Also known as "Policy"
    //public override void Heuristic(in ActionBuffers actions_out) {}

    public void CalculateColorMappings() {

        map_material = plane.GetComponent<MeshRenderer>().sharedMaterial;
        map_manager.plane = plane;
        mappings = map_manager.Preprocessing(input_vegetation_map, map_material); // TODO: Paralelitzar per fer-lo més ràpid

        map = map_manager.GetMap();
        map_manager.StoreMappings(mappings);

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
    }
}