using UnityEngine;

public class TerrainMesher : MonoBehaviour
{
    [SerializeField] private Terrain terrain;
    [SerializeField] private Material meshMaterial;
    [SerializeField] private bool generateCollider = true;
    [SerializeField] [Range(1, 32)] private int resolution = 8;

    [ContextMenu("Convert Terrain to Mesh")]
    public void ConvertTerrain()
    {
        if (terrain == null)
        {
            Debug.LogError("Terrain not assigned");
            return;
        }

        TerrainData terrainData = terrain.terrainData;
        int w = terrainData.heightmapResolution;
        int h = terrainData.heightmapResolution;
        Vector3 meshScale = terrainData.size;

        int tRes = resolution;
        meshScale = new Vector3(meshScale.x / (w - 1) * tRes, meshScale.y, meshScale.z / (h - 1) * tRes);
        Vector2 uvScale = new Vector2(1.0f / (w - 1), 1.0f / (h - 1));
        float[,] tData = terrainData.GetHeights(0, 0, w, h);

        w = (w - 1) / tRes + 1;
        h = (h - 1) / tRes + 1;
        Vector3[] vertices = new Vector3[w * h];
        Vector2[] uvs = new Vector2[w * h];
        int[] triangles = new int[(w - 1) * (h - 1) * 6];

        // Build vertices and UVs
        for (int y = 0; y < h; y++)
        {
            for (int x = 0; x < w; x++)
            {
                vertices[y * w + x] = Vector3.Scale(meshScale, new Vector3(x, tData[y * tRes, x * tRes], y));
                uvs[y * w + x] = Vector2.Scale(new Vector2(x * tRes, y * tRes), uvScale);
            }
        }

        // Build triangle indices
        int index = 0;
        for (int y = 0; y < h - 1; y++)
        {
            for (int x = 0; x < w - 1; x++)
            {
                triangles[index++] = (y * w) + x;
                triangles[index++] = ((y + 1) * w) + x;
                triangles[index++] = (y * w) + x + 1;

                triangles[index++] = ((y + 1) * w) + x;
                triangles[index++] = ((y + 1) * w) + x + 1;
                triangles[index++] = (y * w) + x + 1;
            }
        }

        // Create mesh
        Mesh mesh = new Mesh();
        mesh.vertices = vertices;
        mesh.uv = uvs;
        mesh.triangles = triangles;
        mesh.RecalculateNormals();

        // Create new GameObject with mesh
        GameObject meshObject = new GameObject(terrain.gameObject.name + "_Mesh");
        meshObject.transform.SetParent(terrain.transform.parent);
        meshObject.transform.position = terrain.transform.position;
        meshObject.transform.rotation = terrain.transform.rotation;
        meshObject.transform.localScale = terrain.transform.localScale;

        MeshFilter meshFilter = meshObject.AddComponent<MeshFilter>();
        meshFilter.mesh = mesh;

        MeshRenderer meshRenderer = meshObject.AddComponent<MeshRenderer>();
        if (meshMaterial != null)
            meshRenderer.material = meshMaterial;
        else
            meshRenderer.material = new Material(Shader.Find("Standard"));

        if (generateCollider)
        {
            meshObject.AddComponent<MeshCollider>();
        }

        Debug.Log($"Terrain converted to mesh: {vertices.Length} vertices, {triangles.Length / 3} triangles");
    }
}