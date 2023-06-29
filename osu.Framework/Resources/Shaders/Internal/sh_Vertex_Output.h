// Automatically included for every vertex shader.

#ifndef INTERNAL_VERTEX_OUTPUT_H
#define INTERNAL_VERTEX_OUTPUT_H

// The -1 is a placeholder value to offset all vertex input members
// of the actual vertex shader during inclusion of this header.
layout(location = -1) in highp float m_BackbufferDrawDepth;

void main()
{
    {{ real_main }}(); // Invoke real main func
}

#endif