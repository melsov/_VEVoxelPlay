using System;
using System.IO;
using System.Collections;
using System.Collections.Generic;

using UnityEngine;
using UnityEditor;
using VoxelPlay;

namespace VE.Importers
{

    public class VEMagicaVoxelImportTools : EditorWindow
    {

        enum IMPORT_FORMAT
        {
            MagicaVoxel,
        }

        static string ExtensionForImportFormat(IMPORT_FORMAT format)
        {
            switch (format)
            {
                case IMPORT_FORMAT.MagicaVoxel:
                default:
                    return "vox";
            }
        }

        struct Cuboid
        {
            public Bounds bounds;
            public Color32 color;
        }

        struct Face
        {
            public Vector3 center;
            public Vector3 size;
            public Vector3[] vertices;
            public Vector3[] normals;
            public Color32 color;

            public Face(Vector3 center, Vector3 size, Vector3[] vertices, Vector3[] normals, Color32 color)
            {
                this.center = center;
                this.size = size;
                this.vertices = vertices;
                this.normals = normals;
                this.color = color;
            }


            public static bool operator ==(Face f1, Face f2)
            {
                return f1.size == f2.size && f1.center == f2.center;
            }

            public static bool operator !=(Face f1, Face f2)
            {
                return f1.size != f2.size || f1.center != f2.center;
            }

            public override bool Equals(object obj)
            {
                if (obj == null || !(obj is Face))
                    return false;
                Face other = (Face)obj;
                return size == other.size && center == other.center;
            }

            public override int GetHashCode()
            {
                int hash = 23;
                hash = hash * 31 + center.GetHashCode();
                hash = hash * 31 + size.GetHashCode();
                return hash;
            }
        }

        static Vector3[] faceVerticesForward = new Vector3[] {
            new Vector3 (0.5f, -0.5f, 0.5f),
            new Vector3 (0.5f, 0.5f, 0.5f),
            new Vector3 (-0.5f, -0.5f, 0.5f),
            new Vector3 (-0.5f, 0.5f, 0.5f)
        };
        static Vector3[] faceVerticesBack = new Vector3[] {
            new Vector3 (-0.5f, -0.5f, -0.5f),
            new Vector3 (-0.5f, 0.5f, -0.5f),
            new Vector3 (0.5f, -0.5f, -0.5f),
            new Vector3 (0.5f, 0.5f, -0.5f)
        };
        static Vector3[] faceVerticesLeft = new Vector3[] {
            new Vector3 (-0.5f, -0.5f, 0.5f),
            new Vector3 (-0.5f, 0.5f, 0.5f),
            new Vector3 (-0.5f, -0.5f, -0.5f),
            new Vector3 (-0.5f, 0.5f, -0.5f)
        };
        static Vector3[] faceVerticesRight = new Vector3[] {
            new Vector3 (0.5f, -0.5f, -0.5f),
            new Vector3 (0.5f, 0.5f, -0.5f),
            new Vector3 (0.5f, -0.5f, 0.5f),
            new Vector3 (0.5f, 0.5f, 0.5f)
        };
        static Vector3[] faceVerticesTop = new Vector3[] {
            new Vector3 (-0.5f, 0.5f, 0.5f),
            new Vector3 (0.5f, 0.5f, 0.5f),
            new Vector3 (-0.5f, 0.5f, -0.5f),
            new Vector3 (0.5f, 0.5f, -0.5f)
        };
        static Vector3[] faceVerticesBottom = new Vector3[] {
            new Vector3 (-0.5f, -0.5f, -0.5f),
            new Vector3 (0.5f, -0.5f, -0.5f),
            new Vector3 (-0.5f, -0.5f, 0.5f),
            new Vector3 (0.5f, -0.5f, 0.5f)
        };
        static Vector3[] normalsBack = new Vector3[] {
            Misc.vector3back, Misc.vector3back, Misc.vector3back, Misc.vector3back
        };
        static Vector3[] normalsForward = new Vector3[] {
            Misc.vector3forward, Misc.vector3forward, Misc.vector3forward, Misc.vector3forward
        };
        static Vector3[] normalsLeft = new Vector3[] {
            Misc.vector3left, Misc.vector3left, Misc.vector3left, Misc.vector3left
        };
        static Vector3[] normalsRight = new Vector3[] {
            Misc.vector3right, Misc.vector3right, Misc.vector3right, Misc.vector3right
        };
        static Vector3[] normalsUp = new Vector3[] {
            Misc.vector3up, Misc.vector3up, Misc.vector3up, Misc.vector3up
        };
        static Vector3[] normalsDown = new Vector3[] {
            Misc.vector3down, Misc.vector3down, Misc.vector3down, Misc.vector3down
        };



