#include "sh_Utils.h"
#include "sh_yuv2rgb.h"

layout(location = 1) in lowp vec4 v_Colour;
layout(location = 2) in mediump vec2 v_TexCoord;
layout(location = 3) in mediump vec4 v_TexRect;

layout(location = 0) out vec4 o_Colour;

void main() 
{
    o_Colour = toSRGB(v_Colour) * wrappedSamplerRgb(wrap(v_TexCoord, v_TexRect), v_TexRect, 0.0);
}