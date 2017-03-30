using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
namespace TerrainConverter
{

    [CustomEditor(typeof(TerrainToMeshConverter))]
    public class TerrainToMeshConverterInspector : Editor
    {
        TerrainToMeshConverter converter;
        public void OnEnable()
        {
            converter = target as TerrainToMeshConverter;
        }

        public override void OnInspectorGUI()
        {
            base.OnInspectorGUI();
            if (converter.terrain) {
                GUILayout.BeginHorizontal();
                int gridNum = (converter.terrain.terrainData.heightmapWidth - 1) / converter.gridSize;

                if (GUILayout.Button("生成分层网格")) {
                    converter.trees = new LodNodeTree[converter.maxLodLevel + 1];
                    for (int i= 0; i<=converter.maxLodLevel; i++) {
                        converter.trees[i] = new LodNodeTree();
                        float error = converter.minError * Mathf.Pow(Mathf.Pow(converter.maxError / converter.minError, 1.0f / (converter.maxLodLevel - 1)), i);
                        Node tempNode = CreateNode(error);
                        List<byte> bytes = new List<byte>();
                        tempNode.ToBytes(bytes);
                        converter.trees[i].tree = bytes;
                        converter.trees[i].alphaLayers = tempNode.GetAlphaBytes();
                    }

                    Node[] nodes = new Node[converter.maxLodLevel + 1];
                    for(int i= 0; i <= converter.maxLodLevel; i++) {
                        //Node node = new Node(0, 0, converter.terrain.terrainData.heightmapWidth - 1);
                        //node.CreateChildFromBytes(converter.trees[0].tree);
                        //node.SetAlphaBytes(converter.trees[0].alphaLayers);

                        float error = converter.minError * Mathf.Pow(Mathf.Pow(converter.maxError / converter.minError, 1.0f / (converter.maxLodLevel - 1)), i);
                        Node tempNode = CreateNode(error);
                        nodes[i] = tempNode;
                    }

                    var children = new List<GameObject>();
                    foreach (Transform child in converter.transform) children.Add(child.gameObject);
                    children.ForEach(child => DestroyImmediate(child));

                    converter.CreateGridObjects(nodes);
                    
                    //List<MeshInfo>[,] meshes = new List<MeshInfo>[gridNum, gridNum];
                    //for (int i = 0; i < gridNum; i++) {
                    //    for (int j = 0; j < gridNum; j++) {
                    //        meshes[i, j] = new List<MeshInfo>();
                    //    }
                    //}
                    //
                    //{
                    //    List<MeshInfo> ms = TerrainToMeshTool.CreateMeshes(node, converter.terrain.terrainData, converter.gridSize, -1);
                    //    foreach (MeshInfo m in ms) {
                    //        meshes[m.gridX, m.gridY].Add(m);
                    //    }
                    //}
                    //
                    //for (int l = 0; l < converter.terrain.terrainData.alphamapLayers; l++) {
                    //    List<MeshInfo> ms = TerrainToMeshTool.CreateMeshes(node,converter.terrain.terrainData,converter.gridSize, l);
                    //    foreach (MeshInfo m in ms) {
                    //        meshes[m.gridX, m.gridY].Add(m);
                    //    }
                    //}
                    //
                    //TerrainToMeshTool.CreateObjects(converter.terrain.terrainData,converter.transform, meshes);
                }
                GUILayout.EndHorizontal();
            }
        }

        float GetHeightError(float[,] heights, int x, int y, int step,out bool swapEdge)
        {
            float p0 = heights[x, y];
            float p1 = heights[x + step, y];
            float p2 = heights[x, y + step];
            float p3 = heights[x + step, y + step];
            float center = heights[x + step / 2, y + step / 2];
            float bottom = heights[x + step / 2, y];
            float left = heights[x, y + step / 2];
            float top = heights[x + step / 2, y + step];
            float right = heights[x + step, y + step / 2];
            float error0 = Mathf.Abs(center - (p0 + p3) / 2);
            float error1 = Mathf.Abs(center - (p1 + p2) / 2);
            swapEdge = error0 < error1;

            float error = Mathf.Min(error0, error1);
            error += Mathf.Abs(bottom - (p0 + p1) / 2);
            error += Mathf.Abs(left - (p0 + p2) / 2);
            error += Mathf.Abs(top - (p2 + p3) / 2);
            error += Mathf.Abs(right - (p3 + p1) / 2);
            return error;
        }

