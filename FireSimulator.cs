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
    private List<Cell> pixels_burned;
    private Vector3 wind_direction;
    private double humidity_coeff;
    private double temperature_coeff;
    private bool debug;
    private System.Random random = new System.Random();
    private readonly Color BLACK_COLOR = Color.black; // Unity no permet colors constants, posem readonly que fa la mateixa funció

    public FireSimulator(Dictionary<Color, ColorToVegetation> mappings, Vector3 wind = new Vector3(), double humidity = 0.0, double temperature = 0.0, bool debug = false) {

        this.color_mappings = mappings;
        this.pixels_burning = new List<Cell>();
        this.pixels_burned = new List<Cell>();
        this.wind_direction = wind;
        this.humidity_coeff = humidity;
        this.temperature_coeff = temperature;
        this.debug = debug;
    }

    public List<int> InitRandomFire(Texture2D map, Material map_material) {
        // Returns where the fire was originated

        List<int> res = new List<int>();

        int random_x = Random.Range(0, map.width);
        int random_y =  Random.Range(0, map.height);
        res.Add(random_x);
        res.Add(random_y);
        int opportunities = CalcOpportunities(random_x, random_y, map);  

        pixels_burning.Add(new Cell(random_x, random_y, opportunities));
        map.SetPixel(random_x, random_y, BLACK_COLOR); // Set the piel to "Black" as it ois the "fire/burned" color
        map.Apply();

        if (debug) Debug.Log("Init -- X: " + res[0] + " Y: " + res[1]);

        return res;
    }

    public void InitFireWithData(FireMapsPreparation.FireData fire_data, Texture2D map, Material map_material) {
        // Returns where the fire was originated

        int init_x = fire_data.init_x;
        int init_y = fire_data.init_y;
        int opportunities = CalcOpportunities(init_x, init_y, map);  

        pixels_burning.Add(new Cell(init_x, init_y, opportunities));
        map.SetPixel(init_x, init_y, BLACK_COLOR); // Set the piel to "Black" as it ois the "fire/burned" color
        map.Apply();

        if (debug) Debug.Log("Init -- X: " + init_x + " Y: " + init_y);

    }

    public List<Cell> BurntPixels() {
        return pixels_burned;
    }

    public List<Cell> ObtainNeighborsUnburned(Cell cell, Texture2D map) {

        List<Cell> res = new List<Cell>();
        /*
        // The next code gets all the neighbor pixels (8) including diagonals
        for (int i = x - 1; i <= x + 1; i++) {
            for (int j = y - 1; j <= y + 1; j++) {
                if (x >= 0 && y >= 0 && x < map.width && y < map.height && NotBurned(i, j, map_manager)) res.Add((i, j));
            }
        }
        */

        int x = cell.x;
        int y = cell.y;

        if (x - 1 >= 0 && NotBurned(x - 1, y, map)) res.Add(new Cell(x-1, y, CalcOpportunities(x-1, y, map))); // North neighbor
        if (y - 1 >= 0 && NotBurned(x, y - 1, map)) res.Add(new Cell(x, y-1, CalcOpportunities(x, y-1, map))); // West neighbor
        if (y + 1 < map.width && NotBurned(x, y + 1, map)) res.Add(new Cell(x, y+1, CalcOpportunities(x, y+1, map))); // East neighbor
        if (x + 1 < map.height && NotBurned(x + 1, y, map)) res.Add(new Cell(x+1, y, CalcOpportunities(x+1, y, map))); // South neighbor

        return res;
    }

    public bool NotBurned(int x, int y, Texture2D map) {
        return map.GetPixel(x, y) != BLACK_COLOR;
    }

    public bool ExpandFire(int max_span, Texture2D heightmap, Texture2D map, Material map_material) { 

        bool fire_ended = false;

        if (pixels_burning.Count > 0) {

            int rand_expand_pixels = random.Next(0, 10); // number of pixels tu expand this iteration. Maximum of 10
            for (int i = 0; i < rand_expand_pixels; i++) {

                // expand the fire to its neighbors (to all or to only some of them)
                int rand_pixel = random.Next(0, pixels_burning.Count);
                Cell origin_cell = pixels_burning[rand_pixel];
                List<Cell> neighbors = ObtainNeighborsUnburned(origin_cell, map);

                if (neighbors.Count > 0) {

                    bool expanded = false;
                    foreach (Cell neigh in neighbors) {

                        double expand_prob = ExpandProbability(origin_cell, neigh, true, true, true, true, true, heightmap, map);
                        double prob = random.Next(0, 100) / 100;
                        if (expand_prob >= prob) { // 0.2

                            map.SetPixel(neigh.x, neigh.y, BLACK_COLOR);
                            pixels_burning.Add(neigh);
                            expanded = true;
                            
                        }

                    }

                    if (expanded) AddPixelToBurntOnes(rand_pixel, origin_cell);
                    else {

                        origin_cell.opportunities -= 1;
                        pixels_burning[rand_pixel] = origin_cell;

                        if (origin_cell.opportunities == 0) AddPixelToBurntOnes(rand_pixel, origin_cell);

                    }                   

                }
                else AddPixelToBurntOnes(rand_pixel, origin_cell);
                
                if (pixels_burning.Count == 0) {
                    Debug.Log("The fire has ended");
                    fire_ended = true;
                    break; // Break the for loop
                }

                //---------DEBUG ONLY---------//
                if (pixels_burned.Count >= max_span) {
                    fire_ended = true;
                    break;
                }
                //---------DEBUG ONLY---------//

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

    private void AddPixelToBurntOnes(int cell_index, Cell origin_cell) {
        // Both cell_index and origin_cell are the same pixel but one is for removing from an index and the other is for adding directly the cell

        pixels_burning.RemoveAt(cell_index); // Remove origin_cell
        pixels_burned.Add(origin_cell);
    }

    private double ExpandProbability(Cell origin_pixel, Cell target_pixel, bool veg_coeff_on, bool height_on, bool wind_on, bool hum_on, 
                                    bool temp_on, Texture2D heightmap, Texture2D map) { 

        // TODO: Fer funció amb paràmetre que calculi la probabilitat necessària per expandir a aquella casella (segons vent, altura...)

        double probability = 0.0;
        if (hum_on && CalcHumidityProbability() < 30) probability = 1.0;
        else {
            
            Color pixel_color = map.GetPixel(target_pixel.x, target_pixel.y);
            if (pixel_color != new Color(0, 0, 0)) { // if not burned

                // Store enable bits to multiply the coefficients (1 if true, 0 if false)
                int veg_enable = veg_coeff_on? 1 : 0;
                int height_enable = height_on? 1 : 0;
                int wind_enable = wind_on? 1 : 0;
                int hum_enable = hum_on? 1 : 0;
                int temp_enable = temp_on? 1 : 0;

                double alfa_weight = 0.30*veg_enable; // Weight or the expand_coefficient
                double h_weight = 0.10*height_enable; // Weight for the height coefficient
                double w_weight = 0.50*wind_enable; // Wheight for the wind coefficient
                double hum_weight = 0.10*hum_enable; // Wheight for the humidity coefficient
                double temp_weight = 0.10*temp_enable; // Wheight for the temperature coefficient

                double max_probability = alfa_weight + h_weight + w_weight + hum_weight; 
                // max_probability will be >= 0 and <= 1. Represents the maximum value we can get from the selected coefficients. 
                // P.E: 0.3+0.5 = 0.8. Thus we could only have values between 0 and 0.75 because the other coefficients are
                //      not taken into account. So we will later need to "scale" the value to the range of 0 to 1.

                ColorToVegetation mapping = color_mappings[pixel_color];

                double alfa = mapping.expandCoefficient;
                double w = CalcWindProbability(origin_pixel, target_pixel, heightmap); 
                double h = CalcHeightProbability(origin_pixel, target_pixel, heightmap);
                double hum = CalcHumidityProbability();
                double temp = CalcTemperatureProbanility();

                probability = alfa*alfa_weight + h*h_weight + w*w_weight + hum*hum_weight + temp*temp_weight;
                probability = probability / max_probability; // Ensure that probability is between 0 and 1. 

            }

        }
        
        return probability;
    }

    private double CalcWindProbability(Cell origin_pixel, Cell target_pixel, Texture2D heightmap) {

        Vector3 pointA = Get3DPointAt(origin_pixel, heightmap);
        Vector3 pointB = Get3DPointAt(target_pixel, heightmap);
        Vector3 vec_displacement_norm = (pointB - pointA).normalized; // Vector fo displacement, normalized (-1 to 1);

        float scalar_prod = Vector3.Dot(wind_direction.normalized, vec_displacement_norm); // Scalar product
        
        float modul = wind_direction.magnitude;
        if (modul < 3) scalar_prod *= 0.05f;
        else if (modul < 10) scalar_prod *= 0.1f;
        else if (modul < 15) scalar_prod *= 0.2f;
        else if (modul < 30) scalar_prod *= 0.3f;
        else if (modul < 50) scalar_prod *= 0.4f;
        else if (modul > 50) scalar_prod *= 0.5f;

        return scalar_prod;
    }

    private double CalcHeightProbability(Cell origin_pixel, Cell target_pixel, Texture2D heightmap) {

        // Get the height at each point from the heightmap
        Vector3 pointA = Get3DPointAt(origin_pixel, heightmap);
        Vector3 pointB = Get3DPointAt(target_pixel, heightmap);

        Vector3 vec_displacement = pointB - pointA; // Vector fo displacement, normalized (-1 to 1);
        Vector3 y_plane = new Vector3(0, 1, 0);

        float cos_alpha = Vector3.Dot(vec_displacement.normalized, y_plane.normalized); // Com que ens desplaçem només en N,S,E,W sí que és només el cosinus de l'algle. Sinó és el cosinus * un escalar

        return cos_alpha;
    }

    private double CalcHumidityProbability() {
        return 1 - (humidity_coeff / 100.0);
    }

    private double CalcTemperatureProbanility() {
        return 1 - (temperature_coeff / 100.0); 
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

        int opportunities = 0; // All colors with code 4XX
        if (mapping.ICGC_id < 200 && mapping.ICGC_id >= 100) opportunities = 2; // All colors with code 1XX
        else if (mapping.ICGC_id < 400 && mapping.ICGC_id >= 200) opportunities = 3; // All colors with code 2XX or 3XX

        return opportunities;
    }

}
