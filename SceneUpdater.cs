using System.Collections;
using System.Collections.Generic;
using UnityEngine;

using Unity.MLAgents;

public class SceneUpdater : MonoBehaviour {

    public MapManager map_manager = new MapManager();
    public Material mat;
    public Agent[] agents;

    // Start is called before the first frame update
    void Start() {
        agents = FindObjectsOfType<Agent1>();
        foreach (Agent1 agent in agents) {
            agent.map_manager = map_manager;
            //Debug.Log("Agent found");
        }
    }

    // Update is called once per frame
    void Update() {
        map_manager.UpdateMap(mat);
    }
}
