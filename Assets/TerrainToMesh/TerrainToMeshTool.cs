using System.Collections;
using System.Collections.Generic;
using UnityEngine;


namespace TerrainConverter
{
    public class Node
    {
        public Node(int x, int y, int size)
        {
            this.x = x; this.y = y; this.size = size;
        }
        public int x, y;
        public int size;
        public int GetMaxValidNumIndex()
        {
            int maxNums = 0;
            int maxIndex = 0;
            for(int i =0; i < validNums.Length; i++) {
                if(validNums[i] > maxNums) {
                    maxNums = validNums[i];
                    maxIndex = i;
                }
            }
            return maxIndex;
        }
        public int GetValidNum(int layer)
        {
            if (layer == -1 || validNums == null) {
                return size * size;
            } else {
                return validNums[layer];
            }
        }
        public int[] validNums;
        public bool swapEdge;
        public Node[] childs = null;

        //记录那些边要被擦掉
        //+-2-+
        //1 + 3
        //+-0-+
        public byte mergeTriangle;

        //2 6 3
        //5 8 7
        //0 4 1
        public int[] vertexIndices = new int[9];

        public void PostorderTraversal(System.Action<Node> fun)
        {
            if (childs != null) {
                for (int i = 0; i < 4; i++) {
                    childs[i].PostorderTraversal(fun);
                }
            }
            fun(this);
        }
        public void PreorderTraversal(System.Action<Node> fun)
        {
            fun(this);
            if (childs != null) {
                for (int i = 0; i < 4; i++) {
                    childs[i].PreorderTraversal(fun);
                }
            }
        }
        public void TraversalSize(int fsize, System.Action<Node> fun)
        {
            if (size > fsize) {
                if (childs != null) {
                    for (int i = 0; i < 4; i++) {
                        childs[i].TraversalSize(fsize, fun);
                    }
                }
            } else {
                fun(this);
            }
        }
        public Node FindNode(int fx, int fy)
        {
            if (childs == null) {
                return this;
            } else {
                int index = (fy < y + size / 2.0f ? 0 : 2) + (fx < x + size / 2.0f ? 0 : 1);
                return childs[index].FindNode(fx, fy);
            }
        }
        public Node FindSizeNode(int fx, int fy, int fsize)
        {
            if (fsize == size) {
                return this;
            } else if (fsize < size) {
                int index = (fy < y + size / 2 ? 0 : 2) + (fx < x + size / 2 ? 0 : 1);
                if(childs== null || childs[index] == null) {
                    int a = 0;
                }
                return childs[index].FindSizeNode(fx, fy, fsize);
            }
            return null;
        }


        public void ToBytes(List<byte> bytes)
        {
            PreorderTraversal((Node node) => {
                if (node.childs != null) {
                    byte flag = 0;
                    for (int i = 0; i < 4; i++) {
                        flag |= node.childs[i].childs != null ? (byte)(1 << i) : (byte)0;
                    }
                    flag |= (byte)(node.mergeTriangle << 4);
                    bytes.Add(flag);
                }
            });
        }

        public List<byte>[] GetAlphaBytes()
        {
            List<byte>[] result = new List<byte>[validNums.Length];
            for (int layer = 0; layer < validNums.Length; layer++) {
                result[layer] = new List<byte>();
                List<byte> bytes = result[layer];
                PreorderTraversal((Node node) => {
                    if (node.childs != null) {
                        byte flag = 0;
                        for (int i = 0; i < 4; i++) {
                            if (node.childs[i].childs == null) {
                                if (node.childs[i].GetValidNum(layer) > 0) {
                                    flag |= (byte)(1 << i);
                                }
                            }
                        }
                        bytes.Add(flag);
                    }
                });
            }
            return result;
        }

