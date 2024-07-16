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
    private Color FIRETRENCH_COLOR = Color.white;
    private UnityEvent on_sim_end = new UnityEvent();
    private const int MAX_BURN_PRIO = 5;
    private const int MAX_FIRE_SPAN = 4000; // Maximum span of the fire to simulate. Number of opportunities to expand failed.
    private System.Random random = new System.Random();
    private Dictionary<Color, ColorToVegetation> mappings_dict;
    private Dictionary<int, FireMapsPreparation.FireData> fires_data;
    private Vector2 fire_init;

    // Called when the Agent is initialized (only one time)
    public override void Initialize() {

        enviroment_manager = GameObject.Find("EnviromentManager").GetComponent<EnviromentManager>();

        // Prevent modifying the same material as the other agents
        map_material = enviroment_manager.MapMaterial();
        plane.GetComponent<MeshRenderer>().sharedMaterial = map_material;

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

    private void GetFiresData() {

        string[] files = Directory.GetFiles(JSON_Dir + "SampleMaps");
        
        for (int i = 0; i < (int) files.Length / 2; i++) {

            string json_content = File.ReadAllText(JSON_Dir + "SampleMaps/fire_" + i);
            FireMapsPreparation.FireData fire_data = JsonUtility.FromJson<FireMapsPreparation.FireData>(json_content);

            fires_data.Add(i, fire_data);

        }
        

    }

    public override void CollectObservations(VectorSensor sensor) {

        //float distance_to_fire = CalcFireOriginDistance();
        //Debug.Log("Obs: " + distance_to_fire);
        //sensor.AddObservation(distance_to_fire);

    }

    // Called when the Agent requests a decision
    public override void OnActionReceived(ActionBuffers actions) {

        Color color = FIRETRENCH_COLOR;
        Drawer drawer = new Drawer(2);

        List<Vector2> points = new List<Vector2>();
        //int n_points =  actions.DiscreteActions[actions.DiscreteActions.Count() - 1] // The last element is the number of points to use
        for (int i = 0; i < actions.DiscreteActions.Count(); i += 2) {
            points.Add(new Vector2(actions.DiscreteActions[i], actions.DiscreteActions[i+1]));
        }        
        //drawer.DrawLine(points[0], points[1], color, map);
        //drawer.DrawCatmullRomSpline(points, color, map, 0.005f);
        drawer.DrawBezierCurve(points, color, map, 0.005f);


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
        fire_init = fire_simulation.InitFireWithData(fires_data[4], map, map_material);
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
        float fire_reward = 1 - (penalization / max_pen);

        int total_opp = fire_simulation.TotalOpportunities();
        int spent_opp = fire_simulation.SpentOpportunities(); 
        float opportunities_reward = 1 - (spent_opp / total_opp);

        float total_reward = 0.5f*fire_reward + 0.5f*opportunities_reward;
        AddReward(total_reward);
    }
    
    // Funció per calcular la distància entre un punt i una recta
    private float CalcFireOriginDistance() {

        // Calculate ditance between firetrench and fire origin
        Vector2 origin = new Vector2(); // Actual firetrench origin
        Vector2 destination = new Vector2(); // Actual firetrench destination

        // Get the A, B and C coefficients from the line to use the formula ditance between point and line
        GetLineCoefficients(origin, destination, out float A, out float B, out float C); 
        float numerator = Mathf.Abs(A * fire_init.x + B * fire_init.y + C);
        float denominator = Mathf.Sqrt(A * A + B * B);
        
        return numerator / denominator;
    }

    public static void GetLineCoefficients(Vector2 origin, Vector2 destination, out float A, out float B, out float C) {
        // Returns A, B and C coefficients from the line that connects origin and destination

        A = destination.y - origin.y;
        B = origin.x - destination.x;
        C = destination.x * origin.y - origin.x * destination.y;
    }
    
}

[CustomEditor(typeof(Agent1))]
public class Agent1Editor : Editor {
    public override void OnInspectorGUI() {
        DrawDefaultInspector();

        Agent1 myScript = (Agent1)target;

    }

}