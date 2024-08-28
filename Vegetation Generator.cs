using System.Collections;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using System.Linq;

using UnityEngine;
using UnityEditor;

[ExecuteInEditMode]
public class VegetationGenerator : MonoBehaviour {
    
    private List<List<bool>> entity_positions = new List<List<bool>>(); // Matrix of existing entity positions
    private Terrain terrain; // Active terrain
    private ColorVegetationMapper colorToVegMapper = new ColorVegetationMapper();
    private const string JSON_Dir = "./Assets/Resources/JSON/";
    private Dictionary<Color, ColorToVegetation> mappings_dict = new Dictionary<Color, ColorToVegetation>(); // List to map colors to vegetation prefabs

    public string mappings_filename;
    public Texture2D vegetation_map; // Assign JPG file
    public float threshold = 0.2877f; // Threshold for considering colors the same
    public int min_separation = 1; // Density of vegetation (how close the vegetation is generated with eachother)
    public float prefab_size = 1.0f;
    public List<ColorToVegetation> mappings;

    void Start() {
        
        terrain = Terrain.activeTerrain;
        colorToVegMapper = new ColorVegetationMapper() {colorThreshold = threshold};

        if (terrain == null) {
            Debug.LogError("No active terrain found.");
            return;
        }
        
        MapPreprocessing map_preprocessing = new MapPreprocessing();
        map_preprocessing.Start(vegetation_map);
        map_preprocessing.CalculateColorMappings();

        vegetation_map = map_preprocessing.ObtainProcessedMap();
        GenerateVegetation();

    }

    public void GenerateVegetation() {

        colorToVegMapper.mappings = this.mappings_dict;
        
        ClearVegetation(); // Clear existing vegetation
        Initentity_positions();

        int veg_width = vegetation_map.width;
        int veg_height = vegetation_map.height;
        float terrain_width = terrain.terrainData.size.x;
        float terrain_height =  terrain.terrainData.size.z;

        for (int x = 0; x < veg_width; x++) { 
            for (int z = 0; z < veg_height; z++) { 

                Color pixelColor = vegetation_map.GetPixel(x, z);
                ColorToVegetation mapping = colorToVegMapper.FindClosestMapping(pixelColor);

                // if vegetationPrefab is not null
                if (mapping.vegetationPrefab != null) InstantiatePrefab(x, z, mapping, veg_width, terrain_width, veg_height, terrain_height);
                
            }

        }

    }

    private void InstantiatePrefab(int x, int z, ColorToVegetation mapping, int veg_width, float terrain_width, int veg_height, float terrain_height) {

        float y = terrain.terrainData.GetHeight(x * 2, z * 2); // Multiply by 2, vegetation map is 512 and the size of the terrain is 1024x1024
        Vector3 position = new Vector3(x / (float)veg_width * terrain_width, y, z / (float)veg_height * terrain_height);

        if (NoNearbyEntity(x, z)) {
            GameObject vegetation = Instantiate(mapping.vegetationPrefab, position, Quaternion.identity);
            vegetation.transform.parent = terrain.transform;
            vegetation.transform.localScale = new Vector3(prefab_size, prefab_size, prefab_size);
            entity_positions[x][z] = true;
        }

    }

    private void Initentity_positions() {

        entity_positions.Clear();

        for (int i = 0; i < vegetation_map.width; i++) {

            List<bool> new_col = new List<bool>();
            for (int j = 0; j < vegetation_map.height; j++) new_col.Add(false);
            entity_positions.Add(new_col);

        }

    }

    private bool NoNearbyEntity(int x, int y) {

        bool res = true;

        for (int i = x - min_separation; i <= x + min_separation && i < vegetation_map.width; i++) {
            if (i >= 0) {
                for (int j = y - min_separation; j <= y + min_separation && j < vegetation_map.height; j++) {
                    if (j >= 0 && entity_positions[i][j]) return false;
                }
            }

        }

        return res;
    }

    public void ClearVegetation() {

        Initentity_positions();
        Transform terrainTransform = terrain.transform;

        // Destroy all children of the terrain transform
        for (int i = terrainTransform.childCount - 1; i >= 0; i--) {
            DestroyImmediate(terrainTransform.GetChild(i).gameObject);
        }

    }

    public void LoadMappings() {

        MapManager map_manager = new MapManager();

        map_manager.LoadMappings(JSON_Dir + mappings_filename);
        mappings_dict = map_manager.GetMappings();

        mappings = mappings_dict.Values.ToList();
    }

}

[CustomEditor(typeof(VegetationGenerator))]
public class VegetationGeneratorEditor : Editor {
    public override void OnInspectorGUI() {
        DrawDefaultInspector();

        VegetationGenerator myScript = (VegetationGenerator)target;

        if (GUILayout.Button("Generate Vegetation")) myScript.GenerateVegetation();
        if (GUILayout.Button("Clear Vegetation")) myScript.ClearVegetation();
        if (GUILayout.Button("Load mappings")) myScript.LoadMappings();
        
        
    }
}