        // Model import tools
        IMPORT_FORMAT importFormat;
        bool importIgnoreOffset = true;
        string importFilename;
        Vector3 scale = Misc.vector3one;
        private bool shouldMakeColorToVoxMap;
        private VoxelDefinition defaultMapVoxel;

        [MenuItem("Assets/Create/Voxel Engines/Import Magica Tools...", false, 1000)]
        public static void ShowWindow()
        {
            VEMagicaVoxelImportTools window = GetWindow<VEMagicaVoxelImportTools>("Import Tools", true);
            window.minSize = new Vector2(300, 120);
            window.Show();
        }

        void OnGUI()
        {
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.HelpBox("Import voxel models from other applications.", MessageType.Info);
            EditorGUILayout.EndHorizontal();
            EditorGUILayout.Separator();

            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("Format", GUILayout.Width(120));
            importFormat = (IMPORT_FORMAT)EditorGUILayout.EnumPopup(importFormat);
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("File name", GUILayout.Width(120));
            importFilename = EditorGUILayout.TextField(importFilename);
            if (GUILayout.Button("Open...", GUILayout.Width(80)))
            {
                string ext = ExtensionForImportFormat(importFormat);
                importFilename = EditorUtility.OpenFilePanel("Select model File (*." + ext + ")", "", ext);
            }
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField(new GUIContent("Ignore Offset", "Model can specify an offset for the center."), GUILayout.Width(120));
            importIgnoreOffset = EditorGUILayout.Toggle(importIgnoreOffset);
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField(new GUIContent("Scale", "Scale applied to the model."), GUILayout.Width(120));
            scale = EditorGUILayout.Vector3Field("", scale);
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField(new GUIContent("Color To Voxel Map", "Additionally create a color to voxel definition map."), GUILayout.Width(160));
            shouldMakeColorToVoxMap = EditorGUILayout.Toggle(shouldMakeColorToVoxMap);
            if (shouldMakeColorToVoxMap)
            {
                defaultMapVoxel = (VoxelDefinition)EditorGUILayout.ObjectField("Default Voxel Def", defaultMapVoxel, typeof(VoxelDefinition), true);
            }
            EditorGUILayout.EndHorizontal();


            EditorGUILayout.Separator();
            GUI.enabled = !string.IsNullOrEmpty(importFilename);
            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("Generate Model Asset", GUILayout.Width(160)))
            {
                GenerateModelAsset();
                GUIUtility.ExitGUI();
            }
            if (GUILayout.Button("Generate Prefab", GUILayout.Width(160)))
            {
                GeneratePrefab();
                GUIUtility.ExitGUI();
            }
            GUI.enabled = false;
            EditorGUILayout.EndHorizontal();
        }


        void GenerateModelAsset()
        {
            ColorBasedModelDefinition baseModel = BinaryToColorBasedModelDefinition();
            if (baseModel.colors == null)
                return;

            ModelDefinition newModel = VoxelPlayFormatTools.GetModelDefinition(null, baseModel, importIgnoreOffset);
            if (!string.IsNullOrEmpty(baseModel.name))
            {
                newModel.name = baseModel.name;
            }

            newModel.name = Path.GetFileNameWithoutExtension(importFilename);

            // Create a suitable file path
            string path = GetPathForNewModel();
            string modelName = GetFilenameForNewModel(newModel.name);
            AssetDatabase.CreateAsset(newModel, path + "/" +  modelName + ".asset");

            if (shouldMakeColorToVoxMap)
            {
                var map = CreateColorToVoxDefMap(newModel);
                AssetDatabase.CreateAsset(map, path + "/" + modelName + ".CTVMAP.asset");
            }

            AssetDatabase.SaveAssets();
            EditorUtility.FocusProjectWindow();
            Selection.activeObject = newModel;
        }

