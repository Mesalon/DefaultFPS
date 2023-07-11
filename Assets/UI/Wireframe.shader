Shader "SuperSystems/Wireframe"
{
	Properties
	{
		_WireThickness ("Wire Thickness", RANGE(0, 800)) = 100
		_WireSmoothness ("Wire Smoothness", RANGE(0, 20)) = 3
		_WireColor ("Wire Color", Color) = (0.0, 1.0, 0.0, 1.0)
		_WireEmission ("Wire Emission", Color) = (0.0, 1.0, 0.0, 1.0)
		_EmissionIntensity("Emission Intensity", float) = 1
		_BaseColor ("Base Color", Color) = (0.0, 0.0, 0.0, 1.0)
		_MaxTriSize ("Max Tri Size", RANGE(0, 200)) = 25
	}

	SubShader
	{
		Tags { 
			"RenderType"="Opaque"
		}

		Pass
		{
			CGPROGRAM
			#pragma vertex vert
			#pragma geometry geom
			#pragma fragment frag

			#include "UnityCG.cginc"
			#include "Wireframe.cginc"

			ENDCG
		}
	}
}
