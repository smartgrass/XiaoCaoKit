Shader "Debug/GLlineZOff" {
        SubShader {
                Blend SrcAlpha OneMinusSrcAlpha
                ZWrite Off
                ZTest Always
                Cull Off
                Pass {
                        BindChannels {
                                Bind "vertex", vertex
                                Bind "color", color
                        }
                }
        }
}