        VEColor32ToVoxelDefinitionMap CreateColorToVoxDefMap(ModelDefinition modelDef)
        {
            var colors = new HashSet<Color32>();
            for(int i=0; i < modelDef.bits.Length; ++i)
            {
                colors.Add(modelDef.bits[i].color);
            }

            var pairs = new List<ColorToVoxDef>(colors.Count);
            foreach(var c in colors)
            {
                var pair = new ColorToVoxDef
                {
                    color = c,
                    voxDef = defaultMapVoxel
                };
                pairs.Add(pair);
            }

            var result = ScriptableObject.CreateInstance<VEColor32ToVoxelDefinitionMap>();
            result.Map = pairs.ToArray();
            return result;

        }


        void GeneratePrefab()
        {
            ColorBasedModelDefinition baseModel = BinaryToColorBasedModelDefinition();
            if (baseModel.colors == null)
                return;

            // Generate a cuboid per visible voxel
            int index;
            int sizeX = baseModel.sizeX;
            int sizeY = baseModel.sizeY;
            int sizeZ = baseModel.sizeZ;
            int ONE_Y_ROW = sizeX * sizeZ;
            int ONE_Z_ROW = sizeX;
            float offsetX = 0, offsetY = 0, offsetZ = 0;
            if (!importIgnoreOffset)
            {
                offsetX += baseModel.offsetX;
                offsetY += baseModel.offsetY;
                offsetZ += baseModel.offsetZ;
            }
            Color32[] colors = baseModel.colors;
            List<Cuboid> cuboidsPick = new List<Cuboid>();
            for (int y = 0; y < sizeY; y++)
            {
                int posy = y * ONE_Y_ROW;
                for (int z = 0; z < sizeZ; z++)
                {
                    int posz = z * ONE_Z_ROW;
                    for (int x = 0; x < sizeX; x++)
                    {
                        index = posy + posz + x;
                        Color32 color = colors[index];
                        if (color.a > 0)
                        {
                            Cuboid cuboid = new Cuboid();
                            Vector3 center = new Vector3(x - sizeX / 2 - 0.5f + offsetX, y + 0.5f + offsetY, z - sizeZ / 2 - 0.5f + offsetZ);
                            cuboid.bounds = new Bounds(center, Misc.vector3one);
                            cuboid.color = color;
                            cuboidsPick.Add(cuboid);
                        }
                    }
                }
            }
            Cuboid[] cuboids = cuboidsPick.ToArray();

            // Optimization 1: Fusion same color cuboids
            bool repeat = true;
            while (repeat)
            {
                repeat = false;
                for (int k = 0; k < cuboids.Length; k++)
                {
                    if (cuboids[k].color.a == 0)
                        continue;
                    for (int j = k + 1; j < cuboids.Length; j++)
                    {
                        if (cuboids[j].color.a == 0)
                            continue;
                        if (cuboids[k].color.r == cuboids[j].color.r && cuboids[k].color.b == cuboids[j].color.b && cuboids[k].color.g == cuboids[j].color.g)
                        {
                            bool touching = false;
                            Bounds f1 = cuboids[k].bounds;
                            Bounds f2 = cuboids[j].bounds;
                            // Touching back or forward faces?
                            if (f1.min.x == f2.min.x && f1.max.x == f2.max.x && f1.min.y == f2.min.y && f1.max.y == f2.max.y)
                            {
                                touching = f1.min.z == f2.max.z || f1.max.z == f2.min.z;
                                // ... left or right faces?
                            }
                            else if (f1.min.z == f2.min.z && f1.max.z == f2.max.z && f1.min.y == f2.min.y && f1.max.y == f2.max.y)
                            {
                                touching = f1.min.x == f2.max.x || f1.max.x == f2.min.x;
                                // ... top or bottom faces?
                            }
                            else if (f1.min.x == f2.min.x && f1.max.x == f2.max.x && f1.min.z == f2.min.z && f1.max.z == f2.max.z)
                            {
                                touching = f1.min.y == f2.max.y || f1.max.y == f2.min.y;
                            }
                            if (touching)
                            {
                                cuboids[k].bounds.Encapsulate(cuboids[j].bounds);
                                cuboids[j].color.a = 0; // mark as deleted
                                repeat = true;
                            }
                        }
                    }
                }
            }

            // Optimization 2: Remove hidden cuboids
            for (int k = 0; k < cuboids.Length; k++)
            {
                if (cuboids[k].color.a == 0)
                    continue;
                for (int j = k + 1; j < cuboids.Length; j++)
                {
                    if (cuboids[j].color.a == 0)
                        continue;
                    int occlusion = 0;
                    Bounds f1 = cuboids[k].bounds;
                    Bounds f2 = cuboids[j].bounds;
                    // Touching back or forward faces?
                    if (f1.min.x >= f2.min.x && f1.max.x <= f2.max.x && f1.min.y >= f2.min.y && f1.max.y <= f2.max.y)
                    {
                        if (f1.min.z == f2.max.z)
                            occlusion++;
                        if (f1.max.z == f2.min.z)
                            occlusion++;
                        // ... left or right faces?
                    }
                    else if (f1.min.z >= f2.min.z && f1.max.z <= f2.max.z && f1.min.y >= f2.min.y && f1.max.y <= f2.max.y)
                    {
                        if (f1.min.x == f2.max.x)
                            occlusion++;
                        if (f1.max.x == f2.min.x)
                            occlusion++;
                        // ... top or bottom faces?
                    }
                    else if (f1.min.x >= f2.min.x && f1.max.x <= f2.max.x && f1.min.z >= f2.min.z && f1.max.z <= f2.max.z)
                    {
                        if (f1.min.y == f2.max.y)
                            occlusion++;
                        if (f1.max.y == f2.min.y)
                            occlusion++;
                    }
                    if (occlusion == 6)
                    {
                        cuboids[k].color.a = 0;
                        break;
                    }
                }
            }

            // Optimization 3: Fragment cuboids into faces and remove duplicates
            List<Face> faces = new List<Face>();
            for (int k = 0; k < cuboids.Length; k++)
            {
                if (cuboids[k].color.a == 0)
                    continue;
                Vector3 min = cuboids[k].bounds.min;
                Vector3 max = cuboids[k].bounds.max;
                Vector3 size = cuboids[k].bounds.size;
                Face top = new Face(new Vector3((min.x + max.x) * 0.5f, max.y, (min.z + max.z) * 0.5f), new Vector3(size.x, 0, size.z), faceVerticesTop, normalsUp, cuboids[k].color);
                RemoveDuplicateOrAddFace(faces, top);
                Face bottom = new Face(new Vector3((min.x + max.x) * 0.5f, min.y, (min.z + max.z) * 0.5f), new Vector3(size.x, 0, size.z), faceVerticesBottom, normalsDown, cuboids[k].color);
                RemoveDuplicateOrAddFace(faces, bottom);
                Face left = new Face(new Vector3(min.x, (min.y + max.y) * 0.5f, (min.z + max.z) * 0.5f), new Vector3(0, size.y, size.z), faceVerticesLeft, normalsLeft, cuboids[k].color);
                RemoveDuplicateOrAddFace(faces, left);
                Face right = new Face(new Vector3(max.x, (min.y + max.y) * 0.5f, (min.z + max.z) * 0.5f), new Vector3(0, size.y, size.z), faceVerticesRight, normalsRight, cuboids[k].color);
                RemoveDuplicateOrAddFace(faces, right);
                Face back = new Face(new Vector3((min.x + max.x) * 0.5f, (min.y + max.y) * 0.5f, min.z), new Vector3(size.x, size.y, 0), faceVerticesBack, normalsBack, cuboids[k].color);
                RemoveDuplicateOrAddFace(faces, back);
                Face forward = new Face(new Vector3((min.x + max.x) * 0.5f, (min.y + max.y) * 0.5f, max.z), new Vector3(size.x, size.y, 0), faceVerticesForward, normalsForward, cuboids[k].color);
                RemoveDuplicateOrAddFace(faces, forward);
            }

            // Create geometry & uv mapping
            int facesCount = faces.Count;
            List<Vector3> vertices = new List<Vector3>(facesCount * 4);
            List<int> indices = new List<int>(facesCount * 6);
            List<Vector3> normals = new List<Vector3>(facesCount * 4);
            List<Color32> meshColors = new List<Color32>(facesCount * 4);
            index = 0;
            for (int k = 0; k < facesCount; k++, index += 4)
            {
                Face face = faces[k];
                Vector3 faceVertex;
                for (int j = 0; j < 4; j++)
                {
                    faceVertex.x = (face.center.x + face.vertices[j].x * face.size.x) * scale.x;
                    faceVertex.y = (face.center.y + face.vertices[j].y * face.size.y) * scale.y;
                    faceVertex.z = (face.center.z + face.vertices[j].z * face.size.z) * scale.z;
                    vertices.Add(faceVertex);
                    meshColors.Add(face.color);
                }
                normals.AddRange(face.normals);
                indices.Add(index);
                indices.Add(index + 1);
                indices.Add(index + 2);
                indices.Add(index + 3);
                indices.Add(index + 2);
                indices.Add(index + 1);
            }

            Mesh mesh = new Mesh();
            mesh.SetVertices(vertices);
            mesh.SetNormals(normals);
            mesh.SetTriangles(indices, 0);
            mesh.SetColors(meshColors);
            mesh.RecalculateBounds();

            GameObject obj = new GameObject("Model", typeof(VoxelPlayBehaviour));
            MeshFilter mf = obj.AddComponent<MeshFilter>();
            mf.mesh = mesh;
            MeshRenderer mr = obj.AddComponent<MeshRenderer>();
            mr.sharedMaterial = Resources.Load<Material>("VoxelPlay/Materials/VP Model VertexLit");

            string path = GetPathForNewModel();
            path += "/" + GetFilenameForNewModel(baseModel.name) + ".prefab";
            GameObject prefab = PrefabUtility.CreatePrefab(path, obj);
            // Store the mesh inside the prefab
            AssetDatabase.AddObjectToAsset(mesh, prefab);
            prefab.GetComponent<MeshFilter>().sharedMesh = mesh;
            MeshCollider mc = prefab.AddComponent<MeshCollider>();
            mc.sharedMesh = mesh;
            mc.convex = true;
            Rigidbody rb = prefab.AddComponent<Rigidbody>();
            rb.constraints = RigidbodyConstraints.FreezeRotationX | RigidbodyConstraints.FreezeRotationZ;
            AssetDatabase.SaveAssets();
            DestroyImmediate(obj);

            EditorUtility.FocusProjectWindow();
            Selection.activeObject = prefab;
        }

