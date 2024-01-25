// MIT License

// Copyright (c) 2021 NedMakesGames

// Permission is hereby granted, free of charge, to any person obtaining a copy
// of this software and associated documentation files(the "Software"), to deal
// in the Software without restriction, including without limitation the rights
// to use, copy, modify, merge, publish, distribute, sublicense, and / or sell
// copies of the Software, and to permit persons to whom the Software is
// furnished to do so, subject to the following conditions :

// The above copyright notice and this permission notice shall be included in all
// copies or substantial portions of the Software.

// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
// IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
// FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT.IN NO EVENT SHALL THE
// AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
// LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
// OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE
// SOFTWARE.

using System.Runtime.InteropServices;
using UnityEngine;
using UnityEngine.Rendering;

public static class BladeGrassBaker
{
    // The size of one entry in the various compute buffers
    private const int SOURCE_VERT_STRIDE = sizeof(float) * 3;
    private const int SOURCE_INDEX_STRIDE = sizeof(int);
    private const int GENERATED_VERT_STRIDE = sizeof(float) * (3 + 3 + 2 + 3 + 3);
    private const int GENERATED_INDEX_STRIDE = sizeof(int);

    // This function takes in a mesh and submesh and decomposes it into vertex and index arrays
    // A submesh is a subset of triangles in the mesh. This might happen, for instance, if a mesh
    // has a multiple materials.
    private static void DecomposeMesh(Mesh mesh, int subMeshIndex, out SourceVertex[] verts, out int[] indices)
    {
        SubMeshDescriptor subMesh = mesh.GetSubMesh(subMeshIndex);

        Vector3[] allVertices = mesh.vertices;
        int[] allIndices = mesh.triangles;

        verts = new SourceVertex[subMesh.vertexCount];
        indices = new int[subMesh.indexCount];
        for (int i = 0; i < subMesh.vertexCount; i++)
        {
            // Find the index in the whole mesh index buffer
            int wholeMeshIndex = i + subMesh.firstVertex;
            verts[i] = new SourceVertex
            {
                position = allVertices[wholeMeshIndex]
            };
        }

        for (int i = 0; i < subMesh.indexCount; i++)
            // We need to offset the indices in the mesh index buffer to match
            // the indices in our new vertex buffer. Subtract by subMesh.firstVertex
            // .baseVertex is an offset Unity may define which is a global
            // offset for all indices in this submesh
            indices[i] = allIndices[i + subMesh.indexStart] + subMesh.baseVertex - subMesh.firstVertex;
    }

    // This function takes a vertex and index list and converts it into a Mesh object
    private static Mesh ComposeMesh(GeneratedVertex[] verts, int[] indices)
    {
        Mesh mesh = new();
        Vector3[] vertices = new Vector3[verts.Length];
        Vector3[] normals = new Vector3[verts.Length];
        Vector2[] uvs = new Vector2[verts.Length];
        Vector3[] bladeAnchors = new Vector3[verts.Length];
        Vector3[] shadowCastNormals = new Vector3[verts.Length];
        for (int i = 0; i < verts.Length; i++)
        {
            GeneratedVertex v = verts[i];
            vertices[i] = v.position;
            normals[i] = v.normal;
            uvs[i] = v.uv;
            bladeAnchors[i] = v.bladeAnchor;
            shadowCastNormals[i] = v.shadowCastNormal;
        }

        mesh.SetVertices(vertices);
        mesh.SetNormals(normals);
        mesh.SetUVs(0, uvs); // TEXCOORD0
        mesh.SetUVs(1, bladeAnchors); // TEXCOORD1
        mesh.SetUVs(2, shadowCastNormals); // TEXCOORD2
        mesh.SetIndices(indices, MeshTopology.Triangles, 0, true); // This sets the index list as triangles
        mesh.Optimize(); // Let Unity optimize the buffer orders
        return mesh;
    }

