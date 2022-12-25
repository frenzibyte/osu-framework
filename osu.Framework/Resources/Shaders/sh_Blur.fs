#include "sh_Utils.h"

#define INV_SQRT_2PI 0.39894

varying highp vec2 v_TexCoord;

uniform lowp sampler2D m_Sampler;

uniform highp vec2 g_TexSize;
uniform int g_Radius;
uniform highp float g_Sigma;
uniform highp vec2 g_BlurDirection;

highp float computeGauss(in highp float x, in highp float sigma)
{
	return INV_SQRT_2PI * exp(-0.5*x*x / (sigma*sigma)) / sigma;
}

lowp vec4 blur(sampler2D tex, int radius, highp vec2 direction, highp vec2 texCoord, highp vec2 texSize, highp float sigma)
{
	highp float factor = computeGauss(0.0, sigma);
	highp vec4 sum = texture2D(tex, texCoord) * factor;

	highp float totalFactor = factor;

	for (int i = 2; i <= 200; i += 2)
	{
		highp float x = float(i) - 0.5;
		factor = computeGauss(x, sigma) * 2.0;
		totalFactor += 2.0 * factor;
		sum += texture2D(tex, texCoord + direction * x / texSize) * factor;
		sum += texture2D(tex, texCoord - direction * x / texSize) * factor;
		if (i >= radius)
			break;
	}

    return toSRGB(sum / totalFactor);
}

void main(void)
{
	gl_FragColor = blur(m_Sampler, g_Radius, g_BlurDirection, v_TexCoord, g_TexSize, g_Sigma);
}