        public void CreateChildFromBytes(List<byte> bytes, ref int startIndex)
        {
            //只要调用到这个函数，说明node肯定包含有四个子节点
            childs = new Node[4];
            int half = size / 2;
            childs[0] = new Node(x, y, half);
            childs[1] = new Node(x + half, y, half);
            childs[2] = new Node(x, y + half, half);
            childs[3] = new Node(x + half, y + half, half);

            Node node = new Node(x, y, size);
            byte flag = bytes[startIndex];
            mergeTriangle = (byte)(flag >> 4);
            startIndex++;
            for (int i = 0; i < 4; i++) {
                if ((flag & (byte)(1 << i)) != 0) {
                    childs[i].CreateChildFromBytes(bytes, ref startIndex);
                }
            }
        }

        public void CreateChildFromBytes(List<byte> bytes)
        {
            int startInde = 0;
            CreateChildFromBytes(bytes, ref startInde);
        }

        public void SetAlphaBytes(List<byte>[] layerBytes)
        {
            PreorderTraversal((Node node) => {
                node.validNums = new int[layerBytes.Length];
            });

            for (int layer = 0; layer < layerBytes.Length; layer++) {
                List<byte> bytes = layerBytes[layer];
                int index = 0;
                PreorderTraversal((Node node) => {
                    if (node.childs != null) {
                        byte flag = bytes[index];
                        index++;
                        for (int i = 0; i < 4; i++) {
                            node.childs[i].validNums[layer] += ((flag & (1 << i)) != 0 ? 1 : 0);
                        }
                    }
                });

                PostorderTraversal((Node node) => {
                    if (node.childs != null) {
                        node.validNums[layer] = node.childs[0].validNums[layer] + node.childs[1].validNums[layer] + node.childs[2].validNums[layer] + node.childs[3].validNums[layer];
                    }
                });
            }
        }
    }


    public class MeshInfo
    {
        public MeshInfo(Mesh mesh, int validNum, int gridX, int gridY, int layer)
        {
            this.mesh = mesh; this.validNum = validNum; this.gridX = gridX; this.gridY = gridY; this.layer = layer;
        }
        public Mesh mesh;
        public int validNum;
        public int gridX;
        public int gridY;
        public int layer;
    }

    public static class TerrainToMeshTool
    {
        public static Material GetMaterial(TerrainData td,int layer, bool bFirst)
        {
            Material mat;
            if (layer != -1) {
                mat = bFirst ? new Material(Shader.Find("Unlit/TerrainFirst")) : new Material(Shader.Find("Unlit/TerrainAdd"));
                mat.SetTexture("_MainTex", td.splatPrototypes[layer].texture);
                Vector2 tileSize = td.splatPrototypes[layer].tileSize;
                mat.mainTextureScale = new Vector2(td.size.x / tileSize.x, td.size.z / tileSize.y);
                mat.mainTextureOffset = td.splatPrototypes[layer].tileOffset;
                mat.SetTexture("_SplatAlpha", td.alphamapTextures[layer / 4]);
                mat.SetFloat("_SplatIndex", layer % 4);
            } else {
                mat = new Material(Shader.Find("Diffuse"));
            }
            return mat;
        }

        public static Texture2D BakeBaseTexture(TerrainData terrainData)
        {
            Material mat = new Material(Shader.Find("Hidden/BakeBaseTexture"));
            int baseMapResolution = terrainData.baseMapResolution;
            RenderTexture rt = RenderTexture.GetTemporary(baseMapResolution, baseMapResolution);
            RenderTexture.active = rt;
            Graphics.Blit(null, mat, 0);
            for (int layer = 0; layer < terrainData.alphamapLayers; layer++) {
                mat.SetTexture("_MainTex", terrainData.splatPrototypes[layer].texture);
                Vector2 tileSize = terrainData.splatPrototypes[layer].tileSize;
                mat.mainTextureScale = new Vector2(terrainData.size.x / tileSize.x, terrainData.size.z / tileSize.y);
                mat.mainTextureOffset = terrainData.splatPrototypes[layer].tileOffset;
                mat.SetVector("_MainTexST", new Vector4(mat.mainTextureScale.x, mat.mainTextureScale.y, mat.mainTextureOffset.x, mat.mainTextureOffset.y));
                mat.SetTexture("_SplatAlpha", terrainData.alphamapTextures[layer / 4]);
                mat.SetFloat("_SplatIndex", layer % 4);
                Graphics.Blit(mat.mainTexture, mat, 1);
            }
            Texture2D result = new Texture2D(rt.width, rt.height, TextureFormat.RGB24, true);
            result.ReadPixels(new Rect(0, 0, rt.width, rt.height), 0, 0);
            result.Apply();
            RenderTexture.ReleaseTemporary(rt);
            return result;
        }

