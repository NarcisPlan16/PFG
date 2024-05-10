using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class FireSimulator {

    private Dictionary<Color, ColorToVegetation> color_mappings;
    private HashSet<(int, int)> pixels_burning;

    private Vector3 wind_direction;

    public FireSimulator(List<ColorToVegetation> mappings, Vector3 wind) {

        color_mappings = new Dictionary<Color, ColorToVegetation>();
        foreach (ColorToVegetation mapping in mappings) {
            color_mappings.Add(mapping.color, mapping);
        }

        pixels_burning = new HashSet<(int, int)>(); // TODO: Carregar-me els veïns i buscar-los a cada iteració (ObtainNeighborsUnburned a cada iteració)
        wind_direction = wind;
    }

    public List<int> InitRandomFire(MapManager map_manager, Texture2D map, Material map_material) {
        // Returns where the fire was originated

        List<int> res = new List<int>();

        int random_x = Random.Range(0, map.width);
        int random_y =  Random.Range(0, map.height);
        
        res.Add(random_x);
        res.Add(random_y);
        pixels_burning.Add((random_x, random_y));
        map_manager.SetPixel(random_x, random_y, new Color(0.0f, 0.0f, 0.0f), map, map_material); // Set the piel to "Black" as it ois the "fire/burned" color

        Debug.Log("Init -- X: " + res[0] + " Y: " + res[1]);

        return res;
    }

    public List<(int, int)> ObtainNeighborsUnburned(int x, int y, Texture2D map, MapManager map_manager) {

        List<(int, int)> res = new List<(int, int)>();
        /*
        // The next code gets all the neighbor pixels (8) including diagonals
        for (int i = x - 1; i <= x + 1; i++) {
            for (int j = y - 1; j <= y + 1; j++) {
                if (x >= 0 && y >= 0 && x < map.width && y < map.height && NotBurned(i, j, map_manager)) res.Add((i, j));
            }
        }
        */

        if (x - 1 >= 0 && NotBurned(x - 1, y, map_manager)) res.Add((x-1, y)); // North neighbor
        if (y - 1 >= 0 && NotBurned(x, y - 1, map_manager)) res.Add((x, y-1)); // West Neighbor
        if (y + 1 < map.width && NotBurned(x, y + 1, map_manager)) res.Add((x, y+1)); // East Neighbor
        if (x + 1 < map.height && NotBurned(x + 1, y, map_manager)) res.Add((x+1, y)); // South Neighbor

        return res;
    }

    public bool NotBurned(int x, int y, MapManager map_manager) {
        return map_manager.GetPixel(x, y) != new Color(0.0f, 0.0f, 0.0f);
    }

    public void ExpandFireRandom(Texture2D heightmap, MapManager map_manager, Texture2D map, Material map_material) { // TODO: Expandir-me només en 4 direccions (n,s,e,w)

        if (pixels_burning.Count > 0) {

            map = map_manager.GetMap();
            int rand_expand_pixels = Random.Range(0, 10); // number of pixels tu expand this iteration.
            for (int i = 0; i < rand_expand_pixels; i++) {

                int rand_pixel = Random.Range(0, pixels_burning.Count);
                List<(int, int)> pixels_burning_list = new List<(int, int)>(pixels_burning);

                (int, int) key = pixels_burning_list[rand_pixel];
                List<(int, int)> neighbors =  ObtainNeighborsUnburned(key.Item1, key.Item2, map, map_manager);

                // expand the fire to its neighbors (to all or to only some of them)
                if (neighbors.Count > 0) {
                    foreach ((int, int) neigh in neighbors) {

                        double expand_prob = ExpandProbability(key, neigh, true, true, true, heightmap, map, map_manager);
                        if (expand_prob >= 0.2) { 

                            map_manager.SetPixel(neigh.Item1, neigh.Item2, new Color(0.0f, 0.0f, 0.0f), map, map_material);
                            pixels_burning.Add((neigh.Item1, neigh.Item2));

                        }

                    }
                }

                pixels_burning.Remove(key);
                if (pixels_burning.Count == 0) break; // Break the for loop
                

            }

        }
        else Debug.Log("The fire has ended");

    }

    private double ExpandProbability((int, int) origin_pixel, (int, int) target_pixel, bool veg_coeff_on, bool height_on, bool wind_on, 
                                    Texture2D heightmap, Texture2D map, MapManager map_manager) { 

        // TODO: Fer funció amb paràmetre que calculi la probabilitat necessària per expandir a aquella casella (segons vent, altura...)

        double probability = 0.0;

        Color pixel_color = map.GetPixel(target_pixel.Item1, target_pixel.Item2);
        if (pixel_color != new Color(0, 0, 0)) { // if not burned

            // Store enable bits to multiply the coefficients (1 if true, 0 if false)
            int veg_enable = veg_coeff_on? 1 : 0;
            int height_enable = height_on? 1 : 0;
            int wind_enable = wind_on? 1 : 0;

            double alfa_weight = 0.51*veg_enable; // Weight or the expand_coefficient
            double h_weight = 0.32*height_enable; // Weight for the height coefficient
            double w_weight = 0.07*wind_enable; // Wheight for the winf coefficient
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

    private double CalcWindProbability((int, int) origin_pixel, (int, int) target_pixel, Texture2D heightmap) {

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

    private double CalcHeightProbability((int, int) origin_pixel, (int, int) target_pixel, Texture2D heightmap) {

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

    private Vector3 Get3DPointAt((int, int) point, Texture2D heightmap) {

        float terrain_width = Terrain.activeTerrain.terrainData.size.x;
        float terrain_depth = Terrain.activeTerrain.terrainData.size.z;
        float heightmap_texture_width = heightmap.width;
        float heightmap_texture_height = heightmap.height;

        float width_ratio = terrain_width / heightmap_texture_width;
        float height_ratio = terrain_depth / heightmap_texture_height;

        // Transform pixel coordinates into Terrain X and Y
        Vector2 terrain_coords_2D = new Vector2(point.Item1 * width_ratio, point.Item2 * height_ratio);

        Vector3 point_3D = new Vector3();
        point_3D.x = terrain_coords_2D.x;
        point_3D.y = 0;
        point_3D.z = terrain_coords_2D.y;

        // Get z coordinate (height) of the terrain on that point
        point_3D.y = Terrain.activeTerrain.SampleHeight(point_3D); 

        return point_3D;
    }

}
