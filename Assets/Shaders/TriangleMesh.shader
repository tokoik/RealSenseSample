Shader "Unlit/TriangleMesh" {
  Properties{
    _MainTex("Texture", 2D) = "white" {}
    _UVMap("UV", 2D) = "" {}
  }

    SubShader
  {
    Pass
    {
      CGPROGRAM
      #pragma vertex vert
      #pragma geometry geom
      #pragma fragment frag

      #include "UnityCG.cginc"

      //struct appdata
      //{
      //  float4 vertex : POSITION;
      //  float2 uv : TEXCOORD0;
      //};

      struct v2f
      {
        float4 vertex : SV_POSITION;
        float2 uv : TEXCOORD0;
      };

      sampler2D _MainTex;
      sampler2D _UVMap;
      float4 _UVMap_TexelSize;
      StructuredBuffer<float3> _Vertex;

      //v2f vert(appdata v)
      v2f vert(uint vertex_id : SV_VertexID, uint instance_id : SV_InstanceID)
      {
        // vertex_id は 0～3 なので instance_id と組み合わせて実際の頂点番号を求める
        uint b0 = vertex_id & 1;
        uint b1 = vertex_id >> 1;
        vertex_id = instance_id + b1 * _UVMap_TexelSize.z + (b0 ^ b1);

        v2f v;
        v.vertex = float4(_Vertex[vertex_id], 1.0);

        if (all((float3)v.vertex == 0.0))
        {
          v.vertex.w = 0.0;
          return v;
        }

        // UV を vertex_id から求める
        v.uv = float2(fmod(vertex_id, _UVMap_TexelSize.z) * _UVMap_TexelSize.x,
          floor(vertex_id * _UVMap_TexelSize.x) * _UVMap_TexelSize.y);
        v.vertex.y = -v.vertex.y;
        v.vertex = UnityObjectToClipPos(v.vertex);
        return v;
      }

      [maxvertexcount(3)]
      void geom(triangle v2f v[3], inout TriangleStream<v2f> ts)
      {
        if (any(float3(v[0].vertex.w, v[1].vertex.w, v[2].vertex.w) == 0.0)) return;

        ts.Append(v[0]);
        ts.Append(v[1]);
        ts.Append(v[2]);
      }

      fixed4 frag(v2f i) : SV_Target
      {
        float2 uv = tex2D(_UVMap, i.uv);
        if (any(float4(uv, 1.0 - uv) <= 0.0)) discard;
        return tex2D(_MainTex, uv);
      }
      ENDCG
    }
  }
}
