// This file is automatically included in every shader

#ifndef GL_ES
    #define lowp
    #define highp
    #define highp
#else
    // GL_ES expects a defined precision for every member. Users may miss this requirement, so a default precision is specified.
    precision highp float;
#endif
