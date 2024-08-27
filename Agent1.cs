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
    private EnvironmentManager enviroment_manager;
    private FireSimulator fire_simulation;
    private Texture2D map;
    private Material map_material;
    private const string JSON_Dir = "./Assets/Resources/JSON/";
    private Color WHITE_COLOR = Color.white;
    private UnityEvent on_sim_end = new UnityEvent();
    private const int MAX_BURN_PRIO = 5;
    private System.Random random = new System.Random();
    private Dictionary<Color, ColorToVegetation> mappings_dict;
    private Dictionary<int, FireMapsPreparation.FireData> fires_data;
    private Vector2 fire_init;
    private float action_opportunities_reward = 0;
    private float action_fire_reward = 0;
    private int n_fire_test_files = 0;
    private int n_file = 0;

    // Called when the Agent is initialized (only one time)
    public override void Initialize() {

        enviroment_manager = GameObject.Find("EnviromentManager").GetComponent<EnvironmentManager>();
        this.n_file = enviroment_manager.n_file;

        // Prevent modifying the same material as the other agents
        map_material = enviroment_manager.MapMaterial();
        plane.GetComponent<MeshRenderer>().sharedMaterial = map_material;

        mappings_dict = enviroment_manager.ObtainEditorMappingsDict();
        map = enviroment_manager.VegetationMapTexture();
        map_material.mainTexture = map;

        ColorToVegetation white_mapping = new ColorToVegetation();
        white_mapping.color = WHITE_COLOR;
        white_mapping.ICGC_id = -1;
        white_mapping.expandCoefficient = -1.0f;
        white_mapping.burnPriority = 1;

        mappings_dict.Add(WHITE_COLOR, white_mapping);

        fires_data = new Dictionary<int, FireMapsPreparation.FireData>();
        GetFiresData();

        SetReward(0);
        enviroment_manager.AddAgent();
        enviroment_manager.AddAgentReady();
        Debug.Log("Init done");
        
    }

    private void GetFiresData() {

        string[] files = Directory.GetFiles(JSON_Dir + "SampleMaps");
        this.n_fire_test_files = (int) Mathf.Floor(files.Length / 2);
        
        for (int i = 0; i < this.n_fire_test_files; i++) {

            string json_content = File.ReadAllText(JSON_Dir + "SampleMaps/fire_"+i+".json");
            FireMapsPreparation.FireData fire_data = JsonUtility.FromJson<FireMapsPreparation.FireData>(json_content);

            fires_data.Add(i, fire_data);

        }
        

    }

    public override void CollectObservations(VectorSensor sensor) {

        //float distance_to_fire = CalcFireOriginDistance();
        //Debug.Log("Obs: " + distance_to_fire);
        //sensor.AddObservation(distance_to_fire);

        sensor.AddObservation(action_opportunities_reward);
        sensor.AddObservation(action_fire_reward);

    }

    // Called when the Agent requests a decision
    public override void OnActionReceived(ActionBuffers actions) {

        Drawer drawer = new Drawer(1);

        List<Vector2> points = new List<Vector2>();
        //int n_points =  actions.DiscreteActions[actions.DiscreteActions.Count() - 1] // The last element is the number of points to use
        int n_parameters = actions.DiscreteActions.Count();
        for (int i = 0; i < n_parameters - 1; i += 2) { // n_parameters - 1 because the last paraeter is 0 or 1 (compare by x or by y the points)
            points.Add(new Vector2(actions.DiscreteActions[i], actions.DiscreteActions[i+1]));
        }       

        bool x_first = true;
        // Uncomment when usign bezier or Catmullrom
        if (actions.DiscreteActions[n_parameters-1] == 0) x_first = false;

        //drawer.DrawLine(points[0], points[1], WHITE_COLOR, map); // 4 discrete actions, 2 for each point
        //drawer.DrawJointPointsPoligonal(points, WHITE_COLOR, map);
        drawer.DrawCatmullRomSpline(points, WHITE_COLOR, map, 0.004f);
        //drawer.DrawBezierCurve(points, WHITE_COLOR, map, 0.005f, x_first); // 9 discrete actions, 8 for points, 1 for x_first 

        this.action_opportunities_reward = 0;
        this.action_fire_reward = 0;
        FinishEpoch();
    }

    // Called when the Agent resets. Here is where we reset everything after the reward is given
    public override void OnEpisodeBegin() {

        //Debug.Log("-----------------------------EPOCH " + Academy.Instance.EpisodeCount + "-----------------------------");
        //Debug.Log("REWARD: " + + GetCumulativeReward()); // DEBUG

    }

    public void FinishEpoch() {
        StartCoroutine(SimulateFireAndCalcReward());
    }

    public IEnumerator SimulateFireAndCalcReward() {

        fire_simulation = new FireSimulator(mappings_dict, 
                                            fires_data[n_file].wind_dir, 
                                            fires_data[n_file].humidity_percentage, 
                                            fires_data[n_file].temperature
                                            );
        fire_simulation.SetSimSpeed(10);

        yield return StartCoroutine(SimulateFire());

        // Reset the map
        map = enviroment_manager.VegetationMapTexture();
        map_material.mainTexture = map;

        enviroment_manager.AddAgentReady();
    }

    private IEnumerator SimulateFire() {
        // If we want to test 1 firetrench vs some fires, add a for loop here and loop through some random files and reseting the texture at every step

        bool fire_ended = false;
        FireMapsPreparation.FireData fire_data = fires_data[n_file];
        fire_init = fire_simulation.InitFireAt(fire_data.init_x, fire_data.init_y, map, map_material);
        while (!fire_ended) {
            fire_ended = fire_simulation.ExpandFire(fires_data[n_file].max_span, 
                                                    enviroment_manager.HeightMap(), 
                                                    map, 
                                                    map_material
                                                    );
            yield return null; // Prevent Unity freezing the frames
        }

        float penalization = fire_simulation.GetReward();
        float max_pen = fires_data[n_file].total_cost;
        float fire_reward = 1 - (penalization / max_pen); // This way the rewaard can be positive as it decreases to 0 when "penalization" approaches to "max_pen"
        this.action_fire_reward += fire_reward;

        int firetrench_spent_opps = fire_simulation.FiretrenchSpentOpportunities();
        int spent_opportunities = fire_simulation.SpentOpportunities();
        float opportunities_reward = firetrench_spent_opps + (1 - (spent_opportunities / fires_data[n_file].max_span)); 
        // Opportunities reward is always positive as it grows when more opportunities are spent
        this.action_opportunities_reward += opportunities_reward;
        
        float total_reward = fire_reward + 0.1f*opportunities_reward;
        AddReward(total_reward);
    }
    
}