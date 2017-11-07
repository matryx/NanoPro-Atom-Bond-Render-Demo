using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public static class CustomMesh
{
    public static Mesh CreateSphere(int nbLong = 24, int nbLat = 16, float radius = 1f)
    {
        Vector3[] vertices = new Vector3[(nbLong + 1) * nbLat + 2];
        float _pi = Mathf.PI;
        float _2pi = _pi * 2f;
        vertices[0] = Vector3.up * radius;
        for (int lat = 0; lat < nbLat; lat++)
        {
            float a1 = _pi * (float)(lat + 1) / (nbLat + 1);
            float sin1 = Mathf.Sin(a1);
            float cos1 = Mathf.Cos(a1);
            for (int lon = 0; lon <= nbLong; lon++)
            {
                float a2 = _2pi * (float)(lon == nbLong ? 0 : lon) / nbLong;
                float sin2 = Mathf.Sin(a2);
                float cos2 = Mathf.Cos(a2);
                vertices[lon + lat * (nbLong + 1) + 1] = new Vector3(sin1 * cos2, cos1, sin1 * sin2) * radius;
            }
        }
        vertices[vertices.Length - 1] = Vector3.up * -radius;

        Vector3[] normals = new Vector3[vertices.Length];
        for (int n = 0; n < vertices.Length; n++)
        {
            normals[n] = vertices[n].normalized;
        }

        Vector2[] uvs = new Vector2[vertices.Length];
        uvs[0] = Vector2.up;
        for (int lat = 0; lat < nbLat; lat++)
        {
            for (int lon = 0; lon <= nbLong; lon++)
            {
                uvs[lon + lat * (nbLong + 1) + 1] = new Vector2((float)lon / nbLong, 1f - (float)(lat + 1) / (nbLat + 1));
            }
        }

        int nbFaces = vertices.Length;
        int nbTriangles = nbFaces * 2;
        int nbIndexes = nbTriangles * 3;
        int[] triangles = new int[nbIndexes];
        // Top Cap
        int i = 0;
        for (int lon = 0; lon < nbLong; lon++)
        {
            triangles[i++] = lon + 2;
            triangles[i++] = lon + 1;
            triangles[i++] = 0;
        }
        // Middle
        for (int lat = 0; lat < nbLat - 1; lat++)
        {
            for (int lon = 0; lon < nbLong; lon++)
            {
                int current = lon + lat * (nbLong + 1) + 1;
                int next = current + nbLong + 1;
                triangles[i++] = current;
                triangles[i++] = current + 1;
                triangles[i++] = next + 1;
                triangles[i++] = current;
                triangles[i++] = next + 1;
                triangles[i++] = next;
            }
        }
        // Bottom Cap
        for (int lon = 0; lon < nbLong; lon++)
        {
            triangles[i++] = vertices.Length - 1;
            triangles[i++] = vertices.Length - (lon + 2) - 1;
            triangles[i++] = vertices.Length - (lon + 1) - 1;
        }

        var mesh = new Mesh();
        mesh.vertices = vertices;
        mesh.normals = normals;
        mesh.uv = uvs;
        mesh.triangles = triangles;
        mesh.colors = new Color[mesh.vertices.Length];
        return mesh;
    }

    public static Mesh CreateCylinder(float height = 1f, int nbSides = 24)
    {
        var _2pi = Mathf.PI * 2f;

        var hheight = height / 2f;
        var radius = 0.5f;

        var verticesNb = nbSides * 2;

        var vertices = new Vector3[verticesNb];
        var normals = new Vector3[verticesNb];
        var uvs = new Vector2[verticesNb];

        var triangles = new int[nbSides * 2 * 3];
        var triangle = 0;

        var order = -1;
        for (int i = 0; i < nbSides; i++)
        {
            var ratio = (float)i / (float)nbSides;
            var radii = ratio * _2pi;
            var cos = Mathf.Cos(radii);
            var sin = Mathf.Sin(radii);
            var idx1 = i;
            var idx2 = i + nbSides;
            vertices[idx1] = new Vector3(cos * radius, hheight, sin * radius);
            vertices[idx2] = new Vector3(cos * radius, -hheight, sin * radius);
            normals[idx1] = new Vector3(cos, 0, sin);
            normals[idx2] = new Vector3(cos, 0, sin);
            uvs[idx1] = new Vector2(ratio, 0f);
            uvs[idx2] = new Vector2(ratio, 1f);
            var t1 = triangle + 1;
            triangles[t1 - order] = idx1;
            triangles[t1] = idx2;
            triangles[t1 + order] = (idx1 + 1) % nbSides;
            var t2 = triangle + 4;
            triangles[t2 - order] = idx2;
            triangles[t2] = (idx2 + 1) % nbSides + nbSides;
            triangles[t2 + order] = (idx1 + 1) % nbSides;
            triangle += 6;
        }
        /**
         * Done
         */
        var mesh = new Mesh();
        mesh.vertices = vertices;
        mesh.normals = normals;
        mesh.uv = uvs;
        mesh.triangles = triangles;
        mesh.colors = new Color[mesh.vertices.Length];
        // Return mesh
        return mesh;
    }
}
