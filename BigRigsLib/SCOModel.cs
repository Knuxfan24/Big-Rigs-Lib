using Assimp;
using System;
using System.IO;
using System.Numerics;
using System.Collections.Generic;

namespace BigRigsLib
{
    // Python script from https://www.yourewinner.com/index.php?topic=5212.0 was heavily referenced in the creation of this class.
    // This code is rather sloppy and could do with being cleaned up at some point as I figure things out.

    /// <summary>
    /// <para>File base for the SCO model format and the MAT material library format.</para>
    /// </summary>
    public class SCOModel
    {
        public SCOModel() { }
        public SCOModel(string file)
        {
            switch (Path.GetExtension(file))
            {
                // Specifying an FBX or OBJ (need to test more formats) should import it straight away.
                case ".fbx":
                case ".obj":
                ImportAssimp(file);
                break;

                // Specifying a MAT file should load the materials from it.
                case ".mat":
                LoadMaterials(file);
                break;

                // Assume the file is an SCO file and try to load the model from it.
                default:
                LoadModel(file);
                break;
            }
        }

        public class Face
        {
            public int Vertex1;
            public int Vertex2;
            public int Vertex3;
            public string Material;
        }

        public class Material
        {
            public string Name;
            public string Flags;
            public int Opacity;
            public string Texture;
            public string AlphaMask;
            public Vector3 Colour;
            public string NormalMap;
            public string EnvironmentMap;
            public int EnvironmentMapPower;
        }

        public string Name;
        public List<Vector3> Vertices           = new List<Vector3>();
        public List<Vector2> TextureCoordinates = new List<Vector2>();
        public List<Face> Faces                 = new List<Face>();
        public List<Material> Materials         = new List<Material>();

        /// <summary>
        /// <para>Load a model from the specified file.</para>
        /// </summary>
        /// <param name="filepath">The file to read.</param>
        public void LoadModel(string filepath)
        {
            // Read SCO File.
            string[] sco = File.ReadAllLines(filepath);

            // Get this model's name.
            Name = sco[1].Substring(6);

            // Verticies
            int vertCount = int.Parse(sco[3].Substring(7));
            for (int i = 0; i < vertCount; i++)
            {
                string[] vertexString = sco[4 + i].Split(' ');
                Vertices.Add(new Vector3(float.Parse(vertexString[0]), float.Parse(vertexString[1]), float.Parse(vertexString[2])));
            }

            // Faces & Textures Coordinates.
            int faceCount = int.Parse(sco[4 + vertCount].Substring(7));
            for (int i = 0; i < faceCount; i++)
            {
                string[] faceString = sco[5 + vertCount + i].Split(new char[] { ' ', '\t' }, StringSplitOptions.RemoveEmptyEntries);

                // Face.
                Face face = new Face
                {
                    Vertex1 = int.Parse(faceString[1]),
                    Vertex2 = int.Parse(faceString[2]),
                    Vertex3 = int.Parse(faceString[3]),
                    Material = faceString[4]
                };
                Faces.Add(face);

                // Texture Coordinates.
                Vector2 textureCoordinate = new Vector2(float.Parse(faceString[5]), Math.Abs(float.Parse(faceString[6])));
                TextureCoordinates.Add(textureCoordinate);

                textureCoordinate = new Vector2(float.Parse(faceString[7]), Math.Abs(float.Parse(faceString[8])));
                TextureCoordinates.Add(textureCoordinate);

                textureCoordinate = new Vector2(float.Parse(faceString[9]), Math.Abs(float.Parse(faceString[10])));
                TextureCoordinates.Add(textureCoordinate);
            }
        }

