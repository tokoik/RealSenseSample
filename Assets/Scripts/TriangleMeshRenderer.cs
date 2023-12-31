using System;
using UnityEngine;
using Intel.RealSense;
using UnityEngine.Rendering;
using UnityEngine.Assertions;
using System.Runtime.InteropServices;
using System.Threading;
using System.Collections.Generic;
using System.Linq;

//[RequireComponent(typeof(MeshFilter), typeof(MeshRenderer))]
//public class RsPointCloudRenderer : MonoBehaviour
public class TriangleMeshRenderer : MonoBehaviour
{
  public RsFrameProvider Source;
  //private Mesh mesh;
  private GraphicsBuffer vertexBuffer = null;
  //private GraphicsBuffer indexBuffer = null;
  private int instances = 0;
  [SerializeField]
  private Material material;
  private Texture2D uvmap;

  [NonSerialized]
  private Vector3[] vertices;

  FrameQueue q;

  void Start()
  {
    Source.OnStart += OnStartStreaming;
    Source.OnStop += Dispose;
  }

  private void OnStartStreaming(PipelineProfile obj)
  {
    q = new FrameQueue(1);

    using (var depth = obj.Streams.FirstOrDefault(s => s.Stream == Stream.Depth && s.Format == Format.Z16).As<VideoStreamProfile>())
      ResetMesh(depth.Width, depth.Height);

    Source.OnNewSample += OnNewSample;
  }

  private static int[] CreateTriangleMeshIndex(int slices, int stacks)
  {
    // 三角形の頂点の数
    int vertices = slices * stacks * 6;

    // 三角形のデータ
    int[] indeces = new int[vertices];

    // 三角形の頂点番号を求める
    for (int j = 0; j < stacks; ++j)
    {
      // 各段の左端の下側の頂点番号の格納先
      int f0 = j * slices * 6;

      // 各段の左端の下側の頂点の頂点番号
      int p0 = j * (slices + 1);

      for (int i = 0; i < slices; ++i)
      {
        // 左から i 番目の四角形の左下の頂点番号の格納先
        int fi = f0 + i * 6;

        // 左から i 番目の四角形の左下の頂点番号
        int pi = p0 + i;

        // １つ目の三角形の頂点番号
        indeces[fi + 0] = pi;
        indeces[fi + 1] = pi + 1;
        indeces[fi + 2] = pi + slices + 1;

        // ２つ目の三角形の頂点番号
        indeces[fi + 3] = pi + slices + 2;
        indeces[fi + 4] = pi + slices + 1;
        indeces[fi + 5] = pi + 1;
      }
    }

    // 作成したインデックスを返す
    return indeces;
  }

  private void ResetMesh(int width, int height)
  {
    Assert.IsTrue(SystemInfo.SupportsTextureFormat(TextureFormat.RGFloat));
    uvmap = new Texture2D(width, height, TextureFormat.RGFloat, false, true)
    {
      wrapMode = TextureWrapMode.Clamp,
      filterMode = FilterMode.Point,
    };
    //GetComponent<MeshRenderer>().sharedMaterial.SetTexture("_UVMap", uvmap);
    //material = GetComponent<MeshRenderer>().sharedMaterial;
    material.SetTexture("_UVMap", uvmap);

    //if (mesh != null)
    //  mesh.Clear();
    //else
    //  mesh = new Mesh()
    //  {
    //    indexFormat = IndexFormat.UInt32,
    //  };

    vertices = new Vector3[width * height];
    if (vertexBuffer != null)
      vertexBuffer.Release();
    vertexBuffer = new GraphicsBuffer(GraphicsBuffer.Target.Structured,
      vertices.Length, sizeof(float) * 3);
    material.SetBuffer("_Vertex", vertexBuffer);

    //var indices = new int[vertices.Length];
    //for (int i = 0; i < vertices.Length; i++)
    //  indices[i] = i;
    //var indices = CreateTriangleMeshIndex(width - 1, height - 1);
    //if (indexBuffer != null)
    //  indexBuffer.Release();
    //indexBuffer = new GraphicsBuffer(GraphicsBuffer.Target.Index,
    //  indices.Length, sizeof(int));
    //indexBuffer.SetData(indices);
    instances = (width - 1) * (height - 1);

    //mesh.MarkDynamic();
    //mesh.vertices = vertices;

    //var uvs = new Vector2[width * height];
    //Array.Clear(uvs, 0, uvs.Length);
    //for (int j = 0; j < height; j++)
    //{
    //  for (int i = 0; i < width; i++)
    //  {
    //    uvs[i + j * width].x = i / (float)width;
    //    uvs[i + j * width].y = j / (float)height;
    //  }
    //}

    //mesh.uv = uvs;

    ////mesh.SetIndices(indices, MeshTopology.Points, 0, false);
    //mesh.SetIndices(indices, MeshTopology.Triangles, 0, false);
    //mesh.bounds = new Bounds(Vector3.zero, Vector3.one * 10f);

    //GetComponent<MeshFilter>().sharedMesh = mesh;
  }

  void OnDestroy()
  {
    if (q != null)
    {
      q.Dispose();
      q = null;
    }

    //if (mesh != null)
    //  Destroy(null);
    //if (indexBuffer != null)
    //  indexBuffer.Release();
    if (vertexBuffer != null)
    {
      vertexBuffer.Release();
      Destroy(null);
    }
  }

  private void Dispose()
  {
    Source.OnNewSample -= OnNewSample;

    if (q != null)
    {
      q.Dispose();
      q = null;
    }
  }

  private void OnNewSample(Frame frame)
  {
    if (q == null)
      return;
    try
    {
      if (frame.IsComposite)
      {
        using (var fs = frame.As<FrameSet>())
        using (var points = fs.FirstOrDefault<Points>(Stream.Depth, Format.Xyz32f))
        {
          if (points != null)
          {
            q.Enqueue(points);
          }
        }
        return;
      }

      if (frame.Is(Extension.Points))
      {
        q.Enqueue(frame);
      }
    }
    catch (Exception e)
    {
      Debug.LogException(e);
    }
  }


  protected void LateUpdate()
  {
    if (q != null)
    {
      Points points;
      if (q.PollForFrame<Points>(out points))
        using (points)
        {
          //if (points.Count != mesh.vertexCount)
          if (points.Count != vertices.Length)
          {
            using (var p = points.GetProfile<VideoStreamProfile>())
              ResetMesh(p.Width, p.Height);
          }

          if (points.TextureData != IntPtr.Zero)
          {
            uvmap.LoadRawTextureData(points.TextureData, points.Count * sizeof(float) * 2);
            uvmap.Apply();
          }

          if (points.VertexData != IntPtr.Zero)
          {
            points.CopyVertices(vertices);

            //mesh.vertices = vertices;
            //mesh.UploadMeshData(false);
            vertexBuffer.SetData(vertices);
          }
        }
    }
  }

  void OnRenderObject()
  {
    //if (indexBuffer != null)
    if (instances > 0)
    {
      material.SetPass(0);
      //Graphics.DrawProceduralNow(MeshTopology.Triangles, indexBuffer, indexBuffer.count);
      Graphics.DrawProceduralNow(MeshTopology.Quads, 4, instances);
    }
  }
}
