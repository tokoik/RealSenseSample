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

      struct appdata
      {
        float4 vertex : POSITION;
        float2 uv : TEXCOORD0;
      };

      struct v2f
      {
        float4 vertex : SV_POSITION;
        float2 uv : TEXCOORD0;
      };

      sampler2D _MainTex;
      sampler2D _UVMap;

      v2f vert(appdata v)
      {
        if (all((float3)v.vertex == 0.0))
        {
          v.vertex.w = 0.0;
          return v;
        }

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
