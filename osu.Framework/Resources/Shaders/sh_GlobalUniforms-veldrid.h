layout(set = 0, binding = 0) uniform m_GlobalUniforms
{
    mat4 g_ProjMatrix;
    bool g_IsMasking;
    vec4 g_MaskingRect;
    mat3 g_ToMaskingSpace;
    float g_CornerRadius;
    float g_CornerExponent;
    float g_BorderThickness;
    mat4 g_BorderColour;
    float g_MaskingBlendRange;
    float g_AlphaExponent;
    vec2 g_EdgeOffset;
    bool g_DiscardInner;
    float g_InnerCornerRadius;
    bool g_GammaCorrection;
    int g_WrapModeS;
    int g_WrapModeT;
    bool g_BackbufferDraw;
};
