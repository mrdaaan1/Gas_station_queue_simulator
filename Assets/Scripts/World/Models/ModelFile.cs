using System;
using System.IO;
using System.IO.Compression;
using System.Text;
using UnityEngine;

namespace GasQueue
{
    /// <summary>
    /// Модель машины из файла (её готовит Blender: Tools/Blender/x5_build.py). Формат «GQM1», сжат gzip:
    /// узлы по порядку обхода (имя, родитель, положение, углы Эйлера как в Unity) и их сетки по материалам
    /// (вершины, нормали, UV, треугольники) — в локальных координатах узла, как <see cref="ModelNode"/>.
    /// Нормали в файле готовые (сглаживание и острые кромки из Blender), их не пересчитываем.
    /// </summary>
    public static class ModelFile
    {
        public static Model Read(byte[] bytes)
        {
            using (var gz = new GZipStream(new MemoryStream(bytes), CompressionMode.Decompress))
            using (var r = new BinaryReader(gz, Encoding.UTF8))
            {
                if (new string(r.ReadChars(4)) != "GQM1") throw new InvalidDataException("не GQM1");
                int count = r.ReadInt32();
                var nodes = new ModelNode[count];
                var model = new Model();
                for (int i = 0; i < count; i++)
                {
                    string name = r.ReadString();
                    int parent = r.ReadInt32();
                    var pos = V3(r);
                    var euler = V3(r);
                    ModelNode node;
                    if (parent < 0)
                    {
                        node = model.root;
                        node.name = name;
                        node.pos = pos;
                        node.euler = euler;
                    }
                    else node = nodes[parent].Child(name, pos, euler);
                    node.keepNormals = true;
                    nodes[i] = node;
                    int meshes = r.ReadInt32();
                    for (int k = 0; k < meshes; k++)
                    {
                        var m = node.M(r.ReadString());
                        int nv = r.ReadInt32();
                        for (int j = 0; j < nv; j++)
                        {
                            var p = V3(r);
                            var n = V3(r);
                            var uv = new Vector2(r.ReadSingle(), r.ReadSingle());
                            m.Add(p, n, uv);
                        }
                        int ni = r.ReadInt32();
                        for (int j = 0; j < ni; j++) m.t.Add(r.ReadInt32());
                    }
                }
                return model;
            }
        }

        static Vector3 V3(BinaryReader r) => new Vector3(r.ReadSingle(), r.ReadSingle(), r.ReadSingle());
    }
}