        public static void CreateObjects(TerrainData terrainData,Transform transform,List<MeshInfo>[,] meshes)
        {
            Material matBase = new Material(Shader.Find("Diffuse"));
            matBase.mainTexture = BakeBaseTexture(terrainData);
            List<Material> matAdd = new List<Material>();
            List<Material> matFirst = new List<Material>();
            for (int l = 0; l < terrainData.alphamapLayers; l++) {
                matAdd.Add(GetMaterial(terrainData,l, false));
                matFirst.Add(GetMaterial(terrainData,l, true));
            }

            for (int i = 0; i < meshes.GetLength(0); i++) {
                for (int j = 0; j < meshes.GetLength(1); j++) {
                    Mesh fullMesh = null;
                    int maxValidLayer = 0;
                    int maxValidNum = 0;
                    List<MeshInfo> group = meshes[i, j];
                    group.Sort((MeshInfo m0, MeshInfo m1) => { return m1.validNum.CompareTo(m0.validNum); });
                    foreach (var m in group) {
                        if (m.layer == -1) {
                            fullMesh = m.mesh;
                        } else {
                            if (m.validNum > maxValidNum) {
                                maxValidNum = m.validNum;
                                maxValidLayer = m.layer;
                            }
                        }
                    }

                    if (group.Count > 0) {
                        GameObject obj = new GameObject("mesh_" + group[0].gridX.ToString() + "_" + group[0].gridY.ToString());
                        obj.transform.SetParent(transform);
                        Renderer[] baseRenderer = new Renderer[1];
                        GameObject baseObj = new GameObject("base");
                        {
                            MeshRenderer renderer = baseObj.AddComponent<MeshRenderer>();
                            MeshFilter filter = baseObj.AddComponent<MeshFilter>();
                            filter.sharedMesh = fullMesh;
                            renderer.sharedMaterial = matBase;
                            baseObj.transform.SetParent(obj.transform);
                            baseRenderer[0] = renderer;
                        }

                        List<Renderer> splatRenderers = new List<Renderer>();
                        foreach (var m in group) {
                            if (m.layer != -1) {
                                GameObject subObj = new GameObject("mesh_" + m.gridX.ToString() + "_" + m.gridY.ToString() + "_" + m.layer.ToString());
                                MeshRenderer renderer = subObj.AddComponent<MeshRenderer>();
                                MeshFilter filter = subObj.AddComponent<MeshFilter>();
                                filter.sharedMesh = m.layer == maxValidLayer ? fullMesh : m.mesh;
                                renderer.sharedMaterial = m.layer == maxValidLayer ? matFirst[m.layer] : matAdd[m.layer];
                                subObj.transform.SetParent(obj.transform);
                                splatRenderers.Add(renderer);
                            }
                        }
                        LOD[] lods = new LOD[2];
                        lods[0].renderers = splatRenderers.ToArray();
                        lods[0].screenRelativeTransitionHeight = 0.4f;
                        lods[1].renderers = baseRenderer;
                        lods[1].screenRelativeTransitionHeight = 0f;
                        LODGroup lodg = obj.AddComponent<LODGroup>();
                        lodg.SetLODs(lods);
                    }
                }
            }

        }

        struct VecInt2
        {
            public VecInt2(int x, int y) { this.x = x; this.y = y; }
            public int x, y;
        }

