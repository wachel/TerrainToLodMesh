using System.Collections;
using System.Collections.Generic;
using System.Text;
using UnityEngine;



namespace TerrainConverter
{
    [System.Serializable]
    public class AlphaLayer
    {
        public List<byte> bytes;
    }


    [System.Serializable]
    public class LodNodeTree
    {
        public List<byte> tree;
        public AlphaLayer[] alphaLayers;//存储每个LOD在每个层上四叉树的节点是否有效[layer]
    }

    [ExecuteInEditMode]
    public class TerrainToMeshConverter : MonoBehaviour
    {
        public Terrain terrain;
        public int gridSize = 64;
        public float minError = 0.1f;
        public float maxError = 20.0f;
        public float alphaMapThreshold = 0.02f;
        public int maxLodLevel = 5;
        public bool createStaticMesh;

        public LodNodeTree[] trees;
        TerrainToMeshTile[] tiles;

        public Node[] roots { get; set; }
        

        public void OnEnable()
        {
            ClearChildren();
            LoadNodes();
            CreateGridObjects();
        }

        public void LoadNodes()
        {
            roots = new Node[maxLodLevel + 1];
            for (int i = 0; i <= maxLodLevel; i++) {
                Node node = new Node(0, 0, terrain.terrainData.heightmapWidth - 1);
                node.CreateChildFromBytes(trees[i].tree);
                node.SetAlphaBytes(trees[i].alphaLayers);
                roots[i] = node;
            }

        }

        public void ClearChildren()
        {
            var children = new List<GameObject>();
            foreach (Transform child in transform) children.Add(child.gameObject);
            children.ForEach(child => DestroyImmediate(child));
        }

        public void CreateGridObjects()
        {
            TerrainData terrainData = terrain.terrainData;
            float[,] heights = terrain.terrainData.GetHeights(0, 0, terrain.terrainData.heightmapWidth, terrain.terrainData.heightmapHeight);
            Material matBase = new Material(Shader.Find("Diffuse"));
            matBase.mainTexture = TerrainToMeshTool.BakeBaseTexture(terrainData);
            List<Material> matAdd = new List<Material>();
            List<Material> matFirst = new List<Material>();
            for (int l = 0; l < terrainData.alphamapLayers; l++) {
                matAdd.Add(TerrainToMeshTool.GetMaterial(terrainData, l, false));
                matFirst.Add(TerrainToMeshTool.GetMaterial(terrainData, l, true));
            }

            int w = terrainData.heightmapWidth - 1;
            int gridNumX = w / gridSize;
            tiles = new TerrainToMeshTile[gridNumX * gridNumX];

            for (int x = 0; x < gridNumX; x++) {
                for (int y = 0; y < gridNumX; y++) {
                    GameObject objGrid = new GameObject("mesh_" + x + "_" + y);
                    objGrid.transform.SetParent(this.transform, false);
                    TerrainToMeshTile tile = objGrid.AddComponent<TerrainToMeshTile>();
                    tiles[y * gridNumX + x] = tile;
                    tile.matBase = matBase;
                    tile.matAdd = matAdd;
                    tile.matFirst = matFirst;
                    tile.roots = roots;
                    tile.trees = new Node[roots.Length];
                    tile.lodLevel = -1;
                    tile.terrainData = terrainData;
                    tile.heights = heights;
                    for (int i = 0; i < roots.Length; i++) {
                        tile.trees[i] = roots[i].FindSizeNode(x * gridSize, y * gridSize, gridSize);
                    }
                }
            }
            for (int x = 0; x < gridNumX; x++) {
                for (int y = 0; y < gridNumX; y++) {
                    //  2
                    //1   3
                    //  0
                    tiles[y * gridNumX + x].adjacencies[0] = y > 0 ? tiles[(y - 1) * gridNumX + x] : null;
                    tiles[y * gridNumX + x].adjacencies[2] = y < gridNumX - 1 ? tiles[(y + 1) * gridNumX + x] : null;
                    tiles[y * gridNumX + x].adjacencies[1] = x > 0 ? tiles[y * gridNumX + x - 1] : null;
                    tiles[y * gridNumX + x].adjacencies[3] = x < gridNumX - 1 ? tiles[y * gridNumX + x + 1] : null;
                }
            }
            Update();
        }

        public void Update()
        {
            TerrainData terrainData = terrain.terrainData;
            int w = terrainData.heightmapWidth - 1;
            int gridNumX = w / gridSize;
            Vector2 camera = new Vector2(Camera.main.transform.position.x, Camera.main.transform.position.z);
            float viewDistance = Camera.main.farClipPlane;
            for (int x = 0; x < gridNumX; x++) {
                for (int y = 0; y < gridNumX; y++) {
                    Vector2 center = new Vector2(transform.position.x, transform.position.z) + new Vector2(y * gridSize, x * gridSize) + new Vector2(gridSize, gridSize) * 0.5f;
                    float t = Mathf.Clamp01((center - camera).magnitude / viewDistance);
                    tiles[y * gridNumX + x].newLodLevel = Mathf.Min((int)(maxLodLevel * t * t), maxLodLevel);
                }
            }
            for (int x = 0; x < gridNumX; x++) {
                for (int y = 0; y < gridNumX; y++) {
                    Vector2 center = new Vector2(transform.position.x, transform.position.z) + new Vector2(y * gridSize, x * gridSize) + new Vector2(gridSize, gridSize) * 0.5f;
                    float t = 1 - Mathf.Clamp01((center - camera).magnitude / viewDistance);
                    tiles[y * gridNumX + x].newLodLevel = Mathf.Min((int)(maxLodLevel * (1 - t * t)), maxLodLevel );
                    if (tiles[y * gridNumX + x].lodLevel != tiles[y * gridNumX + x].newLodLevel) {
                        tiles[y * gridNumX + x].UpdateChildren();
                    }
                }
            }

        }
    }

}
