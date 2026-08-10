Shader "Custom/GridShader"
{
    Properties
    {
        _BackgroundColor ("Background Color", Color) = (0.15, 0.15, 0.15, 1)

        _MinorGridColor ("1m Grid Color", Color) = (0.35, 0.35, 0.35, 1)
        _MajorGridColor ("5m Grid Color", Color) = (0.65, 0.65, 0.65, 1)

        _MinorGridSize ("Minor Grid Size", Float) = 1
        _MajorGridSize ("Major Grid Size", Float) = 5

        _MinorLineWidth ("1m Line Width", Range(0.001, 0.2)) = 0.015
        _MajorLineWidth ("5m Line Width", Range(0.001, 0.3)) = 0.035
    }

    SubShader
    {
        Tags
        {
            "RenderType" = "Opaque"
            "RenderPipeline" = "UniversalPipeline"
        }

        Pass
        {
            HLSLPROGRAM

            #pragma vertex vert
            #pragma fragment frag

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            struct Attributes
            {
                float4 positionOS : POSITION;
            };

            struct Varyings
            {
                float4 positionHCS : SV_POSITION;
                float3 positionWS : TEXCOORD0;
            };

            CBUFFER_START(UnityPerMaterial)

                float4 _BackgroundColor;
                float4 _MinorGridColor;
                float4 _MajorGridColor;

                float _MinorGridSize;
                float _MajorGridSize;

                float _MinorLineWidth;
                float _MajorLineWidth;

            CBUFFER_END

            Varyings vert(Attributes input)
            {
                Varyings output;

                output.positionWS =
                    TransformObjectToWorld(input.positionOS.xyz);

                output.positionHCS =
                    TransformWorldToHClip(output.positionWS);

                return output;
            }

            float GridLine(
                float2 worldPosition,
                float gridSize,
                float lineWidth)
            {
                float2 grid =
                    abs(frac(worldPosition / gridSize + 0.5) - 0.5)
                    * gridSize;

                float distanceToLine = min(grid.x, grid.y);

                return 1.0 - smoothstep(
                    lineWidth,
                    lineWidth + fwidth(distanceToLine),
                    distanceToLine);
            }

            half4 frag(Varyings input) : SV_Target
            {
                // Unity의 바닥 Plane은 XZ 평면이므로
                // X, Z 월드 좌표를 사용한다.
                float2 worldPosition = input.positionWS.xz;

                float minorGrid = GridLine(
                    worldPosition,
                    _MinorGridSize,
                    _MinorLineWidth);

                float majorGrid = GridLine(
                    worldPosition,
                    _MajorGridSize,
                    _MajorLineWidth);

                float3 color = _BackgroundColor.rgb;

                // 1m 격자
                color = lerp(
                    color,
                    _MinorGridColor.rgb,
                    minorGrid);

                // 5m 격자를 나중에 그려서 우선순위를 높인다.
                color = lerp(
                    color,
                    _MajorGridColor.rgb,
                    majorGrid);

                return half4(color, 1);
            }

            ENDHLSL
        }
    }
}