        void RemoveDuplicateOrAddFace(List<Face> faces, Face face)
        {
            int index = faces.IndexOf(face);
            if (index >= 0)
                faces.RemoveAt(index);
            else
                faces.Add(face);
        }

        ColorBasedModelDefinition BinaryToColorBasedModelDefinition()
        {
            if (importFormat == IMPORT_FORMAT.MagicaVoxel)
            {
                return MagicaVoxelBinaryToColorBasedModelDefinition();
            }
            
            return ColorBasedModelDefinition.Null;
        }


        private ColorBasedModelDefinition MagicaVoxelBinaryToColorBasedModelDefinition()
        {
            ColorBasedModelDefinition baseModel = ColorBasedModelDefinition.Null;
            Stream file = System.IO.File.Open(importFilename, FileMode.Open);
            try
            {
                baseModel = VE.Importers.MagicaVoxelImporter.ImportBinary(file, System.Text.Encoding.UTF8);
            }
            catch (Exception e)
            {
                Debug.Log("excp in Magica to Bin: " + e.ToString());
            }
            finally
            {
                file.Close();
            }
            return baseModel;
        }

        string GetPathForNewModel()
        {
            string path;
            if (VoxelPlayEnvironment.instance != null)
            {
                path = AssetDatabase.GetAssetPath(VoxelPlayEnvironment.instance.world);
                path = System.IO.Path.GetDirectoryName(path) + "/Models";
            }
            else
            {
                path = "Assets/ImportedModels";
            }
            System.IO.Directory.CreateDirectory(path);
            return path;
        }

        string GetFilenameForNewModel(string proposed)
        {
            if (string.IsNullOrEmpty(proposed))
            {
                return "NewModel";
            }
            else
            {
                return String.Concat(proposed.Split(Path.GetInvalidFileNameChars()));
            }

        }



    }

}
