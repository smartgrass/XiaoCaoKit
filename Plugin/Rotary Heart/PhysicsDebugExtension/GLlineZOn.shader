Shader "Debug/GLlineZOn" {
        SubShader {
                Blend SrcAlpha OneMinusSrcAlpha
                ZWrite Off
                Cull Off
                Pass {
                        BindChannels {
                                Bind "vertex", vertex
                                Bind "color", color
                        }
                }
        }
}