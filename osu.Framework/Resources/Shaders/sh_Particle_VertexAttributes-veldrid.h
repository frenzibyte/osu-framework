#ifdef OSU_VERTEX_SHADER
    layout(location = 0) in vec2 m_Position;
    layout(location = 1) in vec4 m_Colour;
    layout(location = 2) in vec2 m_TexCoord;
    layout(location = 3) in float m_Time;
    layout(location = 4) in vec2 m_Direction;
#endif

#ifdef OSU_VERTEX_SHADER
    layout(location = 0) out vec4 v_Colour;
    layout(location = 1) out vec2 v_TexCoord;
#else
    layout(location = 0) in vec4 v_Colour;
    layout(location = 1) in vec2 v_TexCoord;
#endif
