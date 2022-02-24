#include "sh_Utils.h"

layout(location = 0) in highp vec2 v_MaskingPosition;
layout(location = 1) in lowp vec4 v_Colour;
layout(location = 2) in mediump vec2 v_TexCoord;
layout(location = 3) in mediump vec4 v_TexRect;
layout(location = 4) in mediump vec2 v_BlendRange;

layout(location = 0) out vec4 o_Colour;

void main(void)
{
    float hueValue = v_TexCoord.x / (v_TexRect[2] - v_TexRect[0]);
    o_Colour = hsv2rgb(vec4(hueValue, 1, 1, 1));
}
