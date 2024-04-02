using System.Collections;
using System.Collections.Generic;

using UnityEngine;
using UnityEditor;

using Unity.MLAgents;
using Unity.MLAgents.Actuators;
using Unity.MLAgents.Sensors;

public class Agent1 : Agent {

    private MapManager map_manager = new MapManager();

    public List<ColorToVegetation> mappings;
    public Texture2D input_vegetation_map;

    void Start() {

        map_manager.Preprocessing(input_vegetation_map);
        map_manager.StoreMappings(mappings);

    }

    // Called when the Agent is initialized (at the beginning of each epoch).
    public override void Initialize() {
        
    }

    // Called when the Agent requests a decision
    public override void OnActionReceived(ActionBuffers actions) {

        //Debug.Log(actions.DiscreteActions[0]);

    }

    // Called when the Agent's observations need to be updated
    public override void CollectObservations(VectorSensor sensor) {
        
    }

    // Called when the Agent resets
    public override void OnEpisodeBegin() {
        
    }

    // Called when heuristic method is requested. Also known as "Policy"
    public override void Heuristic(in ActionBuffers actions_out) {

        var discrete_actions_out = actions_out.DiscreteActions; // Create a placeholder for continuous actions

        discrete_actions_out[0] = Random.Range(0, 4);
    }

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