Shader "Skybox/Cubemap (Full 3D Rotation)" {
Properties {
    _Tint ("Tint Color", Color) = (.5, .5, .5, .5)
    [Gamma] _Exposure ("Exposure", Float) = 1.0
    _RotationX ("Rotation X (Pitch)", Float) = 0
    _RotationY ("Rotation Y (Yaw)", Float) = 0
    _RotationZ ("Rotation Z (Roll)", Float) = 0
    [NoScaleOffset] _Tex ("Cubemap (HDR)", Cube) = "grey" {}
}

SubShader {
    Tags { "Queue"="Background" "RenderType"="Background" "PreviewType"="Skybox" }
    Cull Off ZWrite Off

    Pass {
        CGPROGRAM
        #pragma vertex vert
        #pragma fragment frag
        #include "UnityCG.cginc"

        samplerCUBE _Tex;
        half4 _Tex_HDR;
        half4 _Tint;
        half _Exposure;
        float _RotationX, _RotationY, _RotationZ;

        // Applies Pitch (X), Yaw (Y), and Roll (Z) rotations
        float3 Rotate3D(float3 vertex, float3 angle) {
            angle = radians(angle);
            
            // X-Axis Rotation (Vertical / Pitch)
            float3x3 rotX = float3x3(
                1, 0, 0,
                0, cos(angle.x), -sin(angle.x),
                0, sin(angle.x), cos(angle.x)
            );
            
            // Y-Axis Rotation (Horizontal / Yaw)
            float3x3 rotY = float3x3(
                cos(angle.y), 0, sin(angle.y),
                0, 1, 0,
                -sin(angle.y), 0, cos(angle.y)
            );

            // Z-Axis Rotation (Roll)
            float3x3 rotZ = float3x3(
                cos(angle.z), -sin(angle.z), 0,
                sin(angle.z), cos(angle.z), 0,
                0, 0, 1
            );

            // Combine rotations: Z * X * Y
            return mul(rotY, mul(rotX, mul(rotZ, vertex)));
        }

        struct appdata_t {
            float4 vertex : POSITION;
        };

        struct v2f {
            float4 vertex : SV_POSITION;
            float3 texcoord : TEXCOORD0;
        };

        v2f vert (appdata_t v) {
            v2f o;
            // Rotate the direction vectors sampling the cubemap
            float3 rotated = Rotate3D(v.vertex.xyz, float3(_RotationX, _RotationY, _RotationZ));
            o.vertex = UnityObjectToClipPos(rotated);
            o.texcoord = v.vertex.xyz;
            return o;
        }

        half4 frag (v2f i) : SV_Target {
            half4 tex = texCUBE(_Tex, i.texcoord);
            half3 c = DecodeHDR(tex, _Tex_HDR);
            c = c * _Tint.rgb * unity_ColorSpaceDouble.rgb;
            c *= _Exposure;
            return half4(c, 1.0);
        }
        ENDCG
    }
}
}