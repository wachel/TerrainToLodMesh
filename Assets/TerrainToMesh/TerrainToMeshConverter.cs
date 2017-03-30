using System.Collections;
using System.Collections.Generic;
using System.Text;
using UnityEngine;



namespace TerrainConverter
{

    [System.Serializable]
    public class LodNodeTree
    {
        public List<byte> tree;
        public List<byte>[] alphaLayers;//存储每个LOD在每个层上四叉树的节点是否有效[layer]
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

        public LodNodeTree[] trees;
        TerrainToMeshTile[] tiles;

        public void CreateGridObjects(Node[] roots)
        {
            TerrainData terrainData = terrain.terrainData;
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
                    for (int i = 0; i < roots.Length; i++) {
                        tile.trees[i] = roots[i].FindSizeNode(x * gridSize, y * gridSize, gridSize);
                    }
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
                    Vector2 center = new Vector2(transform.position.x,transform.position.z) + new Vector2(y*gridSize,x * gridSize) + new Vector2(gridSize,gridSize) * 0.5f;
                    float t = (center - camera).magnitude / viewDistance;
                    int lodLevel = Mathf.Min((int)(maxLodLevel * t * t), maxLodLevel - 1);
                    
                    if(tiles[y * gridNumX + x].lodLevel != lodLevel) {
                        tiles[y * gridNumX + x].UpdateChildren(lodLevel);
                    }
                }
            }
        }
    }

}
