#ifndef TEXTURE_FS
#define TEXTURE_FS

#include "sh_Utils.h"

layout(location = 2) in mediump vec2 v_TexCoord;

layout(location = 0) out vec4 o_Colour;

void main(void) 
{
    o_Colour = vec4(1.0, 1.0, 1.0, 1.0);
}

#endif