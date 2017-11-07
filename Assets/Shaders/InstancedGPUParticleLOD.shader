Shader "Hidden/InstancedGPUParticleLOD" {

SubShader
{

Tags { "RenderType" = "Opaque" }

CGINCLUDE

#include "UnityCG.cginc"

struct Particle
{
	float3 position;
	float3 rotation;
	float3 scale;
	float4 color;
};

#if SHADER_TARGET >= 45
StructuredBuffer<Particle> _Particles;
StructuredBuffer<int> _Ids;
#endif

float4x4 _localToWorld;
uniform float4 _LightColor0;

struct appdata
{
	float4 vertex : POSITION;
	float3 normal : NORMAL;
};

struct v2f 
{
	float4 position : SV_POSITION;
	float3 normal : NORMAL;
	float4 diffuse : COLOR;
};

inline float3 rotate(float3 v, float3 rotation)
{
	float3 a = normalize(rotation);
	float angle = length(rotation);
	if (abs(angle) < 0.001) return v;
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
	return mul(m, v);
}

v2f vert(appdata v, uint instanceID : SV_INSTANCEID)
{
	float4 c = 1;
#if SHADER_TARGET >= 45
	Particle p = _Particles[_Ids[instanceID]];
	v.vertex.xyz *= p.scale;
	if(length(p.scale) != 0)
	{
		v.vertex.xyz = rotate(v.vertex.xyz, p.rotation);
		v.vertex.xyz += p.position;
		v.vertex = mul(_localToWorld, v.vertex);
		v.normal = rotate(v.normal, p.rotation);
		v.normal = mul(_localToWorld, float4(v.normal, 0)).xyz;
		v.normal = normalize(v.normal);
		c = p.color;
	}
#endif
	v2f o;
	o.position = mul(UNITY_MATRIX_VP, v.vertex);
	o.normal = v.normal;
	o.diffuse = c;
	return o;
}

float4 frag(v2f i) : COLOR
{
	float4 diffuse = i.diffuse;
	float3 normal = i.normal;
	float3 view = normalize(_WorldSpaceCameraPos - i.position.xyz);
	float3 light = normalize(_WorldSpaceLightPos0.xyz);

	float3 ambient = UNITY_LIGHTMODEL_AMBIENT.rgb * diffuse.rgb;
	float3 diffuseReflection = _LightColor0.rgb * diffuse.rgb * max(0, dot(normal, light));
	float3 emission = diffuse * 0.1;
	return float4(ambient + diffuseReflection + emission, 1);
}

ENDCG

Pass
{

Tags { "LightMode" = "ForwardBase" }
ZWrite On

CGPROGRAM

#pragma vertex vert
#pragma fragment frag
#pragma multi_compile_fwdbase
#pragma multi_compile_instancing
#pragma target 4.5

ENDCG

}

}

FallBack "Diffuse"

}
