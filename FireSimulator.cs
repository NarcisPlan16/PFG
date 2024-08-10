using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class FireSimulator {

    public struct Cell {

        public Cell(int X, int Y, int opp){
            x = X;
            y = Y;
            opportunities = opp;
        }

        public int x;
        public int y;
        public int opportunities;
    }

    private Dictionary<Color, ColorToVegetation> color_mappings;
    private List<Cell> pixels_burning;
    private int pixels_burnt;
    private Vector3 wind_direction;
    private float humidity_coeff;
    private float temperature_coeff;
    private bool debug;
    private System.Random random = new System.Random();
    private readonly Color BLACK_COLOR = Color.black; // Unity no permet colors constants, posem readonly que fa la mateixa funció
    private readonly Color RED_COLOR = Color.red;
    private float reward; // Accumulated reward
    private int total_opportunities; // Total cell opportunities that could have been spent on this fire
    private int spent_opportunities; // Actual spent opportunities on this fire
    private int firetrench_spent_opps; // Spent opportunities trying to burn and expand firetrench pixels
    private int _sim_speed;

    public FireSimulator(Dictionary<Color, ColorToVegetation> mappings, Vector3 wind = new Vector3(), float humidity = 0.0f, float temperature = 0.0f, bool debug = false) {

        this.color_mappings = mappings;
        this.pixels_burning = new List<Cell>();
        this.pixels_burnt = 0;
        this.wind_direction = wind;
        this.humidity_coeff = humidity;
        this.temperature_coeff = temperature;
        this.debug = debug;

        this.reward = 0;
        this.total_opportunities = 0;
        this.spent_opportunities = 0;
        this.firetrench_spent_opps = 0;
        this._sim_speed = 10;
    }

    public Vector2 InitRandomFire(Texture2D map, Material map_material) {
        // Returns where the fire was originated

        int random_x = Random.Range(0, map.width);
        int random_y =  Random.Range(0, map.height);

        int opportunities = CalcOpportunities(random_x, random_y, map); 
        this.total_opportunities += opportunities;
        this.reward += CalcReward(random_x, random_y, map);

        this.pixels_burning.Add(new Cell(random_x, random_y, opportunities)); // Add to pixels burning
        map.SetPixel(random_x, random_y, RED_COLOR); // Set the piel to "Red" as it is the "fire burning" color
        map.Apply();

        if (this.debug) Debug.Log("Init -- X: " + random_x + " Y: " + random_y);

        return new Vector2(random_x, random_y);
    }

    public Vector2 InitFireAt(int x, int y, Texture2D map, Material map_material) {
        // Returns where the fire was originated

        int opportunities = CalcOpportunities(x, y, map);  
        this.total_opportunities += opportunities;
        this.reward += CalcReward(x, y, map);

        this.pixels_burning.Add(new Cell(x, y, opportunities)); // Add to pixels burning
        map.SetPixel(x, y, RED_COLOR); // Set the piel to "Red" as it is the "fire burning" color
        map.Apply();

        if (this.debug) Debug.Log("Init -- X: " + x + " Y: " + y);

        return new Vector2(x, y);
    }

    public void SetSimSpeed(int speed) {
        this._sim_speed = speed;
    }

    public List<Cell> ObtainNeighborsUnburned(Cell cell, Texture2D map) {

        List<Cell> res = new List<Cell>();
        int x = cell.x;
        int y = cell.y;

        // The next code gets all the neighbor pixels (8) including diagonals
        for (int i = x - 1; i <= x + 1; i++) {
            for (int j = y - 1; j <= y + 1; j++) {
                if (x >= 0 && y >= 0 && x < map.width && y < map.height && NotBurnt(i, j, map)) {

                    int opportunities = CalcOpportunities(i, j, map);
                    if (opportunities > 0) res.Add(new Cell(i, j, opportunities));
                }
            }
        }

        // Expand to the 4 adjacent cells 
        //if (x - 1 >= 0 && NotBurnt(x - 1, y, map)) res.Add(new Cell(x-1, y, CalcOpportunities(x-1, y, map))); // North neighbor
        //if (y - 1 >= 0 && NotBurnt(x, y - 1, map)) res.Add(new Cell(x, y-1, CalcOpportunities(x, y-1, map))); // West neighbor
        //if (y + 1 < map.width && NotBurnt(x, y + 1, map)) res.Add(new Cell(x, y+1, CalcOpportunities(x, y+1, map))); // East neighbor
        //if (x + 1 < map.height && NotBurnt(x + 1, y, map)) res.Add(new Cell(x+1, y, CalcOpportunities(x+1, y, map))); // South neighbor

        return res;
    }

    public bool NotBurnt(int x, int y, Texture2D map) {

        Color pixel_color =  map.GetPixel(x, y);

        return pixel_color != BLACK_COLOR && pixel_color != RED_COLOR;
    }

    public bool ExpandFire(int max_span, Texture2D heightmap, Texture2D map, Material map_material) { 

        bool fire_ended = false;
        if (this.pixels_burning.Count > 0) {

            int rand_expand_pixels = random.Next(0, this._sim_speed); // number of pixels tu expand this iteration. Maximum of 10
            for (int i = 0; i < rand_expand_pixels; i++) {

                // expand the fire to its neighbors (to all or to only some of them)
                int rand_pixel = random.Next(0, this.pixels_burning.Count);
                Cell origin_cell = this.pixels_burning[rand_pixel];
                this.total_opportunities += origin_cell.opportunities;
                List<Cell> neighbors = ObtainNeighborsUnburned(origin_cell, map);

                if (neighbors.Count > 0) ExpandPixel(origin_cell, neighbors, rand_pixel, heightmap, map);
                else AddPixelToBurntOnes(rand_pixel, map);
                
                if (this.pixels_burning.Count == 0) {
                    Debug.Log("The fire has ended");
                    fire_ended = true;
                    break; // Break the for loop
                }
                else if (this.spent_opportunities >= max_span) { // LIMIT SIMULATION SPAN
                    fire_ended = true;
                    break; // Break the for loop
                }
                

            }

            map.Apply();
            map_material.mainTexture = map;

        }
        else {
            Debug.Log("The fire has ended");
            fire_ended = true;
        }

        return fire_ended;
    }

    public bool CheckFiretrench(Cell neigh, float expand_prob, Texture2D map) {
        bool burnt = true;

        if (IsFiretrench(neigh, map)) {

            float dice2 = random.Next(0, 100) / 100.0f;
            float dice3 = random.Next(0, 100) / 100.0f;
            if (dice2 > expand_prob) {
                if (dice3 > expand_prob) burnt = false;
                else this.firetrench_spent_opps += 1;
            }
            else this.firetrench_spent_opps += 1;
        }

        return burnt;
    }

    public bool IsFiretrench(Cell cell, Texture2D map) {
        return map.GetPixel(cell.x, cell.y) == Color.white;
    }

    public float CalcReward(int x, int y, Texture2D map) {

        Color pixel_color = map.GetPixel(x, y);
        ColorToVegetation mapping = ObtainMapping(pixel_color);

        return mapping.burnPriority;
    }

    public float GetReward() {
        return reward;
    }

    public int TotalOpportunities() {
        return total_opportunities;
    }

    public int SpentOpportunities() {
        return spent_opportunities;
    }

    public int TotalPixelsBurnt() {
        return pixels_burnt;
    }

    public int FiretrenchSpentOpportunities() {
        return this.firetrench_spent_opps;
    }

    private ColorToVegetation ObtainMapping(Color color) {

        ColorToVegetation mapping = new ColorToVegetation();
        if (color_mappings.ContainsKey(color)) {
            mapping = color_mappings[color];
        }
        else {

            ColorVegetationMapper col_mapper = new ColorVegetationMapper();
            col_mapper.mappings = color_mappings;

            mapping = col_mapper.FindClosestMapping(color);

        }

        return mapping;
    }

    private bool ExpandPixel(Cell origin_cell, List<Cell> targets, int rand_pixel, Texture2D heightmap, Texture2D map) {

        bool expanded = false;
        foreach (Cell cell in targets) if (ExpandToPixel(origin_cell, cell, heightmap, map)) expanded = true;

        if (expanded) AddPixelToBurntOnes(rand_pixel, map);
        else {

            origin_cell.opportunities -= 1;
            this.pixels_burning[rand_pixel] = origin_cell;
            if (IsFiretrench(origin_cell, map)) this.firetrench_spent_opps += 1;

            if (origin_cell.opportunities == 0) AddPixelToBurntOnes(rand_pixel, map);

        }                   

        this.spent_opportunities += 1;

        return expanded;
    }

    private bool ExpandToPixel(Cell origin_cell, Cell target, Texture2D heightmap, Texture2D map) {

        bool expanded = false;
        float expand_prob = ExpandProbability(origin_cell, target, true, true, true, true, true, heightmap, map);
        float dice = random.Next(0, 100) / 100.0f;

        if (dice < expand_prob) {
            // if expand prob is 0.87, the dice has 87/100 chanches. So the dice must be between 0 and 87 in order to expand.

            bool burnt = CheckFiretrench(target, expand_prob, map); // If its a firetrench, try to start a fire into it
            if (burnt) {
                reward += CalcReward(target.x, target.y, map);

                map.SetPixel(target.x, target.y, RED_COLOR);
                this.pixels_burning.Add(target);
                expanded = true;
            }
            
        }
        else if (IsFiretrench(target, map)) this.firetrench_spent_opps += 1;

        return expanded;
    }

    private void AddPixelToBurntOnes(int cell_index, Texture2D map) {

        Cell origin_cell = pixels_burning[cell_index];
        map.SetPixel(origin_cell.x, origin_cell.y, BLACK_COLOR);

        pixels_burning.RemoveAt(cell_index); // Remove origin_cell
        pixels_burnt += 1;
    }

    private float ExpandProbability(Cell origin_pixel, Cell target_pixel, bool veg_coeff_on, bool height_on, bool wind_on, bool hum_on, 
                                    bool temp_on, Texture2D heightmap, Texture2D map) { 

        float probability = 0.0f;
        if (NotBurnt(target_pixel.x, target_pixel.y, map)) { // if the pixel is not bunt or not burning

            // Store enable bits to multiply the coefficients (1 if true, 0 if false)
            int veg_enable = veg_coeff_on? 1 : 0;
            int height_enable = height_on? 1 : 0;
            int wind_enable = wind_on? 1 : 0;
            int hum_enable = hum_on? 1 : 0;
            int temp_enable = temp_on? 1 : 0;

            float alfa_weight = 0.25f*veg_enable; // Weight or the expand_coefficient. Initially was 0.3
            float h_weight = 0.12f*height_enable; // Weight for the height coefficient. Initially was 0.1
            float w_weight = 0.38f*wind_enable; // Wheight for the wind coefficient. Initially was 0.3
            float hum_weight = 0.10f*hum_enable; // Wheight for the humidity coefficient. Initially was 0.1
            float temp_weight = 0.10f*temp_enable; // Wheight for the temperature coefficient. Initially was 0.1
            float rand_weight = 0.05f * (0.05f + alfa_weight + h_weight + w_weight + hum_weight + temp_weight); // Wheight for the random factor coefficient. Initially was none existent

            float max_probability = alfa_weight + h_weight + w_weight + hum_weight + temp_weight + rand_weight; 
            // max_probability will be >= 0 and <= 1. Represents the maximum value we can get from the selected coefficients. 
            // P.E: 0.3+0.5 = 0.8. Thus we could only have values between 0 and 0.8 because the other coefficients are
            //      not taken into account. So we will later need to "scale" the value to the range of 0 to 1.

            Color pixel_color = map.GetPixel(target_pixel.x, target_pixel.y);
            ColorToVegetation mapping = color_mappings[pixel_color];

            float alfa = mapping.expandCoefficient;
            float w = CalcWindProbability(origin_pixel, target_pixel, heightmap); 
            float h = CalcHeightProbability(origin_pixel, target_pixel, heightmap);
            float hum = CalcHumidityProbability();
            float temp = CalcTemperatureProbanility();
            float rand = random.Next(0, 100) / 100.0f;

            probability = alfa*alfa_weight + h*h_weight + w*w_weight + hum*hum_weight + temp*temp_weight + rand*rand_weight;
            probability = probability / max_probability; // Ensure that probability is between 0 and 1. 
        }
        
        return probability;
    }

    private float CalcWindProbability(Cell origin_pixel, Cell target_pixel, Texture2D heightmap) {

        Vector3 pointA = Get3DPointAt(origin_pixel, heightmap);
        Vector3 pointB = Get3DPointAt(target_pixel, heightmap);
        Vector3 vec_displacement_norm = (pointB - pointA).normalized; // Vector fo displacement, normalized (-1 to 1);

        float scalar_prod = Vector3.Dot(wind_direction.normalized, vec_displacement_norm); // Scalar product
        
        float modul = wind_direction.magnitude;
        if (modul < 3) scalar_prod  *= 0.1f;
        else if (modul < 10) scalar_prod *= 0.2f;
        else if (modul < 15) scalar_prod *= 0.3f;
        else if (modul < 30) scalar_prod *= 0.5f;
        else if (modul < 50) scalar_prod *= 0.7f;
        else scalar_prod *= 1.0f; // modul > 50

        return scalar_prod;
    }

    private float CalcHeightProbability(Cell origin_pixel, Cell target_pixel, Texture2D heightmap) {

        // Get the height at each point from the heightmap
        Vector3 pointA = Get3DPointAt(origin_pixel, heightmap);
        Vector3 pointB = Get3DPointAt(target_pixel, heightmap);

        Vector3 vec_displacement = pointB - pointA; // Vector fo displacement, normalized (-1 to 1);
        Vector3 y_plane = new Vector3(0, 1, 0);

        float cos_alpha = Vector3.Dot(vec_displacement.normalized, y_plane.normalized); // Com que ens desplaçem només en N,S,E,W sí que és només el cosinus de l'algle. Sinó és el cosinus * un escalar

        return cos_alpha;
    }

    private float CalcHumidityProbability() {

        float probability = 1 - (humidity_coeff / 100.0f);

        if (humidity_coeff > 30 && humidity_coeff < 50) probability *= 0.7f;
        else if (humidity_coeff >= 50) probability *= 0.6f;   

        return probability;
    }

    private float CalcTemperatureProbanility() {

        float probability = temperature_coeff / 100.0f; 

        if (temperature_coeff > 30 && temperature_coeff <= 40) probability *= 0.7f;
        else if (temperature_coeff < 30) probability *= 0.6f;

        return probability;
    }

    private Vector3 Get3DPointAt(Cell point, Texture2D heightmap) {

        float terrain_width = Terrain.activeTerrain.terrainData.size.x;
        float terrain_depth = Terrain.activeTerrain.terrainData.size.z;
        float heightmap_texture_width = heightmap.width;
        float heightmap_texture_height = heightmap.height;

        float width_ratio = terrain_width / heightmap_texture_width;
        float height_ratio = terrain_depth / heightmap_texture_height;

        // Transform pixel coordinates into Terrain X and Y
        Vector2 terrain_coords_2D = new Vector2(point.x * width_ratio, point.y * height_ratio);

        Vector3 point_3D = new Vector3();
        point_3D.x = terrain_coords_2D.x;
        point_3D.y = 0;
        point_3D.z = terrain_coords_2D.y;

        // Get y coordinate (height) of the terrain on that point
        point_3D.y = Terrain.activeTerrain.SampleHeight(point_3D); 

        return point_3D;
    }

    private int CalcOpportunities(int x, int y, Texture2D map) {

        Color pixel_color = map.GetPixel(x, y);

        ColorVegetationMapper mapper = new ColorVegetationMapper();
        mapper.mappings = color_mappings;

        ColorToVegetation mapping = new ColorToVegetation();
        if (color_mappings.ContainsKey(pixel_color))  mapping = color_mappings[pixel_color];
        else mapping = mapper.FindClosestMapping(pixel_color);

        int opportunities = 0; // All colors with code 4XX, 0 opportunities
        if (mapping.ICGC_id < 200 && mapping.ICGC_id >= 100) opportunities = 2; // All colors with code 1XX, 2 opportunities
        else if (mapping.ICGC_id < 400 && mapping.ICGC_id >= 200 && mapping.ICGC_id != 231) opportunities = 3; // All colors with code 2XX or 3XX not burnt (code 231), 3 opportunities
        else if (mapping.ICGC_id == -1) opportunities = 1; // Firetrench ID

        return opportunities;
    }

}
