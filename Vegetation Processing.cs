using System.Collections;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

using UnityEngine;
using UnityEditor;

[ExecuteInEditMode]
public class VegetationGenerator : MonoBehaviour {
    
    private List<List<bool>> entityPositions = new List<List<bool>>(); // Matrix of existing entity positions
    private Terrain terrain; // Active terrain
    private ColorVegetationMapper colorToVegMapper = new ColorVegetationMapper();

    public Texture2D vegetationMap; // Assign JPG file
    public float threshold = 0.2877f; // Threshold for considering colors the same
    public int minSeparation = 1; // Density of vegetation (how close the vegetation is generated with eachother)
    public List<ColorToVegetation> colorVegetationMappings = new List<ColorToVegetation>(); // List to map colors to vegetation prefabs

    void Start() {
        
        terrain = Terrain.activeTerrain;
        colorToVegMapper = new ColorVegetationMapper() {colorThreshold = threshold};

        if (terrain == null) {
            Debug.LogError("No active terrain found.");
            return;
        }
        
    }

    void OnEnable() {

    }

    public void CalculateColorMappings() {
        if (vegetationMap != null) {
            colorToVegMapper.ExtractColorMappings(vegetationMap);
            colorVegetationMappings = colorToVegMapper.ObtainMappings();
        }

    }

    public void GenerateVegetation() {
        
        ClearVegetation(); // Clear existing vegetation
        InitEntityPositions();

        int width = vegetationMap.width;
        int height = vegetationMap.height;

        for (int x = 0; x < width; x++) { 
            for (int z = 0; z < height; z++) { 

                Color pixelColor = vegetationMap.GetPixel(x, z);
                ColorToVegetation mapping = colorToVegMapper.GetColorMapping(pixelColor);

                if (mapping.vegetationPrefab != null) { // Check if vegetationPrefab is not null

                    float y = terrain.terrainData.GetHeight(x / 2, z / 2); // Divide by 2 as the size of the vegetation map is 2048x2048 and the size pof the terrain is 1024x1024
                    Vector3 position = new Vector3(x / (float)width * terrain.terrainData.size.x, y, z / (float)height * terrain.terrainData.size.z);

                    //float randomValue = Random.Range(0f, 1f);
                    if (/*randomValue < mapping.spawnChance &&*/ NoNearbyEntity(x, z)) {
                        GameObject vegetation = Instantiate(mapping.vegetationPrefab, position, Quaternion.identity);
                        vegetation.transform.parent = terrain.transform;
                        entityPositions[x][z] = true;
                    }

                }
                
            }

        }

    }

    private void InitEntityPositions() {

        entityPositions.Clear();

        for (int i = 0; i < vegetationMap.width; i++) {

            List<bool> new_col = new List<bool>();
            for (int j = 0; j < vegetationMap.height; j++) new_col.Add(false);
            entityPositions.Add(new_col);

        }

    }

    private bool NoNearbyEntity(int x, int y) {

        bool res = true;

        for (int i = x - minSeparation; i <= x + minSeparation && i < vegetationMap.width; i++) {
            if (i >= 0) {
                for (int j = y - minSeparation; j <= y + minSeparation && j < vegetationMap.height; j++) {
                    if (j >= 0 && entityPositions[i][j]) return false;
                }
            }

        }

        return res;
    }

    public void ClearVegetation() {

        InitEntityPositions();
        Transform terrainTransform = terrain.transform;

        // Destroy all children of the terrain transform
        for (int i = terrainTransform.childCount - 1; i >= 0; i--) {
            DestroyImmediate(terrainTransform.GetChild(i).gameObject);
        }

    }



}

[CustomEditor(typeof(VegetationGenerator))]
public class VegetationGeneratorEditor : Editor {
    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();

        VegetationGenerator myScript = (VegetationGenerator)target;

        if (GUILayout.Button("Generate Vegetation"))
        {
            myScript.GenerateVegetation();
        }

        if (GUILayout.Button("Clear Vegetation"))
        {
            myScript.ClearVegetation();
        }

        if (GUILayout.Button("Recalculate Color Mappings"))
        {
            myScript.CalculateColorMappings();
        }
    }
}

