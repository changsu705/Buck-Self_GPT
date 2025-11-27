Shader "Unlit/Unlit Transparent Color With Texture" {
Properties {
    _Color ("Main Color", Color) = (1,1,1,1)
    _MainTex ("Base (RGB)", 2D) = "white" {} // [추가됨] 텍스처 입력 슬롯
}

SubShader {
    Tags {"Queue"="Transparent" "IgnoreProjector"="True" "RenderType"="Transparent"}
    LOD 100
    Fog {Mode Off}

    ZTest Always
    Blend SrcAlpha OneMinusSrcAlpha
    
    // 기존의 단순 Color 명령 대신 Pass 블록 안에서 텍스처 처리를 합니다.
    Pass {
        SetTexture [_MainTex] {
            ConstantColor [_Color]
            // 텍스처(texture)와 설정한 색상(constant)을 곱해서 출력
            Combine texture * constant
        }
    }
}
}