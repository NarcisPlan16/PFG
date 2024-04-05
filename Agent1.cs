using System.Collections;
using System.Collections.Generic;

using UnityEngine;
using UnityEditor;

using Unity.MLAgents;
using Unity.MLAgents.Actuators;
using Unity.MLAgents.Sensors;

public class Agent1 : Agent {

    private MapManager map_manager = new MapManager();

    public GameObject plane; // Reference to the terrain object
    public List<ColorToVegetation> mappings;
    public Texture2D input_vegetation_map;

    void Start() {

        //map_manager.Preprocessing(input_vegetation_map);
        //map_manager.StoreMappings(mappings);

        //Renderer plane_renderer = plane.GetComponent<Renderer>();
        //Material new_material = new Material(plane_renderer.material);

        //Texture2D map = map_manager.GetMap();

        //-----I dont know why but if you remove this, the plane does not display the texture...-----//
        //Color[] pixels = map.GetPixels();
        //map.SetPixels(pixels);
        //map.Apply();
        //-------------------------------------------------------------------------------------------//

        //Texture new_texture = new Texture2D(map.width, map.height);
        //Graphics.CopyTexture(map, new_texture);

        //new_material.mainTexture = new_texture;
        //plane_renderer.material = new_material;

    }

    // Called when the Agent is initialized (at the beginning of each epoch).
    public override void Initialize() {
        
    }

    // Called when the Agent requests a decision
    public override void OnActionReceived(ActionBuffers actions) {

        Debug.Log(actions.DiscreteActions[0]);

    }

    // Called when the Agent's observations need to be updated
    public override void CollectObservations(VectorSensor sensor) {
        
    }

    // Called when the Agent resets. Here is where we reset after the reward is given
    public override void OnEpisodeBegin() {
        
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