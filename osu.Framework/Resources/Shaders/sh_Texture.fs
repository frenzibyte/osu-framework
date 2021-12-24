#include "sh_Utils.h"
#include "sh_TextureWrapping.h"

layout(location = 1) in lowp vec4 v_Colour;
layout(location = 2) in mediump vec2 v_TexCoord;
layout(location = 3) in mediump vec4 v_TexRect;

uniform lowp texture2D m_Texture;
uniform lowp sampler m_Sampler;

layout(location = 0) out vec4 o_Colour;

void main()
{
    o_Colour = toSRGB(v_Colour * wrappedTexture(wrap(v_TexCoord, v_TexRect), v_TexRect, m_Texture, m_Sampler));
}
