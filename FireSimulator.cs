using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class FireSimulator {

    private List<ColorToVegetation> color_mappings;
    private Dictionary<(int, int), List<(int, int)>> pixels_burning;

    public FireSimulator(List<ColorToVegetation> mappings) {
        color_mappings = mappings;         
        pixels_burning = new Dictionary<(int, int), List<(int, int)>>(); // TODO: Carregar-me els veïns i buscar-los a cada iteració (ObtainNeighborsUnburned a cada iteració)
    }

    public List<int> InitRandomFire(MapManager map_manager, Texture2D map, Material map_material) {
        // Returns where the fire was originated

        List<int> res = new List<int>();

        int random_x = Random.Range(0, map.width);
        int random_y =  Random.Range(0, map.height);
        
        res.Add(random_x);
        res.Add(random_y);
        pixels_burning.Add((random_x, random_y), new List<(int, int)>());
        List<(int, int)> neighbors = ObtainNeighborsUnburned(random_x, random_y, map, map_manager);
        pixels_burning[(random_x, random_y)] = neighbors;

        map_manager.SetPixel(random_x, random_y, new Color(0.0f, 0.0f, 0.0f), map, map_material); // Set the piel to "Black" as it ois the "fire/burned" color

        Debug.Log("Init -- X: " + res[0] + " Y: " + res[1]);
        return res;
    }

    public List<(int, int)> ObtainNeighborsUnburned(int x, int y, Texture2D map, MapManager map_manager) {

        List<(int, int)> res = new List<(int, int)>();
        for (int i = x - 1; i <= x + 1; i++) {
            for (int j = y - 1; j <= y + 1; j++) {
                if (x >= 0 && y >= 0 && x <= map.width && y <= map.height && NotBurned(i, j, map_manager)) res.Add((i, j));
            }
        }

        return res;
    }

    public bool NotBurned(int x, int y, MapManager map_manager) {
        return map_manager.GetPixel(x, y) != new Color(0.0f, 0.0f, 0.0f);
    }

    public void ExpandFireRandom(MapManager map_manager, Texture2D map, Material map_material) { // TODO: Expandir-me només en 4 direccions (n,s,e,w)

        map = map_manager.GetMap();
        int rand_expand_pixels = Random.Range(0, 10); // number of pixels tu expand this iteration. pixels_burning.Count + 1
        for (int i = 0; i < rand_expand_pixels; i++) {

            int rand_pixel = Random.Range(0, pixels_burning.Keys.Count);
            (int, int) key = new List<(int, int)>(pixels_burning.Keys)[rand_pixel];

            List<(int, int)> neighbors = pixels_burning[key];

            // expand the fire to its neighbors (to all or to only some of them)
            if (neighbors.Count > 0) {
                foreach ((int, int) neigh in neighbors) {

                    int expand_prob = Random.Range(0, 100); // if >=50, expand the fire
                    if (expand_prob >= ExpandProbability(true, false, false)) { // TODO: Fer funció amb paràmetre que calculi la probabilitat necessària per expandir a aquella casella (segons vent, altura...)

                        map_manager.SetPixel(neigh.Item1, neigh.Item2, new Color(0.0f, 0.0f, 0.0f), map, map_material);
                        List<(int, int)> new_neighbors = ObtainNeighborsUnburned(neigh.Item1, neigh.Item2, map, map_manager);

                        if (pixels_burning.ContainsKey((neigh.Item1, neigh.Item2))) pixels_burning.Remove((neigh.Item1, neigh.Item2));
                        pixels_burning.Add((neigh.Item1, neigh.Item2), new_neighbors);

                    }

                }
            }

            pixels_burning.Remove(key);

        }

    }

    private int Probability(bool veg_coeff_on, bool height_on, bool wind_on) {

        int probability = 50;

        if (veg_coeff_on) {

        }
        if (height_on) {}
        if (wind_on) {}

        return probability;
    }

}
