#include "sh_Texture2D_VertexAttributes.h"

#include "sh_Utils.h"
#include "sh_Masking.h"
#include "sh_yuv2rgb.h"

layout(location = 0) out vec4 o_Colour;

void main(void)
{
    vec2 wrappedCoord = wrap(v_TexCoord, v_TexRect);
    o_Colour = getRoundedColor(wrappedSamplerRgb(wrappedCoord, v_TexRect, 0.0), wrappedCoord);
}
