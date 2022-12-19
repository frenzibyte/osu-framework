#ifdef OSU_VERTEX_SHADER
    layout(location = 0) in vec2 m_Position;
#endif

#ifdef OSU_VERTEX_SHADER
    layout(location = 0) out vec4 v_Position;
#else
    layout(location = 0) in vec4 v_Position;
#endif
