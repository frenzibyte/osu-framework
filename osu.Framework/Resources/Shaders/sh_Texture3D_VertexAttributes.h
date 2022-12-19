#ifdef OSU_VERTEX_SHADER
    attribute highp vec3 m_Position;
    attribute lowp vec4 m_Colour;
    attribute mediump vec2 m_TexCoord;
#endif

varying highp vec2 v_MaskingPosition;
varying lowp vec4 v_Colour;
varying mediump vec2 v_TexCoord;
varying mediump vec4 v_TexRect;
varying mediump vec2 v_BlendRange;
