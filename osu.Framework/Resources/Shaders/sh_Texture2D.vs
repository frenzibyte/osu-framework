#include "sh_Utils.h"

layout(location = 0) in vec2 m_Position;
layout(location = 1) in vec4 m_Colour;
layout(location = 2) in vec2 m_TexCoord;
layout(location = 3) in vec4 m_TexRect;
layout(location = 4) in vec2 m_BlendRange;

layout(location = 0) out vec2 v_MaskingPosition;
layout(location = 1) out vec4 v_Colour;
layout(location = 2) out vec2 v_TexCoord;
layout(location = 3) out vec4 v_TexRect;
layout(location = 4) out vec2 v_BlendRange;

uniform mat4 g_ProjMatrix;
uniform mat3 g_ToMaskingSpace;

void main()
{
	// Transform from screen space to masking space.
	highp vec3 maskingPos = g_ToMaskingSpace * vec3(m_Position, 1.0);
	v_MaskingPosition = maskingPos.xy / maskingPos.z;

	v_Colour = m_Colour;
	v_TexCoord = m_TexCoord;
	v_TexRect = m_TexRect;
	v_BlendRange = m_BlendRange;

    //gl_Position.x = g_ProjMatrix[0].x * m_Position.x + g_ProjMatrix[1].x * m_Position.y + g_ProjMatrix[2].x + g_ProjMatrix[3].x;
    //gl_Position.y = g_ProjMatrix[0].y * m_Position.x + g_ProjMatrix[1].y * m_Position.y + g_ProjMatrix[2].y + g_ProjMatrix[3].y;
    //gl_Position.z = g_ProjMatrix[0].z * m_Position.x + g_ProjMatrix[1].z * m_Position.y + g_ProjMatrix[2].z + g_ProjMatrix[3].z;
    //gl_Position.w = g_ProjMatrix[0].w * m_Position.x + g_ProjMatrix[1].w * m_Position.y + g_ProjMatrix[2].w + g_ProjMatrix[3].w;
    gl_Position = g_ProjMatrix * vec4(m_Position, 1.0, 1.0);
}
