using System.Collections;
using System.Collections.Generic;

using UnityEngine;

public class Drawer {

    private int _width;

    public Drawer(int w) {
        this._width = w;
    }

    public void DrawLine(Vector2 origin, Vector2 destination, Color color, Texture2D map) {
        // Draw a line from origin (x0, y0) to destination (x1, y1)

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

            DrawPixelWithWidth(map, x0, y0, color);

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

    public void DrawCatmullRomSpline(List<Vector2> points, Color color, Texture2D map, float step) {

        for (int i = 0; i < points.Count - 1; i++) {
            Vector2 p0 = points[Mathf.Max(i - 1, 0)];
            Vector2 p1 = points[i];
            Vector2 p2 = points[i + 1];
            Vector2 p3 = points[Mathf.Min(i + 2, points.Count - 1)];
            
            for (float t = 0; t < 1.0f; t += step) { // lower t if we want a smother line. lower it to make it a dashed line.
                Vector2 point = CatmullRom(p0, p1, p2, p3, t);
                DrawPixelWithWidth(map, (int)point.x, (int)point.y, color);
            }
        }

    }

    public Vector2 CatmullRom(Vector2 p0, Vector2 p1, Vector2 p2, Vector2 p3, float t) {
        float t2 = t * t;
        float t3 = t2 * t;

        float a0 = -0.5f * t3 + t2 - 0.5f * t;
        float a1 = 1.5f * t3 - 2.5f * t2 + 1.0f;
        float a2 = -1.5f * t3 + 2.0f * t2 + 0.5f * t;
        float a3 = 0.5f * t3 - 0.5f * t2;

        return a0 * p0 + a1 * p1 + a2 * p2 + a3 * p3;
    }

    public void DrawBezierCurve(List<Vector2> points, Color color, Texture2D map, float step) {

        for (int i = 0; i < points.Count - 1; i += 3) {
            Vector2 p0 = points[i];
            Vector2 p1 = points[Mathf.Min(i + 1, points.Count - 1)];
            Vector2 p2 = points[Mathf.Min(i + 2, points.Count - 1)];
            Vector2 p3 = points[Mathf.Min(i + 3, points.Count - 1)];
            
            Vector2 previousPoint = p0;
            for (float t = 0; t <= 1.0f; t += step) {
                Vector2 point = Bezier(p0, p1, p2, p3, t);
                DrawLine(previousPoint, point, color, map);
                previousPoint = point;
            }
        }
    }

    public Vector2 Bezier(Vector2 p0, Vector2 p1, Vector2 p2, Vector2 p3, float t) {
        float u = 1 - t;
        float tt = t * t;
        float uu = u * u;
        float ttt = tt * t;
        float uuu = uu * u;

        Vector2 p = uuu * p0; // (1-t)^3 * p0
        p += 3 * uu * t * p1; // 3 * (1-t)^2 * t * p1
        p += 3 * u * tt * p2; // 3 * (1-t) * t^2 * p2
        p += ttt * p3; // t^3 * p3

        return p;
    }

    private void DrawPixelWithWidth(Texture2D map, int x, int y, Color color) {

        for (int i = -_width / 2; i <= _width / 2; i++) {
            for (int j = -_width / 2; j <= _width / 2; j++) {
                map.SetPixel(x + i, y + j, color);
            }
        }

    }

}
