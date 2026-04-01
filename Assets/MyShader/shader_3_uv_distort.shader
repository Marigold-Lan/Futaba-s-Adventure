Shader "MyShaders/3_uv_distort_AntiTile"
{
    Properties
    {
        _MainTex ("Main Texture", 2D) = "white" {} 
        _DistortTex("Distortion Texture", 2D) = "white" {} 
        
        _DistortAmount("Distortion Amount", Range(0,2)) = 0.5 
        _DistortTexXSpeed("Scroll speed X", Range(-50,50)) = 5 
        _DistortTexYSpeed("Scroll speed Y", Range(-50,50)) = 5 
        
        _BaseColor ("Base Color", Color) = (1, 1, 1, 1)
        
        [HideInInspector] _RandomSeed("MaxYUV", Range(0, 10000)) = 0.0 
    }
    
    SubShader
    {
        Tags { "Queue"="Transparent" "RenderType"="Transparent" }
        Blend SrcAlpha OneMinusSrcAlpha 
        LOD 100

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile_fog
            #pragma multi_compile_instancing 

            #include "UnityCG.cginc"

            struct appdata
            {
                float4 vertex : POSITION;
                float2 uv : TEXCOORD0;
                UNITY_VERTEX_INPUT_INSTANCE_ID 
            };

            struct v2f
            {
                float2 uv : TEXCOORD0; 
                UNITY_FOG_COORDS(1)
                float4 vertex : SV_POSITION;
                half2 uvDistTex : TEXCOORD3; 
                UNITY_VERTEX_INPUT_INSTANCE_ID 
            };

            sampler2D _MainTex; float4 _MainTex_ST;
            sampler2D _DistortTex; float4 _DistortTex_ST;
            half _DistortTexXSpeed;
            half _DistortTexYSpeed;
            half _DistortAmount;
            float4 _BaseColor;

            UNITY_INSTANCING_BUFFER_START(Props)
                UNITY_DEFINE_INSTANCED_PROP(float, _RandomSeed)
            UNITY_INSTANCING_BUFFER_END(Props)
            
            v2f vert (appdata v)
            {
                v2f o;
                UNITY_SETUP_INSTANCE_ID(v);
                UNITY_TRANSFER_INSTANCE_ID(v, o);
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = TRANSFORM_TEX(v.uv, _MainTex);
                o.uvDistTex = TRANSFORM_TEX(v.uv, _DistortTex); // 基础扰动 UV
                UNITY_TRANSFER_FOG(o,o.vertex);
                return o;
            }

            // --- 片元着色器---
            fixed4 frag (v2f i) : SV_Target
            {
                UNITY_SETUP_INSTANCE_ID(i);
                half randomSeed = UNITY_ACCESS_INSTANCED_PROP(Props, _RandomSeed);

                // ==========================================================
                // 核心修改：双重采样消除重复感
                // ==========================================================
                
                // 1. 准备时间变量
                float timeVal = _Time.x + randomSeed;

                // --- 第一层噪声 (Layer 1) ---
                // 保持原本的逻辑：按照设定速度移动
                float2 distUV1 = i.uvDistTex;
                distUV1.x += (timeVal * _DistortTexXSpeed) % 1;
                distUV1.y += (timeVal * _DistortTexYSpeed) % 1;
                
                // --- 第二层噪声 (Layer 2) ---
                // 技巧：把 UV 乘以一个非整数（比如 0.73），并让它反向移动
                // 这样两层噪声永远不会“同步”，极大地打破了网格感
                float2 distUV2 = i.uvDistTex * 0.73; 
                distUV2.x -= (timeVal * _DistortTexXSpeed * 0.5) % 1; // 速度慢一半，且反向
                distUV2.y -= (timeVal * _DistortTexYSpeed * 0.5) % 1;

                // 采样两次噪声图
                half noise1 = tex2D(_DistortTex, distUV1).r;
                half noise2 = tex2D(_DistortTex, distUV2).r;

                // 混合噪声：取平均值
                // (R - 0.5) 是为了把范围归一化到 [-0.5, 0.5] 方便做位移
                half finalNoise = ((noise1 + noise2) * 0.5 - 0.5) * 0.2 * _DistortAmount;

                // ==========================================================

                // --- 应用扰动到主纹理 ---
                float2 finalUV = i.uv;
                finalUV.x += finalNoise;
                finalUV.y += finalNoise;

                fixed4 col = tex2D(_MainTex, finalUV);
                col *= _BaseColor;
                
                UNITY_APPLY_FOG(i.fogCoord, col);

                return col;
            }
            ENDCG
        }
    }
}