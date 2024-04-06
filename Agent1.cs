using System.Collections;
using System.Collections.Generic;

using UnityEngine;
using UnityEditor;

using Unity.MLAgents;
using Unity.MLAgents.Actuators;
using Unity.MLAgents.Sensors;

public class Agent1 : Agent {

    private MapManager map_manager = new MapManager();
    private Renderer plane_renderer;
    private Material plane_material;

    public GameObject plane; // Reference to the terrain object
    public List<ColorToVegetation> mappings;
    public Texture2D input_vegetation_map;

    void Start() {

        plane_renderer = plane.GetComponent<Renderer>();
        plane_material = new Material(plane_renderer.material);

        map_manager.Preprocessing(input_vegetation_map); // TODO: Paralelitzarper fer-lo més ràpid
        map_manager.StoreMappings(mappings);

        UpdatePlane();
    }

    private void UpdatePlane() {

        Texture2D map = map_manager.GetMap();

        //-----I dont know why but if you remove this, the plane does not display the texture...-----//
        Color[] pixels = map.GetPixels();
        map.SetPixels(pixels);
        map.Apply();
        //-------------------------------------------------------------------------------------------//

        Texture new_texture = new Texture2D(map.width, map.height);
        Graphics.CopyTexture(map, new_texture);

        plane_material.mainTexture = new_texture;
        plane_renderer.material = plane_material;
    }

    // Called when the Agent is initialized (at the beginning of each epoch).
    //public override void Initialize() {}

    // Called when the Agent requests a decision
    public override void OnActionReceived(ActionBuffers actions) {

        map_manager.SetPixel(actions.DiscreteActions[0], actions.DiscreteActions[1], new Color(0.8f, 0, 0));
        map_manager.SetPixel(actions.DiscreteActions[1], actions.DiscreteActions[0], new Color(0.8f, 0, 0));
        map_manager.SetPixel(actions.DiscreteActions[1], actions.DiscreteActions[1], new Color(0.8f, 0, 0));
        map_manager.SetPixel(actions.DiscreteActions[0], actions.DiscreteActions[0], new Color(0.8f, 0, 0));

        UpdatePlane();
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
        map_manager.Preprocessing(input_vegetation_map);
        mappings = map_manager.GetMappings();
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