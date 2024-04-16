using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class FireSimulator {

    private Texture2D map;
    private Material map_material;
    private MapManager map_manager;
    private Dictionary pixels_burning;

    public FireSimulator(Texture2D mapa, MapManager manager, Material map_mat) {
        map = mapa;
        map_manager = manager;
        map_material = map_mat;
        pixels_burning = new Dictionary();
    }

    public List<int> InitRandomFire() {

        List<int> res = new List<int>();

        int random_x = Random.Range(0, map.width);
        int random_y =  Random.Range(0, map.height);
        
        res.Add(random_x);
        res.Add(random_y);
        pixels_burning.Add((random_x, random_y), new List<(int, int)>());
        neighbors = ObtainNeighborsUnburned(random_x, random_y);
        pixels_burning[(random_x, random_y)] = neighbors;

        map_manager.SetPixel(random_x, random_y, new Color(0.0f, 0.0f, 0.0f), map, map_material); // Set the piel to "Black" as it ois the "fire/burned" color

        return res;
    }

    public List<(int, int)> ObtainNeighborsUnburned(int x, int y) {

        List<(int, int)> res = new List<(int, int)>();
        for (int i = x - 1; i <= x + 1; i++) {
            for (int j = y - 1; j <= y + 1; j++) {
                if (x >= 0 && y >= 0 && x =< map.width && y =< map.height && NotBurned(i, j)) res.Add((i, j));
            }
        }

        return res;
    }

    public bool NotBurned(int x, int y) {
        return map_manager.GetPixel(x, y) != new Color(0.0f, 0.0f, 0.0f);
    }

    public void ExpandFire() {

        map = map_manager.GetMap();
        int rand_expand_pixels = Random.Range(0, pixels_burning.Count); // number of pixels tu expand 
        for (int i = 0; i < rand_expand_pixels; i++) {

            int rand_pixel = Random.range(0, pixels_burning.Keys.Count);
            List<(int, int)> keys = pixels_burning.Keys.ToList();
            key = keys[rand_pixel];

            List<(int, int)> neighbors = pixels_burning[key];
            pixels_burning.Remove(key);
            // expand the fire to its neighbors (to all or to only some of them)
            int n_rand_neigh = Random.Range(0, neighbors.Count);
            for (int n_neigh = 0; n_neigh < n_rand_neigh; n_neigh++) {

                int rand_neigh = Random.Range(0, neighbors.Count);
                (int, int) neigh = neighbors[rand_neigh];

                map_manager.SetPixel(neigh.Item1, neigh.Item2, new Color(0.0f, 0.0f, 0.0f), map, map_material);

                pixels_burning.Add((neigh.Item1, neigh.Item2), new List<(int, int)>());
                neighbors = ObtainNeighborsUnburned(neigh.Item1, neigh.Item2);
                pixels_burning[(random_x, random_y)] = neighbors;

                neighbors.Remove(rand_neigh);
                // TODO: Comprovar que ho fa correctament

            }

        }

    }

}