        /// <summary>
        /// <para>Load materials from the specified file.</para>
        /// </summary>
        /// <param name="filepath">The file to read.</param>
        public void LoadMaterials(string filepath)
        {
            // Read Material File and setup dummy material.
            string[] mat = File.ReadAllLines(filepath);
            Material material = new Material();

            // Loop through lines in the material file. TODO: Is there a better way I can handle this? This is kinda messy.
            for (int i = 0; i < mat.Length; i++)
            {
                // Create new Material.
                if (mat[i] == "[MaterialBegin]") { material = new Material(); }

                // Read Material's Name.
                else if (mat[i].StartsWith("Name= ")) { material.Name = mat[i].Substring(6); }

                // Read Material's Flags.
                else if (mat[i].StartsWith("Flags= ")) { material.Flags = mat[i].Substring(7); }

                // Read Material's Opacity value.
                else if (mat[i].StartsWith("Opacity= ")) { material.Opacity = int.Parse(mat[i].Substring(9)); }

                // Read Material's Texture.
                else if (mat[i].StartsWith("Texture= ")) { material.Texture = mat[i].Substring(9); }

                // Read Material's Alpha Mask.
                else if (mat[i].StartsWith("AlphaMask= ")) { material.AlphaMask = mat[i].Substring(11); }

                // Read Material's Normal Map.
                else if (mat[i].StartsWith("NormalMap= ")) { material.NormalMap = mat[i].Substring(11); }

                // Parse Material's Colour values.
                else if (mat[i].StartsWith("Color24= "))
                {
                    var split = mat[i].Split(' ');
                    material.Colour = new Vector3(float.Parse(split[1]), float.Parse(split[2]), float.Parse(split[3]));
                }

                // Read Material's Environment Map.
                else if (mat[i].StartsWith("EnvMap= ")) { material.EnvironmentMap = mat[i].Substring(8); }

                // Read Material's Environment Map Power.
                else if (mat[i].StartsWith("EnvPower= ")) { material.EnvironmentMapPower = int.Parse(mat[i].Substring(10)); }

                // Save Material.
                else if (mat[i] == "[MaterialEnd]") { Materials.Add(material); }

                // Warn about unhandled material parameters..
                else
                {
                    if (mat[i] != "")
                    {
                        var split = mat[i].Split(' ');
                        Console.WriteLine($"Value {split[0]} in {filepath} not handled!");
                    }
                }
            }
        }

        /// <summary>
        /// <para>Save the loaded model data to the specified file.</para>
        /// </summary>
        /// <param name="filepath">The file to save to.</param>
        public void SaveModel(string filepath)
        {
            using (StreamWriter sco = new StreamWriter(filepath))
            {
                // "Header".
                sco.WriteLine("[ObjectBegin]");
                sco.WriteLine($"Name= {Name}");
                sco.WriteLine("CentralPoint= 0 0 0"); // TODO: Figure out if I need to do anything with this.

                // Verticies.
                sco.WriteLine($"Verts= {Vertices.Count}");
                foreach (Vector3 vertex in Vertices)
                {
                    sco.WriteLine($"{vertex.X} {vertex.Y} {vertex.Z}");
                }

                // Faces (probably not writing the Texture Coordinates properly, I THINK I understand how they work though).
                sco.WriteLine($"Faces= {Faces.Count}");
                for (int i = 0; i < Faces.Count; i++)
                {
                    sco.WriteLine($"3 {Faces[i].Vertex1} {Faces[i].Vertex2} {Faces[i].Vertex3} {Faces[i].Material} {TextureCoordinates[Faces[i].Vertex1].X} {-TextureCoordinates[Faces[i].Vertex1].Y}" +
                                  $"{TextureCoordinates[Faces[i].Vertex2].X} {-TextureCoordinates[Faces[i].Vertex2].Y} {TextureCoordinates[Faces[i].Vertex3].X} {-TextureCoordinates[Faces[i].Vertex3].Y}");
                }

                // "Footer".
                sco.WriteLine("[ObjectEnd]");
            }
        }

        /// <summary>
        /// <para>Save the loaded material data to the specified file.</para>
        /// </summary>
        /// <param name="filepath">The file to save to.</param>
        public void SaveMaterials(string filepath)
        {
            using (StreamWriter mat = new StreamWriter(filepath))
            {
                for (int i = 0; i < Materials.Count; i++)
                {
                    // Material "Header".
                    mat.WriteLine("[MaterialBegin]");

                    // Values.
                    if (Materials[i].Name != null)             { mat.WriteLine($"Name= {Materials[i].Name}"); }
                    if (Materials[i].Flags != null)            { mat.WriteLine($"Flags= {Materials[i].Flags}"); }
                    if (Materials[i].Opacity != 0)             { mat.WriteLine($"Opacity= {Materials[i].Opacity}"); }
                    if (Materials[i].Texture != null)          { mat.WriteLine($"Texture= {Materials[i].Texture}"); }
                    if (Materials[i].EnvironmentMap != null)   { mat.WriteLine($"EnvMap= {Materials[i].EnvironmentMap}"); }
                    if (Materials[i].EnvironmentMapPower != 0) { mat.WriteLine($"EnvPower= {Materials[i].EnvironmentMapPower}"); }
                    if (Materials[i].AlphaMask != null)        { mat.WriteLine($"AlphaMask= {Materials[i].AlphaMask}"); }
                    if (Materials[i].NormalMap != null)        { mat.WriteLine($"NormalMap= {Materials[i].NormalMap}"); }
                    mat.WriteLine($"Color24= {Convert.ToInt32(Materials[i].Colour.X)} {Convert.ToInt32(Materials[i].Colour.Y)} {Convert.ToInt32(Materials[i].Colour.Z)}");

                    // Material "Footer".
                    mat.WriteLine("[MaterialEnd]\n");
                }
            }
        }

