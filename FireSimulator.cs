using System.Collections;
using System.Collections.Generic;
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

    private const int MAX_TRIES = 5;

    private Dictionary<Color, ColorToVegetation> color_mappings;
    private HashSet<Cell> pixels_burning;

    private Vector3 wind_direction;

    public FireSimulator(List<ColorToVegetation> mappings, Vector3 wind) {

        color_mappings = new Dictionary<Color, ColorToVegetation>();
        foreach (ColorToVegetation mapping in mappings) {
            color_mappings.Add(mapping.color, mapping);
        }

        pixels_burning = new HashSet<Cell>(); // TODO: Carregar-me els veïns i buscar-los a cada iteració (ObtainNeighborsUnburned a cada iteració)
        wind_direction = wind;
    }

    public List<int> InitRandomFire(MapManager map_manager, Texture2D map, Material map_material) {
        // Returns where the fire was originated

        List<int> res = new List<int>();

        int random_x = Random.Range(0, map.width);
        int random_y =  Random.Range(0, map.height);
        
        //----------Debug only------
        random_x = 206;
        random_y = 206;
        //--------------------------

        res.Add(random_x);
        res.Add(random_y);

        Color pixel_color = map.GetPixel(random_x, random_y);
        ColorToVegetation mapping = color_mappings[pixel_color];
        int opportunities = (int) Mathf.Round(mapping.expandCoefficient * MAX_TRIES);

        pixels_burning.Add(new Cell(random_x, random_y, opportunities));
        map_manager.SetPixel(random_x, random_y, new Color(0.0f, 0.0f, 0.0f), map, map_material); // Set the piel to "Black" as it ois the "fire/burned" color

        Debug.Log("Init -- X: " + res[0] + " Y: " + res[1]);

        return res;
    }

    public List<Cell> ObtainNeighborsUnburned(Cell cell, Texture2D map, MapManager map_manager) {

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

        if (x - 1 >= 0 && NotBurned(x - 1, y, map_manager)) res.Add(new Cell(x-1, y, CalcOpportunities(x-1, y, map))); // North neighbor
        if (y - 1 >= 0 && NotBurned(x, y - 1, map_manager)) res.Add(new Cell(x, y-1, CalcOpportunities(x, y-1, map))); // West neighbor
        if (y + 1 < map.width && NotBurned(x, y + 1, map_manager)) res.Add(new Cell(x, y+1, CalcOpportunities(x, y+1, map))); // East neighbor
        if (x + 1 < map.height && NotBurned(x + 1, y, map_manager)) res.Add(new Cell(x+1, y, CalcOpportunities(x+1, y, map))); // South neighbor

        return res;
    }

    public bool NotBurned(int x, int y, MapManager map_manager) {
        return map_manager.GetPixel(x, y) != new Color(0.0f, 0.0f, 0.0f);
    }

    public void ExpandFireRandom(Texture2D heightmap, MapManager map_manager, Texture2D map, Material map_material) { // TODO: Expandir-me només en 4 direccions (n,s,e,w)

        if (pixels_burning.Count > 0) {

            map = map_manager.GetMap();
            int rand_expand_pixels = Random.Range(0, 10); // number of pixels tu expand this iteration. Maximum of 10
            for (int i = 0; i < rand_expand_pixels; i++) {

                int rand_pixel = Random.Range(0, pixels_burning.Count);
                List<Cell> pixels_burning_list = new List<Cell>(pixels_burning);

                Cell origin_cell = pixels_burning_list[rand_pixel];
                List<Cell> neighbors = ObtainNeighborsUnburned(origin_cell, map, map_manager);

                // expand the fire to its neighbors (to all or to only some of them)
                if (neighbors.Count > 0) {

                    bool expanded = false;
                    foreach (Cell neigh in neighbors) {

                        double expand_prob = ExpandProbability(origin_cell, neigh, true, true, true, heightmap, map, map_manager);
                        if (expand_prob >= 0.2) { // 0.2

                            map_manager.SetPixel(neigh.x, neigh.y, new Color(0.0f, 0.0f, 0.0f), map, map_material);
                            pixels_burning.Add(neigh);
                            expanded = true;

                        }

                    }

                    if (!expanded) origin_cell.opportunities--;
                    else pixels_burning.Remove(origin_cell);

                }

                if (origin_cell.opportunities == 0) pixels_burning.Remove(origin_cell);
                if (pixels_burning.Count == 0) break; // Break the for loop

            }

        }
        else Debug.Log("The fire has ended");

    }

    private double ExpandProbability(Cell origin_pixel, Cell target_pixel, bool veg_coeff_on, bool height_on, bool wind_on, 
                                    Texture2D heightmap, Texture2D map, MapManager map_manager) { 

        // TODO: Fer funció amb paràmetre que calculi la probabilitat necessària per expandir a aquella casella (segons vent, altura...)

        double probability = 0.0;

        Color pixel_color = map.GetPixel(target_pixel.x, target_pixel.y);
        if (pixel_color != new Color(0, 0, 0)) { // if not burned

            // Store enable bits to multiply the coefficients (1 if true, 0 if false)
            int veg_enable = veg_coeff_on? 1 : 0;
            int height_enable = height_on? 1 : 0;
            int wind_enable = wind_on? 1 : 0;

            double alfa_weight = 0.37*veg_enable; // Weight or the expand_coefficient
            double h_weight = 0.25*height_enable; // Weight for the height coefficient
            double w_weight = 0.28*wind_enable; // Wheight for the winf coefficient
            double r_weight = 0.1; // Wheight for the random Coefficient
            double max_probability = alfa_weight + h_weight + w_weight + r_weight; 
            // max_probability will be >= 0 and <= 1. Represents the maximum value we can get from the selected coefficients. 
            // P.E: 0.45+0.3 = 0.75. Thus we could only have values between 0 and 0.75 because the other coefficients are
            //      not taken into account. So we will later need to "scale" the value to the range of 0 to 1.

            ColorToVegetation mapping = color_mappings[pixel_color];
            //Debug.Log("Expand Coefficient: " + mapping.expandCoefficient);

            double alfa = mapping.expandCoefficient;
            double w = CalcWindProbability(origin_pixel, target_pixel, heightmap); // TODO
            double h = CalcHeightProbability(origin_pixel, target_pixel, heightmap); // TODO
            double r = Random.Range(0.0f, 1.0f);

            probability = alfa*alfa_weight + h*h_weight + w*w_weight + r*r_weight;
            probability = probability / max_probability; // Ensure that probability is between 0 and 1. 

            //Debug.Log("w: " + w);
            //Debug.Log("h: " + h);
            //Debug.Log("Expand Probability: " + probability);
            //Debug.Log("Max probabiliy: " + max_probability);

        }
        
        return probability;
    }

    private double CalcWindProbability(Cell origin_pixel, Cell target_pixel, Texture2D heightmap) {

        Vector3 pointA = Get3DPointAt(origin_pixel, heightmap);
        Vector3 pointB = Get3DPointAt(target_pixel, heightmap);
        Vector3 vec_displacement_norm = (pointB - pointA).normalized; // Vector fo displacement, normalized (-1 to 1);

        float scalar_prod = Vector3.Dot(wind_direction.normalized, vec_displacement_norm); // Scalar product
        //double res = scalar_prod / (vec_displacement_norm * wind_direction.normalized);

        //Debug.Log("Scalar prod: " + scalar_prod);
        
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

        //Debug.Log("Displacement: " +  vec_displacement);
        //Debug.Log("Cos alpha: " + cos_alpha);

        return cos_alpha;
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

        // Get z coordinate (height) of the terrain on that point
        point_3D.y = Terrain.activeTerrain.SampleHeight(point_3D); 

        return point_3D;
    }

    private int CalcOpportunities(int x, int y, Texture2D map) {

        Color pixel_color = map.GetPixel(x, y);
        ColorToVegetation mapping = color_mappings[pixel_color];

        return (int) Mathf.Round(mapping.expandCoefficient * MAX_TRIES);
    }

}
