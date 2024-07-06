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

    public GameObject plane; // Reference to the terrain object
    private EnviromentManager enviroment_manager;
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

        enviroment_manager = GameObject.Find("EnviromentManager").GetComponent<EnviromentManager>();

        // Prevent modifying the same material as the other agents
        map_material = enviroment_manager.MapMaterial();
        plane.GetComponent<MeshRenderer>().sharedMaterial = map_material;

        WaitEnviromentInit(); // Wait for enviroment manager to finish preprocessing

        mappings_dict = enviroment_manager.ObtainEditorMappingsDict();
        map = enviroment_manager.VegetationMapTexture();
        map_material.mainTexture = map;

        ColorToVegetation white_mapping = new ColorToVegetation();
        white_mapping.color = FIRETRENCH_COLOR;
        white_mapping.ICGC_id = -1;
        white_mapping.expandCoefficient = 0.1f;
        white_mapping.burnPriority = 1;

        mappings_dict.Add(FIRETRENCH_COLOR, white_mapping);

        fires_data = new Dictionary<int, FireMapsPreparation.FireData>();
        GetFiresData();

        SetReward(0);
        enviroment_manager.AddAgent();
        enviroment_manager.AddAgentReady();
        Debug.Log("Init done");
        
    }

    private void WaitEnviromentInit() {
        while(!enviroment_manager.PreprocessingDone());
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

        line_drawer.DrawLine(origin, destination, color, map);
        //Debug.Log(origin.x + ", " + origin.y + " ----> " + destination.x + ", " + destination.y);

        FinishEpoch();
    }

    // Called when the Agent resets. Here is where we reset everything after the reward is given
    public override void OnEpisodeBegin() {

        Debug.Log("-----------------------------EPOCH " + Academy.Instance.EpisodeCount + "-----------------------------");
        //Debug.Log("REWARD: " + + GetCumulativeReward()); // DEBUG

    }

    public void FinishEpoch() {

        // TOOD: A l'enviroment manager, que faci que s'esperi a que tots els agents acabin per llavors fer el next step. I eliminar-lo d'aqui
        StartCoroutine(SimulateFireAndCalcReward());
    }

    public IEnumerator SimulateFireAndCalcReward() {

        fire_simulation = new FireSimulator(mappings_dict, 
                                            fires_data[4].wind_dir, 
                                            fires_data[4].humidity_percentage, 
                                            fires_data[4].temperature
                                            );

        yield return StartCoroutine(SimulateFire());

        // Reset the map
        map = enviroment_manager.VegetationMapTexture();
        map_material.mainTexture = map;

        Debug.Log("REWARD: " + GetCumulativeReward());
        enviroment_manager.AddAgentReady();

    }

    private IEnumerator SimulateFire() {

        bool fire_ended = false;
        fire_simulation.InitFireWithData(fires_data[4], map, map_material);
        while (!fire_ended) {
            fire_ended = fire_simulation.ExpandFire(MAX_FIRE_SPAN, 
                                                    enviroment_manager.HeightMap(), 
                                                    map, 
                                                    map_material
                                                    );
            yield return null;
        }


        float penalization = fire_simulation.GetReward();
        float max_pen = fires_data[4].total_cost;
        float total_reward = 1 - (penalization / max_pen);

        AddReward(total_reward);
    }
    
}

[CustomEditor(typeof(Agent1))]
public class Agent1Editor : Editor {
    public override void OnInspectorGUI() {
        DrawDefaultInspector();

        Agent1 myScript = (Agent1)target;

    }

}