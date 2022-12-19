#ifdef OSU_VERTEX_SHADER
    layout(location = 0) in highp vec3 m_Position;
    layout(location = 1) in lowp vec4 m_Colour;
    layout(location = 2) in mediump vec2 m_TexCoord;
#endif

#ifdef OSU_VERTEX_SHADER
    layout(location = 0) out highp vec2 v_MaskingPosition;
    layout(location = 1) out lowp vec4 v_Colour;
    layout(location = 2) out mediump vec2 v_TexCoord;
    layout(location = 3) out mediump vec4 v_TexRect;
    layout(location = 4) out mediump vec2 v_BlendRange;
#else
    layout(location = 0) in highp vec2 v_MaskingPosition;
    layout(location = 1) in lowp vec4 v_Colour;
    layout(location = 2) in mediump vec2 v_TexCoord;
    layout(location = 3) in mediump vec4 v_TexRect;
    layout(location = 4) in mediump vec2 v_BlendRange;
#endif
