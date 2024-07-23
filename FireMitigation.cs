using System.Collections;
using System.Collections.Generic;
using System.IO;

using UnityEngine;
using UnityEditor;

public class FireMitigation : MonoBehaviour {

    private FireMitigationData _fire_data;

    public struct FireMitigationData {

        public FireMitigationData(int x, int y, Vector3 wind_dir, float humidity_percentage, float temperature_percentage, List<Vector2> points) {
            this.init_x = x;
            this.init_y = y;
            this.wind = wind_dir;
            this.humidity = humidity_percentage;
            this.temperature = temperature_percentage;
            this.firetrench_points = points;
        }

        public int init_x;
        public int init_y;
        public float temperature;
        public float himudity;
        public Vector3 wind;
        public List<Vector2> firetrench_points;

    }

    // Start is called before the first frame update
    void Start() {
        
    }

    // Update is called once per frame
    void Update() {
        
    }

    public void LoadFireAndFiretrenchData() {
        
        string json_content = File.ReadAllText(file_path);
        FireMitigationWrapper data = JsonUtility.FromJson<FireMitigationWrapper>(json_content);

        this._fire_data = data._data;
    }
}

[CustomEditor(typeof(FireMitigation))]
public class FireMitigationEditor : Editor {
    public override void OnInspectorGUI() {

        DrawDefaultInspector();
        FireMitigation myScript = (FireMitigation)target;

        if (GUILayout.Button("Load fire and firetrench data")) myScript.LoadFireAndFiretrenchData();
        
    }
}

[System.Serializable]
public class FireMitigationWrapper {
    public FireMitigation.FireMitigationData _data;

    public FireMitigationWrapper(FireMitigation.FireMitigationData fire_data) {
        this._data = fire_data;
    }

}