        //2 6 3
        //5 8 7
        //0 4 1
        static void AddVertices(Node node, int i00, int i10, int i01, int i11, List<VecInt2> vertices)
        {
            node.vertexIndices[0] = i00;
            node.vertexIndices[1] = i10;
            node.vertexIndices[2] = i01;
            node.vertexIndices[3] = i11;
            if (node.childs != null) {
                node.vertexIndices[4] = vertices.Count + 0;
                node.vertexIndices[5] = vertices.Count + 1;
                node.vertexIndices[6] = vertices.Count + 2;
                node.vertexIndices[7] = vertices.Count + 3;
                node.vertexIndices[8] = vertices.Count + 4;
                vertices.Add(new VecInt2(node.x + node.size / 2, node.y));
                vertices.Add(new VecInt2(node.x, node.y + node.size / 2));
                vertices.Add(new VecInt2(node.x + node.size / 2, node.y + node.size));
                vertices.Add(new VecInt2(node.x + node.size, node.y + node.size / 2));
                vertices.Add(new VecInt2(node.x + node.size / 2, node.y + node.size / 2));
                AddVertices(node.childs[0], node.vertexIndices[0], node.vertexIndices[4], node.vertexIndices[5], node.vertexIndices[8], vertices);
                AddVertices(node.childs[1], node.vertexIndices[4], node.vertexIndices[1], node.vertexIndices[8], node.vertexIndices[7], vertices);
                AddVertices(node.childs[2], node.vertexIndices[5], node.vertexIndices[8], node.vertexIndices[2], node.vertexIndices[6], vertices);
                AddVertices(node.childs[3], node.vertexIndices[8], node.vertexIndices[7], node.vertexIndices[6], node.vertexIndices[3], vertices);
            }
        }

        public static bool IsSizeLeaf(Node root, int x, int y, int size)
        {
            Node node = root.FindSizeNode(x, y, size);
            if (node != null && node.childs == null) {
                return true;
            }
            return false;
        }

        static void AddIndices(List<int> indices, int[] nodeIndices, int a0, int a1, int a2)
        {
            indices.Add(nodeIndices[a0]);
            indices.Add(nodeIndices[a1]);
            indices.Add(nodeIndices[a2]);
        }

        static void AddIndices(List<int>indices,Node subRoot, int alphaLayer)
        {
            //2 6 3
            //5 8 7
            //0 4 1
            subRoot.PreorderTraversal((Node node) => {
                if (node.childs != null) {
                    //x - 1
                    if ((node.mergeTriangle & (1 << 1)) != 0 && (node.childs[0].GetValidNum(alphaLayer) > 0 || node.childs[2].GetValidNum(alphaLayer) > 0)) {
                        AddIndices(indices, node.vertexIndices, 0, 8, 2);
                    } else {
                        if (node.childs[0].childs == null && node.childs[0].GetValidNum(alphaLayer) > 0) {
                            AddIndices(indices, node.vertexIndices, 0, 8, 5);
                        }
                        if (node.childs[2].childs == null && node.childs[2].GetValidNum(alphaLayer) > 0) {
                            AddIndices(indices, node.vertexIndices, 5, 8, 2);
                        }
                    }

                    //y - 1
                    if ((node.mergeTriangle & (1 << 0)) != 0 && (node.childs[0].GetValidNum(alphaLayer) > 0 || node.childs[1].GetValidNum(alphaLayer) > 0)) {
                        AddIndices(indices, node.vertexIndices, 0, 1, 8);
                    } else {
                        if (node.childs[0].childs == null && node.childs[0].GetValidNum(alphaLayer) > 0) {
                            AddIndices(indices, node.vertexIndices, 0, 4, 8);
                        }
                        if (node.childs[1].childs == null && node.childs[1].GetValidNum(alphaLayer) > 0) {
                            AddIndices(indices, node.vertexIndices, 4, 1, 8);
                        }
                    }

                    //x + 1
                    if ((node.mergeTriangle & (1 << 3)) != 0 && (node.childs[1].GetValidNum(alphaLayer) > 0 || node.childs[3].GetValidNum(alphaLayer) > 0)) {
                        AddIndices(indices, node.vertexIndices, 1, 3, 8);
                    } else {
                        if (node.childs[1].childs == null && node.childs[1].GetValidNum(alphaLayer) > 0) {
                            AddIndices(indices, node.vertexIndices, 1, 7, 8);
                        }
                        if (node.childs[3].childs == null && node.childs[3].GetValidNum(alphaLayer) > 0) {
                            AddIndices(indices, node.vertexIndices, 7, 3, 8);
                        }
                    }
                    //y + 1
                    if ((node.mergeTriangle & (1 << 2)) != 0 && (node.childs[2].GetValidNum(alphaLayer) > 0 || node.childs[3].GetValidNum(alphaLayer) > 0)) {
                        AddIndices(indices, node.vertexIndices, 3, 2, 8);
                    } else {
                        if (node.childs[2].childs == null && node.childs[2].GetValidNum(alphaLayer) > 0) {
                            AddIndices(indices, node.vertexIndices, 8, 6, 2);
                        }
                        if (node.childs[3].childs == null && node.childs[3].GetValidNum(alphaLayer) > 0) {
                            AddIndices(indices, node.vertexIndices, 8, 3, 6);
                        }
                    }
                }
            });
        }

