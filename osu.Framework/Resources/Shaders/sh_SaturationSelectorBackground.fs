#include "sh_Utils.h"

layout(location = 0) in highp vec2 v_MaskingPosition;
layout(location = 1) in lowp vec4 v_Colour;
layout(location = 2) in mediump vec2 v_TexCoord;
layout(location = 3) in mediump vec4 v_TexRect;
layout(location = 4) in mediump vec2 v_BlendRange;

uniform mediump float hue;

layout(location = 0) out vec4 o_Colour; 

void main(void)
{
    vec2 resolution = vec2(v_TexRect[2] - v_TexRect[0], v_TexRect[3] - v_TexRect[1]);
    vec2 pixelPos = v_TexCoord / resolution;
    o_Colour = hsv2rgb(vec4(hue, pixelPos.x, 1.0 - pixelPos.y, 1.0));
}
