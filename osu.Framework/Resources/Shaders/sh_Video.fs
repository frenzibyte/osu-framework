#include "sh_Texture2D_VertexAttributes.h"

#include "sh_Utils.h"
#include "sh_Masking.h"
#include "sh_yuv2rgb.h"

void main(void) 
{
    vec2 wrappedCoord = wrap(v_TexCoord, v_TexRect);
    gl_FragColor = getRoundedColor(toLinear(wrappedSamplerRgb(wrappedCoord, v_TexRect, 0.0)), wrappedCoord);
}
