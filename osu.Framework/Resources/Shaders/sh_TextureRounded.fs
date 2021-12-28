#include "sh_Utils.h"
#include "sh_Masking.h"
#include "sh_TextureWrapping.h"

layout(location = 2) in mediump vec2 v_TexCoord;

uniform lowp texture2D m_Texture;
uniform lowp sampler m_Sampler;

layout(location = 0) out vec4 o_Colour;

void main(void) 
{
    vec2 wrappedCoord = wrap(v_TexCoord, v_TexRect);
    o_Colour = getRoundedColor(toSRGB(wrappedTexture(wrappedCoord, v_TexRect, m_Texture, m_Sampler)), wrappedCoord);
}
