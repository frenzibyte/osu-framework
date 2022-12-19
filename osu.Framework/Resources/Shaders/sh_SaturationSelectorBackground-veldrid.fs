#define HIGH_PRECISION_VERTEX

#include "sh_Texture2D_VertexAttributes.h"

#include "sh_Utils.h"
#include "sh_Masking.h"

uniform mediump float hue;

layout(location = 0) out vec4 o_Colour;

void main(void)
{
    highp vec2 resolution = v_TexRect.zw - v_TexRect.xy;
    highp vec2 pixelPos = v_TexCoord / resolution;
    o_Colour = getRoundedColor(toLinear(hsv2rgb(vec4(hue, pixelPos.x, 1.0 - pixelPos.y, 1.0))), v_TexCoord);
}
