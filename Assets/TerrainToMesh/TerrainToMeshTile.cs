using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace TerrainConverter
{
    public class TerrainToMeshTile : MonoBehaviour
    {
        //邻接
        //  2
        //1   3
        //  0
        public TerrainToMeshTile[] adjacencies = new TerrainToMeshTile[4];

        public Material matBase { get; set; }
        public List<Material> matAdd { get; set; }
        public List<Material> matFirst { get; set; }

        public TerrainData terrainData { get; set; }
        public int lodLevel { get; set; }
        public Node[] roots { get; set; }//整个场景的根节点
        public Node[] trees { get; set;}//自己Tile区域的根节点

        public void UpdateChildren(int newLodLevel)
        {
            lodLevel = newLodLevel;
            while(transform.childCount > 0) {
                DestroyImmediate(transform.GetChild(0).gameObject);
            }
            if (lodLevel <= 1) {
                int[] layerIndices = new int[trees[0].validNums.Length];
                int maxIndex = trees[0].GetMaxValidNumIndex();
                for(int i = 0; i < trees[0].validNums.Length; i++) {
                    layerIndices[i == maxIndex ? 0 : (i < maxIndex ? i + 1 : i)] = i;
                }
                Mesh mesh = TerrainToMeshTool.CreateMesh(trees[lodLevel], terrainData, layerIndices);
                GameObject obj = new GameObject("layer_lod_" + lodLevel);
                MeshRenderer renderer = obj.AddComponent<MeshRenderer>();
                MeshFilter filter = obj.AddComponent<MeshFilter>();
                obj.transform.SetParent(transform, false);
                filter.sharedMesh = mesh;
                Material[] sharedMaterials = new Material[mesh.subMeshCount];
                for (int i = 0;i < mesh.subMeshCount; i++) {
                    sharedMaterials[i] = (i == 0) ? matFirst[layerIndices[i]] : matAdd [layerIndices[i]];
                }
                renderer.sharedMaterials = sharedMaterials;
            } else {
                Mesh mesh = TerrainToMeshTool.CreateMesh(trees[lodLevel], terrainData, new int[] { 0 });
                GameObject obj = new GameObject("base_lod_" + lodLevel);
                MeshRenderer renderer = obj.AddComponent<MeshRenderer>();
                MeshFilter filter = obj.AddComponent<MeshFilter>();
                renderer.sharedMaterial = matBase;
                filter.sharedMesh = mesh;
                obj.transform.SetParent(transform, false);
            }
        }
    }
}