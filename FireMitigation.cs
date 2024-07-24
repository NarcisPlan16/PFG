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

    public Texture2D map;
    public Material map_material;
    public int _init_x;
    public int _init_y;
    public double _temperature;
    public float _humidity;
    public Vector3 _wind;
    public List<Vector2> _firetrench_points;

    // Start is called before the first frame update
    void Start() {
        map_material.mainTexture = map;
    }

    // Update is called once per frame
    void Update() {
        
    }

    public void DisplayData() {

        map.SetPixel(_init_x, _init_y, RED_COLOR);

        foreach (Vector2 point in _firetrench_points) {
            map.SetPixel((int)point.x, (int)point.y, WHITE_COLOR);
        }

        map.Apply();

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
}

[CustomEditor(typeof(FireMitigation))]
public class FireMitigationEditor : Editor {
    public override void OnInspectorGUI() {

        DrawDefaultInspector();
        FireMitigation myScript = (FireMitigation)target;

        if (GUILayout.Button("Load fire and firetrench data")) myScript.LoadFireAndFiretrenchData();
        if (GUILayout.Button("Store fire and firetrench data")) myScript.StoreFireAndFiretrenchData();
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
