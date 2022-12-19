#include "sh_Texture2D_VertexAttributes.h"

#include "sh_Utils.h"

#define INV_SQRT_2PI 0.39894

layout(set = 1, binding = 0) uniform texture2D m_Texture;
layout(set = 1, binding = 1) uniform sampler m_Sampler;

uniform mediump vec2 texSize;
uniform int radius;
uniform mediump float sigma;
uniform highp vec2 blurDirection;

layout(location = 0) out vec4 o_Colour;

mediump float computeGauss(in mediump float x, in mediump float sigma)
{
	return INV_SQRT_2PI * exp(-0.5*x*x / (sigma*sigma)) / sigma;
}

lowp vec4 blur(texture2D tex, sampler samp, int radius, highp vec2 direction, mediump vec2 texCoord, mediump vec2 texSize, mediump float sigma)
{
	mediump float factor = computeGauss(0.0, sigma);
	mediump vec4 sum = texture(sampler2D(tex, samp), texCoord) * factor;

	mediump float totalFactor = factor;

	for (int i = 2; i <= 200; i += 2)
	{
		mediump float x = float(i) - 0.5;
		factor = computeGauss(x, sigma) * 2.0;
		totalFactor += 2.0 * factor;
		sum += texture(sampler2D(tex, samp), texCoord + direction * x / texSize) * factor;
		sum += texture(sampler2D(tex, samp), texCoord - direction * x / texSize) * factor;
		if (i >= radius)
			break;
	}

    return toSRGB(sum / totalFactor);
}

void main(void)
{
	o_Colour = blur(m_Texture, m_Sampler, radius, blurDirection, v_TexCoord, texSize, sigma);
}
