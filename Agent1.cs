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
    private Color WHITE_COLOR = Color.white;
    private UnityEvent on_sim_end = new UnityEvent();
    private const int MAX_BURN_PRIO = 5;
    private const int MAX_FIRE_SPAN = 1000; // Maximum span of the fire to simulate. Number of opportunities to expand failed.
    private System.Random random = new System.Random();
    private Dictionary<Color, ColorToVegetation> mappings_dict;
    private Dictionary<int, FireMapsPreparation.FireData> fires_data;
    private Vector2 fire_init;
    private float opportunities_reward = 0;
    private float fire_reward = 0;
    private int n_fire_test_files = 0;

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

        sensor.AddObservation(opportunities_reward);
        sensor.AddObservation(fire_reward);

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
        if (actions.DiscreteActions[n_parameters-1] == 0) x_first = false;

        //drawer.DrawLine(points[0], points[1], WHITE_COLOR, map);
        //drawer.DrawCatmullRomSpline(points, WHITE_COLOR, map, 0.005f);
        drawer.DrawBezierCurve(points, WHITE_COLOR, map, 0.005f, x_first);

        FinishEpoch(points, x_first);
    }

    // Called when the Agent resets. Here is where we reset everything after the reward is given
    public override void OnEpisodeBegin() {

        //Debug.Log("-----------------------------EPOCH " + Academy.Instance.EpisodeCount + "-----------------------------");
        //Debug.Log("REWARD: " + + GetCumulativeReward()); // DEBUG

    }

    public void FinishEpoch(List<Vector2> points, bool x_first) {

        // TOOD: A l'enviroment manager, que faci que s'esperi a que tots els agents acabin per llavors fer el next step. I eliminar-lo d'aqui
        StartCoroutine(SimulateFireAndCalcReward(points, x_first));
    }

    public IEnumerator SimulateFireAndCalcReward(List<Vector2> points, bool x_first) {

        HashSet<int> tested_fires = new HashSet<int>();
        for (int i = 0; i < 10; i++) { // Test with 10 fire example files

            int n_file = random.Next(0, this.n_fire_test_files);
            while(tested_fires.Contains(n_file)) n_file = random.Next(0, this.n_fire_test_files);

            tested_fires.Add(n_file);
            fire_simulation = new FireSimulator(mappings_dict, 
                                                fires_data[n_file].wind_dir, 
                                                fires_data[n_file].humidity_percentage, 
                                                fires_data[n_file].temperature
                                                );

            yield return StartCoroutine(SimulateFire(n_file));

            // Reset the map
            map = enviroment_manager.VegetationMapTexture();
            map_material.mainTexture = map;

            Drawer drawer = new Drawer(1);
            drawer.DrawBezierCurve(points, WHITE_COLOR, map, 0.005f, x_first);
        }

        // Reset the map
        map = enviroment_manager.VegetationMapTexture();
        map_material.mainTexture = map;

        //Debug.Log("REWARD: " + GetCumulativeReward());
        enviroment_manager.AddAgentReady();

    }

    private IEnumerator SimulateFire(int n_file) {

        bool fire_ended = false;
        FireMapsPreparation.FireData fire_data = fires_data[n_file];
        fire_init = fire_simulation.InitFireAt(fire_data.init_x, fire_data.init_y, map, map_material);
        while (!fire_ended) {
            fire_ended = fire_simulation.ExpandFire(MAX_FIRE_SPAN, 
                                                    enviroment_manager.HeightMap(), 
                                                    map, 
                                                    map_material
                                                    );
            yield return null;
        }


        float penalization = fire_simulation.GetReward();
        float max_pen = fires_data[n_file].total_cost;
        fire_reward = 1 - (penalization / max_pen);

        int firetrench_spent_opps = fire_simulation.FiretrenchSpentOpportunities();
        int spent_opportunities = fire_simulation.SpentOpportunities();
        opportunities_reward = firetrench_spent_opps / spent_opportunities;

        float total_reward = 0.4f*fire_reward + 0.6f*opportunities_reward;
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

        Debug.Log("numer: " + numerator);
        Debug.Log("denom: " + denominator);
        
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