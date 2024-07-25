using System.Collections;
using System.Collections.Generic;
using System.IO;

using UnityEngine;
using UnityEditor;

public class FireMitigation : MonoBehaviour {

    private const string JSON_Dir = "./Assets/Resources/JSON/";
    private readonly Color BLACK_COLOR = Color.black; // Unity no permet colors constants, posem readonly que fa la mateixa funció
    private readonly Color WHITE_COLOR = Color.white; // Unity no permet colors constants, posem readonly que fa la mateixa funció
    private readonly Color RED_COLOR = Color.red;
    private FireSimulator _fire_sim;
    private EnviromentManager _enviroment_manager;
    private Dictionary<Color, ColorToVegetation> _mappings_dict;
    private Texture2D _map;
    private Material _map_material;
    private bool _sim_running;
    private bool _load_points = false;

    public GameObject _plane;
    public int _init_x;
    public int _init_y;
    public double _temperature;
    public float _humidity;
    public Vector3 _wind;
    public List<Vector2> _firetrench_points;
    public int MAX_FIRE_SPAN = 1000;
    public int SIMULATION_SPEED = 2;
    public bool _poligonal = false; 

    // Start is called before the first frame update
    void Start() {

        this._sim_running = false;
        this._enviroment_manager = GameObject.Find("EnviromentManager").GetComponent<EnviromentManager>();

        // Prevent modifying the same material as the other agents
        this._map_material = this._enviroment_manager.MapMaterial();
        this._plane.GetComponent<MeshRenderer>().sharedMaterial = this._map_material;

        this._mappings_dict = this._enviroment_manager.ObtainEditorMappingsDict();
        this._map = this._enviroment_manager.VegetationMapTexture();
        this._map_material.mainTexture = this._map;

        ColorToVegetation white_mapping = new ColorToVegetation(); // Firetrench mapping
        white_mapping.color = WHITE_COLOR;
        white_mapping.ICGC_id = -1;
        white_mapping.expandCoefficient = -1.0f;
        white_mapping.burnPriority = 1;

        this._mappings_dict.Add(WHITE_COLOR, white_mapping);
        this._fire_sim = new FireSimulator(this._mappings_dict, this._wind, this._humidity, (float)this._temperature);

        this._fire_sim.InitFireAt(this._init_x, this._init_y, this._map, this._map_material);
        this._fire_sim.SetSimSpeed(SIMULATION_SPEED);

        Drawer drawer = new Drawer(1);
        if (!this._poligonal) drawer.DrawCatmullRomSpline(this._firetrench_points, WHITE_COLOR, this._map, 0.0005f);
        else drawer.DrawJointPointsPoligonal(this._firetrench_points, WHITE_COLOR, this._map);   
    }

    // Update is called once per frame
    void Update() {

        if (!this._sim_running) {
            this._sim_running = true;
            StartCoroutine(FireSimulation());
            this._sim_running = false;
        }
    }

    public IEnumerator FireSimulation() {

        bool fire_ended = false;
        while (!fire_ended) {
            fire_ended = this._fire_sim.ExpandFire(MAX_FIRE_SPAN, 
                                                    _enviroment_manager.HeightMap(), 
                                                    _map, 
                                                    _map_material
                                                    );

            yield return null;
        }

    }

    public void DisplayData() {

        

    }

    public void LoadFireAndFiretrenchData() {
        
        string json_content = File.ReadAllText(JSON_Dir + "FireMitigation_data.json");
        FireMitigationWrapper data = JsonUtility.FromJson<FireMitigationWrapper>(json_content);

        this._init_x = data._init_x;
        this._init_y = data._init_y;
        this._temperature = data._temperature;
        this._humidity = data._humidity;
        this._wind = data._wind;
        this._firetrench_points = data._firetrench_points;

    }

    public void StoreFireAndFiretrenchData() {

        FireMitigationWrapper wrapper = new FireMitigationWrapper(_init_x, _init_y, _temperature, _humidity, _wind, _firetrench_points); 

        string json_content = JsonUtility.ToJson(wrapper);
        File.WriteAllText(JSON_Dir + "FireMitigation_data.json", json_content);
    }

    public void LoadFiretrenchPoints() {

        this._load_points = true;

        string json_content = File.ReadAllText(JSON_Dir + "FiretrenchPoints.json");
        FiretrenchPointsWrapper data = JsonUtility.FromJson<FiretrenchPointsWrapper>(json_content);

        this._firetrench_points = data._firetrench_points;
    }

    public void StoreFiretrenchPoints() {

        FiretrenchPointsWrapper wrapper = new FiretrenchPointsWrapper(this._firetrench_points);

        string json_content = JsonUtility.ToJson(wrapper);
        File.WriteAllText(JSON_Dir + "FiretrenchPoints.json", json_content);
    }
}

[CustomEditor(typeof(FireMitigation))]
public class FireMitigationEditor : Editor {
    public override void OnInspectorGUI() {

        DrawDefaultInspector();
        FireMitigation myScript = (FireMitigation)target;

        if (GUILayout.Button("Load fire and firetrench data")) myScript.LoadFireAndFiretrenchData();
        if (GUILayout.Button("Store fire and firetrench data")) myScript.StoreFireAndFiretrenchData();
        if (GUILayout.Button("Load firetrench points")) myScript.LoadFiretrenchPoints();
        if (GUILayout.Button("Store firetrench points")) myScript.StoreFiretrenchPoints();
        if (GUILayout.Button("Display data")) myScript.DisplayData();
        
    }
}

[System.Serializable]
public class FireMitigationWrapper {
    
    public int _init_x;
    public int _init_y;
    public double _temperature;
    public float _humidity;
    public Vector3 _wind;
    public List<Vector2> _firetrench_points;

    public FireMitigationWrapper(int x, int y, double temp, float hum, Vector3 wind, List<Vector2> points) {
        this._init_x = x;
        this._init_y = y;
        this._temperature = temp;
        this._humidity = hum;
        this._wind = wind;
        this._firetrench_points = points;
    }

}

[System.Serializable]
public class FiretrenchPointsWrapper {
    public List<Vector2> _firetrench_points;

    public FiretrenchPointsWrapper(List<Vector2> points) {
        this._firetrench_points = points;
    }

}