        bool CheckSourrond(Node root, int x, int y, int size)
        {
            if (x - size >= 0) {
                Node node = root.FindSizeNode(x - size, y, size);
                if(node.childs != null  && (node.childs[1].childs != null || node.childs[3].childs != null)) {
                    return false;
                }
            }
            if(x + size + size < root.size) {
                Node node = root.FindSizeNode(x + size, y, size);
                if (node.childs != null && (node.childs[0].childs != null || node.childs[2].childs != null)) {
                    return false;
                }
            }

            if (y - size >= 0) {
                Node node = root.FindSizeNode(x, y - size, size);
                if (node.childs != null && (node.childs[2].childs != null || node.childs[3].childs != null)) {
                    return false;
                }
            }
            if (y + size + size < root.size) {
                Node node = root.FindSizeNode(x, y + size, size);
                if (node.childs != null && (node.childs[0].childs != null || node.childs[1].childs != null)) {
                    return false;
                }
            }
            
            return true;
        }

        //2 3
        //0 1
        void AddNode(Node root)
        {
            root.childs = new Node[4];
            int half = root.size / 2;
            root.childs[0] = new Node(root.x, root.y, half);
            root.childs[1] = new Node(root.x + half, root.y, half);
            root.childs[2] = new Node(root.x, root.y + half, half);
            root.childs[3] = new Node(root.x + half, root.y + half, half);
            if (half > 1) {
                for (int i = 0; i < 4; i++) {
                    AddNode(root.childs[i]);
                }
            }
        }

        public bool TestAlphaMap(float[,,] alphamaps, int x, int y, int size, int layer)
        {
            if (size == 0) {
                size = 1;
            }
            for (int i = 0; i < size; i++) {
                for (int j = 0; j < size; j++) {
                    if (alphamaps[x + i, y + j, layer] > converter.alphaMapThreshold) {
                        return true;
                    }
                }
            }
            return false;
        }

        Node CreateNode(float maxError)
        {
            int w = converter.terrain.terrainData.heightmapWidth;
            int h = converter.terrain.terrainData.heightmapHeight;
            float[,] heights = converter.terrain.terrainData.GetHeights(0, 0, w, h);
            int aw = converter.terrain.terrainData.alphamapWidth;
            int ah = converter.terrain.terrainData.alphamapHeight;
            float[,,] alphamaps = converter.terrain.terrainData.GetAlphamaps(0, 0, aw, ah);

            Node root = new Node(0, 0, w);
            AddNode(root);

            //统计不透明的格子数量
            root.PostorderTraversal((Node node) => {
                node.validNums = new int[converter.terrain.terrainData.alphamapLayers];
                for (int alphaLayer = 0; alphaLayer < node.validNums.Length; alphaLayer++) {
                    if (node.size == 1) {
                        node.validNums[alphaLayer] = TestAlphaMap(alphamaps, node.x * aw / (w - 1), node.y * ah / (h - 1), aw / (w - 1), alphaLayer) ? 1 : 0;
                    } else {
                        node.validNums[alphaLayer] = node.childs[0].validNums[alphaLayer] + node.childs[1].validNums[alphaLayer] + node.childs[2].validNums[alphaLayer] + node.childs[3].validNums[alphaLayer];
                    }
                }
            });

            //合并格子
            for (int m = 1; 1 << m < w; m++) {
                int step = 1 << m;
                root.TraversalSize(step, (Node node) => {
                    bool allChildrenIsMerged = node.childs != null && node.childs[0].childs == null && node.childs[1].childs == null && node.childs[2].childs == null && node.childs[3].childs == null;
                    if (allChildrenIsMerged) {
                        if (GetHeightError(heights, node.x, node.y, node.size, out node.swapEdge) * converter.terrain.terrainData.size.y < maxError && CheckSourrond(root, node.x, node.y, node.size)) {
                            node.childs = null;
                        }
                    }
                });
            }

            //为了消除T接缝，如果相邻格子比自己大，则靠近大格子的两个三角形要合并为一个
            root.PreorderTraversal((Node node) => {
                if (node.childs != null) {
                    //x - 1
                    if (node.childs[0].childs == null && node.childs[2].childs == null && TerrainToMeshTool.IsSizeLeaf(root, node.x - 1, node.y, node.size)) {
                        node.mergeTriangle |= 1 << 1;
                    }
                    //y - 1
                    if (node.childs[0].childs == null && node.childs[1].childs == null && TerrainToMeshTool.IsSizeLeaf(root, node.x, node.y - 1, node.size)) {
                        node.mergeTriangle |= 1 << 0;
                    }

                    //x + 1
                    if (node.childs[1].childs == null && node.childs[3].childs == null && TerrainToMeshTool.IsSizeLeaf(root, node.x + node.size + 1, node.y, node.size)) {
                        node.mergeTriangle |= 1 << 3;
                    }
                    //y + 1
                    if (node.childs[2].childs == null && node.childs[3].childs == null && TerrainToMeshTool.IsSizeLeaf(root, node.x, node.y + node.size + 1, node.size)) {
                        node.mergeTriangle |= 1 << 2;
                    }
                }
            });

            return root;
        }

    }
}