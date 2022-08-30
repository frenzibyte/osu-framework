#include "sh_Utils.h"
#include "sh_Masking.h"

layout(location = 2) in mediump vec2 v_TexCoord;

uniform mediump float hue;

layout(location = 0) out vec4 o_Colour;

void main(void)
{
    vec2 resolution = vec2(v_TexRect[2] - v_TexRect[0], v_TexRect[3] - v_TexRect[1]);
    vec2 pixelPos = v_TexCoord / resolution;
    o_Colour = getRoundedColor(hsv2rgb(vec4(hue, pixelPos.x, 1.0 - pixelPos.y, 1.0)), v_TexCoord);
}