        public static Mesh CreateMesh(Node root,TerrainData terrainData,int[]layerIndex)
        {
            int w = terrainData.heightmapWidth;
            int h = terrainData.heightmapHeight;
            float[,] heights = terrainData.GetHeights(0, 0, w, h);
            int aw = terrainData.alphamapWidth;
            int ah = terrainData.alphamapHeight;
            float[,,] alphamaps = terrainData.GetAlphamaps(0, 0, aw, ah);

            Node subRoot = root;

            List<Vector3> vertices = new List<Vector3>();
            List<int> indices = new List<int>();
            List<Vector3> normals = new List<Vector3>();
            List<Vector2> uvs = new List<Vector2>();
            Vector3 size = terrainData.size;
            List<VecInt2> intVertices = new List<VecInt2>();

            intVertices.Add(new VecInt2(subRoot.x, subRoot.y));
            intVertices.Add(new VecInt2(subRoot.x + subRoot.size, subRoot.y));
            intVertices.Add(new VecInt2(subRoot.x, subRoot.y + subRoot.size));
            intVertices.Add(new VecInt2(subRoot.x + subRoot.size, subRoot.y + subRoot.size));
            AddVertices(subRoot, 0, 1, 2, 3, intVertices);

            for (int i = 0; i < intVertices.Count; i++) {
                float y0 = (intVertices[i].x) / (w - 1.0f);
                float x0 = (intVertices[i].y) / (h - 1.0f);
                vertices.Add(Vector3.Scale(new Vector3(x0, heights[intVertices[i].x, intVertices[i].y], y0), size));
                normals.Add(terrainData.GetInterpolatedNormal(x0, y0));
                uvs.Add(new Vector2(x0, y0));
            }


            Mesh mesh = new Mesh();
            mesh.vertices = vertices.ToArray();
            mesh.normals = normals.ToArray();
            mesh.uv = uvs.ToArray();
            mesh.subMeshCount = layerIndex.Length;
            for (int i = 0; i < layerIndex.Length; i++) {
                indices.Clear();
                AddIndices(indices, subRoot, i == 0 ? -1 : layerIndex[i]);
                mesh.SetTriangles(indices.ToArray(), i);
            }
            return mesh;
        }

        public static List<MeshInfo> CreateMeshes(Node root , TerrainData terrainData, int gridSize)
        {
            List<MeshInfo> meshes = new List<MeshInfo>();
            root.TraversalSize(gridSize, (Node _subRoot) => {
                Node subRoot = _subRoot;
                Mesh mesh = CreateMesh(subRoot, terrainData,new int[] { 0 });
                if (mesh != null) {
                    meshes.Add(new MeshInfo(mesh,subRoot.GetValidNum(0),subRoot.x / subRoot.size,subRoot.y/subRoot.size,0));
                }
            });

            return meshes;
        }

    }
}