        /// <summary>
        /// <para>Export the loaded model as a Wavefront OBJ file.</para>
        /// </summary>
        /// <param name="filepath">The file to save to.</param>
        public void ExportOBJ(string filepath)
        {
            // TODO: Write an MTL.
            using (StreamWriter obj = new StreamWriter(filepath))
            {
                // Verticies
                obj.WriteLine("# Vertex Data");
                foreach (Vector3 vertex in Vertices)
                    obj.WriteLine($"v {vertex.X} {vertex.Y} {vertex.Z}");

                // Texture Coordinates
                obj.WriteLine("\n# Texture Coordinate Data");
                foreach (Vector2 textureCoordinate in TextureCoordinates)
                {
                    obj.WriteLine($"vt {textureCoordinate.X} {textureCoordinate.Y}");
                }

                // Faces
                int index = 1;
                obj.WriteLine("\n# Face Data");
                obj.WriteLine($"o {Name}");
                for (int i = 0; i < Faces.Count; i++)
                {
                    obj.WriteLine($"f {Faces[i].Vertex1 + 1}/{index} {Faces[i].Vertex2 + 1}/{index + 1} {Faces[i].Vertex3 + 1}/{index + 2}");
                    index += 3;
                }
            }
        }

        /// <summary>
        /// <para>Import a model file using Assimp-Net.</para>
        /// </summary>
        /// <param name="filepath">The file to read.</param>
        public void ImportAssimp(string filepath)
        {
            // Setup Assimp Scene.
            AssimpContext assimpImporter = new AssimpContext();
            Scene assimpModel = assimpImporter.ImportFile(filepath, PostProcessSteps.PreTransformVertices);

            for (int m = 0; m < assimpModel.MeshCount; m++)
            {
                // Vertices.
                int previousVertexCount = Vertices.Count;
                for (int i = 0; i < assimpModel.Meshes[m].Vertices.Count; i++)
                {
                    Vector3 vertex = new Vector3(assimpModel.Meshes[m].Vertices[i].X, assimpModel.Meshes[m].Vertices[i].Y, assimpModel.Meshes[m].Vertices[i].Z);
                    Vertices.Add(vertex);

                }

                // Texture Coordinates. These seem to not be fully correct right now.
                for (int i = 0; i < assimpModel.Meshes[m].TextureCoordinateChannels[0].Count; i++)
                {
                    Vector2 textureCoordinate = new Vector2(assimpModel.Meshes[m].TextureCoordinateChannels[0][i].X, assimpModel.Meshes[m].TextureCoordinateChannels[0][i].Y);
                    TextureCoordinates.Add(textureCoordinate);
                }

                // Faces.
                Face face;
                foreach (Assimp.Face assimpFace in assimpModel.Meshes[m].Faces)
                {
                    face = new Face
                    {
                        Vertex1 = assimpFace.Indices[0] + previousVertexCount,
                        Vertex2 = assimpFace.Indices[1] + previousVertexCount,
                        Vertex3 = assimpFace.Indices[2] + previousVertexCount,
                        Material = assimpModel.Materials[m].Name
                    };
                    Faces.Add(face);
                }
            }

            // Materials.
            for (int i = 0; i < assimpModel.MaterialCount; i++)
            {
                // TODO: Improve Material Importing.
                Material material = new Material
                {
                    Name = assimpModel.Materials[i].Name,
                    Flags = "texture_gouraud_",
                    Opacity = 255,
                    Texture = assimpModel.Materials[i].TextureDiffuse.FilePath.Substring(assimpModel.Materials[i].TextureDiffuse.FilePath.LastIndexOf('\\') + 1),
                    Colour = new Vector3(255f, 255f, 255f)
                };
                Materials.Add(material);
            }
        }
    }
}
