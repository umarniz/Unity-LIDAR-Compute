Shader "Custom/PointRender" 
{
    Properties 
    {
        _ParticleTexture ("Diffuse Tex", 2D) = "white" {}
        _Ramp1Texture ("G_Ramp1", 2D) = "white" {}
    }

    SubShader 
    {
        Pass 
        {
            Tags { "Queue"="Transparent" "IgnoreProjector"="True" "RenderType"="Transparent" }
            //Blend OneMinusDstColor One
            Cull Off 
            Lighting Off 
            //ZWrite Off 
            Fog { Color (0,0,0,0) }


            CGPROGRAM
            #pragma target 5.0
            #pragma vertex VSMAIN
            #pragma fragment PSMAIN
			#pragma geometry GSMAIN
            #include "UnityCG.cginc" 

            StructuredBuffer<float3>  points;

            struct VS_INPUT
            {
                uint vertexid           : SV_VertexID;
            };
            //--------------------------------------------------------------------------------
            struct PS_INPUT
            {
                float4 position         : SV_POSITION;
            };
            //--------------------------------------------------------------------------------
            PS_INPUT VSMAIN( in VS_INPUT input )
            {
                PS_INPUT output;
				UNITY_INITIALIZE_OUTPUT(PS_INPUT,output);

                output.position.xyz = points[input.vertexid];
				
                return output;
            }
            //--------------------------------------------------------------------------------
            [maxvertexcount(4)]
            void GSMAIN( point PS_INPUT p[1], inout TriangleStream<PS_INPUT> triStream )
            {           
                float4 pos = p[0].position;

                float halfS = 0.75 * 0.5f;
                float4 offset = float4(halfS, halfS, 0, 1);

                float4 v[4];
                v[0] = pos + float4(offset.x, offset.y, 0, 1);
                v[1] = pos + float4(offset.x, -offset.y, 0, 1);
                v[2] = pos + float4(-offset.x, offset.y, 0, 1);
                v[3] = pos + float4(-offset.x, -offset.y, 0, 1);

                PS_INPUT pIn;
                pIn.position =  mul(UNITY_MATRIX_MVP, v[0]);
                /*pIn.texcoords = float2(1.0f, 0.0f);

                    pIn.size = p[0].size;
                    pIn.age = p[0].age;
                    pIn.type = p[0].type;   */                    

                triStream.Append(pIn);

                pIn.position =  mul(UNITY_MATRIX_MVP, v[1]);
                //pIn.texcoords = float2(1.0f, 1.0f);
                triStream.Append(pIn);

                pIn.position =  mul(UNITY_MATRIX_MVP, v[2]);
               // pIn.texcoords = float2(0.0f, 0.0f);
                triStream.Append(pIn);

                pIn.position =  mul(UNITY_MATRIX_MVP, v[3]);
                //pIn.texcoords = float2(0.0f, 1.0f);
                triStream.Append(pIn);                  
            }
            //--------------------------------------------------------------------------------
            float4 PSMAIN( in PS_INPUT input ) : COLOR
            {
				float height = input.position.y;

                float4 color = float4(height,height,height,1.0);

                return color;
            }
            //--------------------------------------------------------------------------------
            ENDCG
        }
    } 
}