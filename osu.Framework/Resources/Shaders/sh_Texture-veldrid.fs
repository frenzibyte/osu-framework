#include "sh_Texture2D_VertexAttributes.h"

#include "sh_Utils.h"
#include "sh_Masking.h"
#include "sh_TextureWrapping.h"

layout(set = 2, binding = 0) uniform lowp texture2D m_Texture;
layout(set = 2, binding = 1) uniform lowp sampler m_Sampler;

layout(location = 0) out vec4 o_Colour;

void main(void) 
{
    vec2 wrappedCoord = wrap(v_TexCoord, v_TexRect);
    o_Colour = getRoundedColor(toSRGB(wrappedTexture(wrappedCoord, v_TexRect, m_Texture, m_Sampler, -0.9)), wrappedCoord);
}
