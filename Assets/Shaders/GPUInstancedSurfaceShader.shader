Shader "Hidden/GPUInstancedSurfaceShader" {

	Properties {
		_MainTex("Albedo (RGB)", 2D) = "white" {}
		_Glossiness("Smoothness", Range(0,1)) = 0.5
		_Metallic("Metallic", Range(0,1)) = 0.0
	}
	SubShader {
		Tags { "RenderType"="Opaque" }
		LOD 300

		CGPROGRAM
		// Physically based Standard lighting model, and enable shadows on all light types
		#pragma surface surf Standard addshadow vertex:vert nolightmap
		#pragma multi_compile_instancing
		#pragma instancing_options procedural:setup
		#pragma enable_d3d11_debug_symbols

		#pragma target 4.5

		struct Particle
		{
			float3 position;
			float3 rotation;
			float scale;
			float4 color;
		};

		sampler2D _MainTex;

		float4x4 _localToWorld;
		float4x4 _worldToLocal;

		struct appdata
		{
			float4 vertex : POSITION;
			float3 normal : NORMAL;
			float2 texcoord : TEXCOORD0;
			UNITY_VERTEX_INPUT_INSTANCE_ID
		};

		struct Input
		{
			float2 uv_MainTex;
		};

#ifdef UNITY_PROCEDURAL_INSTANCING_ENABLED 
		StructuredBuffer<Particle> _Particles;
#endif

		void setup()
		{
#ifdef UNITY_PROCEDURAL_INSTANCING_ENABLED 
			unity_ObjectToWorld = _localToWorld;
			unity_WorldToObject = _worldToLocal;
#endif
		}

		half _Glossiness;
		half _Metallic;

		float3 rotate(float3 p, float3 rotation)
		{
			float3 a = normalize(rotation);
			float angle = length(rotation);
			if (abs(angle) < 0.001) return p;
			float s = sin(angle);
			float c = cos(angle);
			float r = 1.0 - c;
			float3x3 m = float3x3(
				a.x * a.x * r + c,
				a.y * a.x * r + a.z * s,
				a.z * a.x * r - a.y * s,
				a.x * a.y * r - a.z * s,
				a.y * a.y * r + c,
				a.z * a.y * r + a.x * s,
				a.x * a.z * r + a.y * s,
				a.y * a.z * r - a.x * s,
				a.z * a.z * r + c
			);
			return mul(m, p);
		}

		void vert(inout appdata v)
		{
#ifdef UNITY_PROCEDURAL_INSTANCING_ENABLED
			Particle p = _Particles[unity_InstanceID];
			float3 pos = v.vertex.xyz;
			pos *= p.scale;
			pos = rotate(pos, p.rotation);
			pos += p.position;
			v.vertex = float4(pos, 1);
			v.normal = rotate(v.normal, p.rotation);
#endif
		}

		void surf (Input IN, inout SurfaceOutputStandard o) {
			// Albedo comes from a texture tinted by color
			float4 col = 1.0f;
#ifdef UNITY_PROCEDURAL_INSTANCING_ENABLED
			Particle p = _Particles[unity_InstanceID];
			col = p.color;
#endif
			//fixed4 c = tex2D(_MainTex, IN.uv_MainTex) * col;
			fixed4 c = col;
			o.Albedo = c.rgb;
			// Metallic and smoothness come from slider variables
			o.Metallic = _Metallic;
			o.Smoothness = _Glossiness;
			// o.Emission = o.Albedo * 0.01;
			//o.Alpha = c.a;
		}
		ENDCG
	}
	FallBack "Diffuse"
}
