#define HIGH_PRECISION_VERTEX

#include "sh_Utils.h"
#include "sh_Masking.h"
#include "sh_TextureWrapping.h"
#include "sh_CircularProgressUtils.h"

layout(location = 2) in highp vec2 v_TexCoord;

layout(set = 1, binding = 0) uniform lowp texture2D m_Texture;
layout(set = 1, binding = 1) uniform lowp sampler m_Sampler;

layout(location = 0) out vec4 o_Colour;

uniform mediump float progress;
uniform mediump float innerRadius;
uniform bool roundedCaps;

void main(void)
{
    if (progress == 0.0 || innerRadius == 0.0)
    {
        o_Colour = vec4(0.0);
        return;
    }

    highp vec2 resolution = v_TexRect.zw - v_TexRect.xy;
    highp vec2 pixelPos = v_TexCoord / resolution;
    
    if (!insideProgress(pixelPos, progress, innerRadius, roundedCaps))
    {
        o_Colour = vec4(0.0);
        return;
    }

    highp vec2 wrappedCoord = wrap(v_TexCoord, v_TexRect);
    o_Colour = getRoundedColor(toSRGB(wrappedTexture(wrappedCoord, v_TexRect, m_Texture, m_Sampler, -0.9)), wrappedCoord);
}
