﻿#define GAMMA 2.4

// Perform alpha compositing of two colour components. Assumed both are linear with premultiplied alpha.
// The linearity assumption is sometimes broken in practice (IIRC because it produces nicer looking blends
// than the physically correct linear blend), but alpha premultiplication must always be satisfied.
lowp vec4 blend(lowp vec4 src, lowp vec4 dst)
{
    return src + dst * (1.0 - src.a);
}

lowp vec4 toPremultipliedAlpha(lowp vec4 colour)
{
    return vec4(colour.rgb * colour.a, colour.a);
}

lowp vec4 toEmissive(lowp vec4 colour, bool isEmissive)
{
    return vec4(colour.rgb, isEmissive ? 0.0 : colour.a);
}

// http://lolengine.net/blog/2013/07/27/rgb-to-hsv-in-glsl
// slightly amended to also handle alpha
vec4 hsv2rgb(vec4 c)
{
    vec4 K = vec4(1.0, 2.0 / 3.0, 1.0 / 3.0, 3.0);
    vec3 p = abs(fract(c.xxx + K.xyz) * 6.0 - K.www);
    return vec4(c.z * mix(K.xxx, clamp(p - K.xxx, 0.0, 1.0), c.y), c.w);
}