using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class FireSimulator {

    //private Texture2D map;
    //private Material map_material;
    //private MapManager map_manager;
    private Dictionary<(int, int), List<(int, int)>> pixels_burning;

    public FireSimulator() {
        //map = mapa;
        //map_manager = manager;
        //map_material = map_mat;
        pixels_burning = new Dictionary<(int, int), List<(int, int)>>();
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

    public void ExpandFire(MapManager map_manager, Texture2D map, Material map_material) {

        map = map_manager.GetMap();
        int rand_expand_pixels = Random.Range(0, pixels_burning.Count + 1); // number of pixels tu expand 
        for (int i = 0; i < rand_expand_pixels; i++) {

            int rand_pixel = Random.Range(0, pixels_burning.Keys.Count);
            (int, int) key = new List<(int, int)>(pixels_burning.Keys)[rand_pixel];

            List<(int, int)> neighbors = pixels_burning[key];
            pixels_burning.Remove(key);

            // expand the fire to its neighbors (to all or to only some of them)
            if (neighbors.Count > 0) {

                int n_rand_neigh = Random.Range(0, neighbors.Count); // number of neighbors to expand the fire to
                for (int n_neigh = 0; n_neigh < n_rand_neigh; n_neigh++) {

                    int rand_neigh = Random.Range(0, neighbors.Count);

                    bool enable = true;
                    if (enable) {
                        enable = false;
                        Debug.Log("neighbors.Count = " + neighbors.Count);
                        Debug.Log("Random neigh = " + rand_neigh);
                        Debug.Log("-----------------------------------------------------------");
                    }

                    (int, int) neigh = neighbors[rand_neigh]; // TODO: Dont know why but here halts an error

                    map_manager.SetPixel(neigh.Item1, neigh.Item2, new Color(0.0f, 0.0f, 0.0f), map, map_material);
                    neighbors = ObtainNeighborsUnburned(neigh.Item1, neigh.Item2, map, map_manager);

                    if (pixels_burning.ContainsKey((neigh.Item1, neigh.Item2))) pixels_burning.Remove((neigh.Item1, neigh.Item2));
                    pixels_burning.Add((neigh.Item1, neigh.Item2), neighbors);

                    neighbors.Remove(neigh);
                    //Debug.Log("Expanded to -- X: " + neigh.Item1 + " Y: " + neigh.Item2);

                }

            }

        }

    }

}
