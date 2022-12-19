#define HIGH_PRECISION_VERTEX

#include "sh_Texture2D_VertexAttributes.h"

#include "sh_Utils.h"
#include "sh_Masking.h"

void main(void)
{
    highp float hueValue = v_TexCoord.x / (v_TexRect[2] - v_TexRect[0]);
    gl_FragColor = getRoundedColor(toLinear(hsv2rgb(vec4(hueValue, 1, 1, 1))), v_TexCoord);
}
