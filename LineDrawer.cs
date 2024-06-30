using UnityEngine;

public class LineDrawer {

    // Draw a line from (x0, y0) to (x1, y1)
    public void DrawLine(Vector2 origin, Vector2 destination, Color color, Texture2D map) {

        int x0 = (int)origin.x;
        int y0 = (int)origin.y;
        int x1 = (int)destination.x;
        int y1 = (int)destination.y;

        int dx = Mathf.Abs(x1 - x0);
        int dy = Mathf.Abs(y1 - y0);

        int sx = x0 < x1 ? 1 : -1;
        int sy = y0 < y1 ? 1 : -1;

        int err = dx - dy;

        bool done = false;
        while (!done){

            map.SetPixel(x0, y0, color);

            if (x0 == x1 && y0 == y1) done = true; // We have reached destination 
            else {

                int e2 = err * 2;
                if (e2 > -dy) {
                    err -= dy;
                    x0 += sx;
                }
                if (e2 < dx) {
                    err += dx;
                    y0 += sy;
                }

            }

        }

        map.Apply();

    }
}
