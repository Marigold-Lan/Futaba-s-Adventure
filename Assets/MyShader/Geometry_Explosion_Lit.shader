Shader "MyShaders/Explosion_Physics"
{
    Properties
    {
        _MainTex ("Texture", 2D) = "white" {}
        _Color ("Color", Color) = (1,1,1,1)
        
        [Header(Explosion Settings)]
        // 0 = 完好, 1 = 炸裂结束
        _Progress ("Explosion Progress", Range(0, 1)) = 0 
        
        _Force ("Explosion Force", Range(0, 50)) = 10 // 炸开的力度（飞多远）
        _Gravity ("Gravity", Range(0, 50)) = 10       // 下坠的重力
        _Spin ("Spin Speed", Range(0, 10)) = 3        // 碎片自转速度
        
        [Toggle] _UseClip("Fade Out at End", Float) = 1 // 最后是否消失
    }
    SubShader
    {
        Tags { "RenderType"="Opaque" "Cull"="Off" } // 关闭剔除，为了看到碎片背面
        LOD 100

        Pass
        {
            Tags { "LightMode"="ForwardBase" } // 支持基本光照

            CGPROGRAM
            #pragma vertex vert
            #pragma geometry geom 
            #pragma fragment frag
            #include "UnityCG.cginc"
            #include "UnityLightingCommon.cginc" 

            struct appdata
            {
                float4 vertex : POSITION;
                float2 uv : TEXCOORD0;
                float3 normal : NORMAL;
            };

            struct v2g
            {
                float4 vertex : SV_POSITION; // 这里暂存模型空间坐标
                float2 uv : TEXCOORD0;
                float3 normal : NORMAL;
            };

            struct g2f
            {
                float4 vertex : SV_POSITION;
                float2 uv : TEXCOORD0;
                float3 worldNormal : TEXCOORD1; // 用于简单光照
            };

            sampler2D _MainTex; float4 _MainTex_ST;
            fixed4 _Color;
            float _Progress;
            float _Force;
            float _Gravity;
            float _Spin;
            float _UseClip;

            // --- 随机函数 (根据坐标生成随机数) ---
            float random(float2 uv)
            {
                return frac(sin(dot(uv, float2(12.9898, 78.233))) * 43758.5453123);
            }

            // --- 旋转矩阵 (绕任意轴旋转) ---
            float3x3 AngleAxis3x3(float angle, float3 axis)
            {
                float c, s;
                sincos(angle, s, c);
                float t = 1 - c;
                float x = axis.x;
                float y = axis.y;
                float z = axis.z;
                return float3x3(
                    t * x * x + c, t * x * y - s * z, t * x * z + s * y,
                    t * x * y + s * z, t * y * y + c, t * y * z - s * x,
                    t * x * z - s * y, t * y * z + s * x, t * z * z + c
                );
            }

            v2g vert (appdata v)
            {
                v2g o;
                o.vertex = v.vertex; 
                o.uv = TRANSFORM_TEX(v.uv, _MainTex);
                o.normal = v.normal;
                return o;
            }

            [maxvertexcount(3)] 
            void geom(triangle v2g input[3], inout TriangleStream<g2f> triStream)
            {
                g2f o;

                // 1. 计算三角形中心 (作为碎片的质心)
                float3 center = (input[0].vertex + input[1].vertex + input[2].vertex).xyz / 3;
                
                // 2. 计算面法线 (碎片飞出的方向)
                float3 v1 = input[1].vertex - input[0].vertex;
                float3 v2 = input[2].vertex - input[0].vertex;
                float3 faceNormal = normalize(cross(v1, v2));

                // 3. 生成每个碎片的随机特征
                float r = random(center.xy); // 随机种子
                float3 randomAxis = normalize(float3(r, r*2-1, 1-r)); // 随机旋转轴

                // --- 物理模拟核心公式 ---
                // 位移 = 方向 * 力度 * 时间
                float3 flyOffset = faceNormal * _Force * _Progress;
                // 下坠 = (0, -1, 0) * 重力 * 时间的平方 (模拟抛物线加速)
                float3 gravityOffset = float3(0, -1, 0) * _Gravity * _Progress * _Progress;
                
                // 总位移
                float3 totalOffset = flyOffset + gravityOffset;

                for(int i = 0; i < 3; i++)
                {
                    float3 vPos = input[i].vertex.xyz;

                    // A. 先让顶点相对于中心点旋转 (自转)
                    // 只有当进度 > 0 时才旋转，否则保持原样
                    if (_Progress > 0)
                    {
                        vPos -= center; // 移回原点
                        vPos = mul(AngleAxis3x3(_Progress * _Spin * (10 + r * 10), randomAxis), vPos); // 旋转
                        vPos += center; // 移回原位
                    }

                    // B. 加上整体位移 (抛物线飞行)
                    vPos += totalOffset;

                    // C. 缩放 (可选：在最后阶段稍微变小一点，避免穿帮太严重，但前期保持大块)
                    // 如果 _Progress > 0.8，开始快速缩小
                    float scale = 1;
                    if (_Progress > 0.7) scale = 1 - (_Progress - 0.7) / 0.3;
                    vPos = lerp(center + totalOffset, vPos, scale);

                    // 转到裁切空间
                    o.vertex = UnityObjectToClipPos(float4(vPos, 1));
                    o.uv = input[i].uv;
                    o.worldNormal = UnityObjectToWorldNormal(input[i].normal); // 简单光照
                    triStream.Append(o);
                }
                triStream.RestartStrip();
            }

            fixed4 frag (g2f i) : SV_Target
            {
                fixed4 col = tex2D(_MainTex, i.uv) * _Color;
                
                // 简单兰伯特漫反射 (让碎片有立体感)
                float3 lightDir = normalize(_WorldSpaceLightPos0.xyz);
                float ndotl = max(0, dot(normalize(i.worldNormal), lightDir));
                col.rgb *= (0.5 + ndotl * 0.5); // 半兰伯特，避免背光面全黑

                // 简单的 Fade Out (如果开启)
                if (_UseClip > 0.5 && _Progress > 0.9)
                {
                    // 最后 10% 的时间透明度降低
                    col.a *= (1 - _Progress) * 10; 
                }

                return col;
            }
            ENDCG
        }
    }
}