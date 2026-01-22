using ISILab.LBS.Macros;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace ISILab.LBS.Plugin.MapTools.Generators
{
    public abstract class Generator3DOptimizer
    {
        public abstract void Optimize(GameObject rootObject);
    }

    public class MeshEntry
    {
        // stores the mesh itself, a material and submesh
        public MeshID Id;
        public MeshFilter Filter;
        public MeshRenderer Renderer;

        public MeshEntry(MeshID id, MeshFilter filter, MeshRenderer renderer)
        {
            Id = id;
            Filter = filter;
            Renderer = renderer;
        }
    }

    public struct MeshID
    {
        public Mesh Mesh;
        public Material Material;
        public int SubMesh;

        public MeshID(Mesh mesh, Material mat, int subMesh)
        {
            Mesh = mesh;
            Material = mat;
            SubMesh = subMesh;
        }

        public override bool Equals(object obj)
        {
            if (obj is not MeshID other) return false;
            return Mesh == other.Mesh &&
                   Material == other.Material &&
                   SubMesh == other.SubMesh;
        }

        public override int GetHashCode()
        {
            unchecked
            {
                int hash = Mesh != null ? Mesh.GetHashCode() : 0;
                // using 397, a large prime number to avoid collisions
                hash = (hash * 397) ^ (Material != null ? Material.GetHashCode() : 0);
                hash = (hash * 397) ^ SubMesh;
                return hash;
            }
        }
    }

}
