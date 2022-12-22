﻿#define HIGH_PRECISION_VERTEX

#include "sh_Texture2D_VertexAttributes.h"

#include "sh_Utils.h"
#include "sh_Masking.h"
#include "sh_TextureWrapping.h"
#include "sh_CircularBlobUtils.h"

layout(set = 1, binding = 1) uniform m_FragmentUniforms
{
    mediump float innerRadius;
    mediump float frequency;
    mediump float amplitude;
    highp vec2 noisePosition;
    highp float texelSize;
};

layout(set = 2, binding = 0) uniform lowp texture2D m_Texture;
layout(set = 2, binding = 1) uniform lowp sampler m_Sampler;

layout(location = 0) out vec4 o_Colour;


void main(void)
{
    highp vec2 resolution = v_TexRect.zw - v_TexRect.xy;
    highp vec2 pixelPos = v_TexCoord / resolution;

    highp vec2 wrappedCoord = wrap(v_TexCoord, v_TexRect);
    lowp vec4 textureColour = getRoundedColor(wrappedTexture(wrappedCoord, v_TexRect, m_Texture, m_Sampler, -0.9), wrappedCoord);

    o_Colour = vec4(textureColour.rgb, textureColour.a * blobAlphaAt(pixelPos, innerRadius, texelSize, frequency, amplitude, noisePosition));
}
