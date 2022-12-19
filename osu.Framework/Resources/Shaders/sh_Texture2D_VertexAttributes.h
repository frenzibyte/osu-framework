#ifdef OSU_VERTEX_SHADER
    attribute highp vec2 m_Position;
    attribute lowp vec4 m_Colour;
    attribute mediump vec2 m_TexCoord;
    attribute mediump vec4 m_TexRect;
    attribute mediump vec2 m_BlendRange;
#endif

varying highp vec2 v_MaskingPosition;
varying lowp vec4 v_Colour;

#ifdef HIGH_PRECISION_VERTEX
    varying highp vec2 v_TexCoord;
    varying highp vec4 v_TexRect;
#else
    varying mediump vec2 v_TexCoord;
    varying mediump vec4 v_TexRect;
#endif

varying mediump vec2 v_BlendRange;