    public static bool Run(ComputeShader shader, BladeGrassBakeSettings settings, out Mesh generatedMesh)
    {
        Debug.Assert(settings.numBladeSegments > 0);
        // Decompose the mesh into vertex/index buffers
        DecomposeMesh(settings.sourceMesh, settings.sourceSubMeshIndex, out SourceVertex[] sourceVertices,
            out int[] sourceIndices);

        // The mesh topology is triangles, so there are three indices per triangle
        int numSourceTriangles = sourceIndices.Length / 3;
        int numGeneratedVertices =
            numSourceTriangles * (settings.numBladeSegments * 2 + 1) *
            2; // 2 vertices per segment, plus the tip, doubled
        int numGeneratedIndices = numSourceTriangles * (settings.numBladeSegments * 2 - 1) * 3 * 2;

        GeneratedVertex[] generatedVertices = new GeneratedVertex[numGeneratedVertices];
        int[] generatedIndices = new int[numGeneratedIndices];

        // A graphics buffer is a better version of the compute buffer
        GraphicsBuffer sourceVertBuffer =
            new(GraphicsBuffer.Target.Structured, sourceVertices.Length, SOURCE_VERT_STRIDE);
        GraphicsBuffer sourceIndexBuffer =
            new(GraphicsBuffer.Target.Structured, sourceIndices.Length, SOURCE_INDEX_STRIDE);
        GraphicsBuffer genVertBuffer =
            new(GraphicsBuffer.Target.Structured, generatedVertices.Length, GENERATED_VERT_STRIDE);
        GraphicsBuffer genIndexBuffer =
            new(GraphicsBuffer.Target.Structured, generatedIndices.Length, GENERATED_INDEX_STRIDE);

        // Cache the kernel ID
        int idGrassKernel = shader.FindKernel("Main");

        // Set buffers and variables
        shader.SetBuffer(idGrassKernel, "_SourceVertices", sourceVertBuffer);
        shader.SetBuffer(idGrassKernel, "_SourceIndices", sourceIndexBuffer);
        shader.SetBuffer(idGrassKernel, "_GeneratedVertices", genVertBuffer);
        shader.SetBuffer(idGrassKernel, "_GeneratedIndices", genIndexBuffer);
        // Convert the scale and rotation settings into a transformation matrix
        shader.SetMatrix("_Transform",
            Matrix4x4.TRS(Vector3.zero, Quaternion.Euler(settings.rotation), settings.scale));
        shader.SetInt("_NumSourceTriangles", numSourceTriangles);
        shader.SetVector("_RandomSeed", settings.randomOffset);
        shader.SetInt("_NumBladeSegments", settings.numBladeSegments);
        shader.SetFloat("_BladeCurvature", Mathf.Max(0, settings.curvature));
        shader.SetFloat("_MaxBendAngle", settings.maxBendAngle * Mathf.Deg2Rad);
        shader.SetFloat("_BladeWidth", settings.width);
        shader.SetFloat("_BladeWidthVariance", settings.widthVariance);
        shader.SetFloat("_BladeHeight", settings.height);
        shader.SetFloat("_BladeHeightVariance", settings.heightVariance);

        // Set data in the buffers
        sourceVertBuffer.SetData(sourceVertices);
        sourceIndexBuffer.SetData(sourceIndices);

        // Find the needed dispatch size, so that each triangle will be run over
        shader.GetKernelThreadGroupSizes(idGrassKernel, out uint threadGroupSize, out _, out _);
        int dispatchSize = Mathf.CeilToInt((float)numSourceTriangles / threadGroupSize);
        // Dispatch the compute shader
        shader.Dispatch(idGrassKernel, dispatchSize, 1, 1);

        // Get the data from the compute shader
        // Unity will wait here until the compute shader is completed
        // Don't do this as runtime. Look into AsyncGPUReadback
        genVertBuffer.GetData(generatedVertices);
        genIndexBuffer.GetData(generatedIndices);

        // Compose the vertex/index buffers into a mesh
        generatedMesh = ComposeMesh(generatedVertices, generatedIndices);

        // Release the graphics buffers, disposing them
        sourceVertBuffer.Release();
        sourceIndexBuffer.Release();
        genVertBuffer.Release();
        genIndexBuffer.Release();

        return true; // No error
    }

    // The structure to send to the compute shader
    // This layout kind assures that the data is laid out sequentially
    [StructLayout(LayoutKind.Sequential)]
    private struct SourceVertex
    {
        public Vector3 position;
    }

    // The structure received from the compute shader
    [StructLayout(LayoutKind.Sequential)]
    private struct GeneratedVertex
    {
        public Vector3 position;
        public Vector3 normal;
        public Vector2 uv;
        public Vector3 bladeAnchor;
        public Vector3 shadowCastNormal;
    }
}
