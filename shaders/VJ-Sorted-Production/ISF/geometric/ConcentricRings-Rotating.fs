/*{
    "DESCRIPTION": "ConcentricRings-Rotating",
    "CREDIT": "GLSL Sandbox / various",
    "ISFVSN": "2.0",
    "CATEGORIES": [
        "geometric"
    ],
    "INPUTS": [
        {
            "NAME": "useFrameIndex",
            "TYPE": "bool",
            "DEFAULT": 0,
            "LABEL": "Use frame index (timeline sync)"
        },
        {
            "NAME": "fps",
            "TYPE": "float",
            "DEFAULT": 60.0,
            "MIN": 24.0,
            "MAX": 120.0
        },
        {
            "NAME": "timeScale",
            "TYPE": "float",
            "DEFAULT": 1.0,
            "MIN": 0.1,
            "MAX": 4.0,
            "LABEL": "Time speed"
        },
        {
            "NAME": "mouseX",
            "TYPE": "float",
            "DEFAULT": 0.5,
            "MIN": 0.0,
            "MAX": 1.0,
            "LABEL": "Mouse X"
        },
        {
            "NAME": "mouseY",
            "TYPE": "float",
            "DEFAULT": 0.5,
            "MIN": 0.0,
            "MAX": 1.0,
            "LABEL": "Mouse Y"
        }
    ],
    "TAGS": [
        "geometric"
    ]
}*/




#define time ((useFrameIndex ? (float(FRAMEINDEX) / fps) : TIME) * timeScale)
#define mouse vec2(mouseX, mouseY)
#define resolution RENDERSIZE
#ifdef GL_ES
precision mediump float;
#endif

float Mod(float a, float b)
{
    return a - fract(a / b) * b;
}
float RectFunc(float x , float a , float b)
{
    return step(x , b) - step(x , a);
}

void main( void ) 
{
    vec2 uv = gl_FragCoord.xy ;

    float theta = - time * .7;
    mat2 rot = mat2(cos(theta),-sin(theta),sin(theta),cos(theta));	
	
    uv = (uv - resolution * 0.5 ) / max(resolution.x,resolution.y);
	
    float w = 0.05;
    float r = 0.09;       
    float c = 0.0;
   
    //triangle
    float size = 1.0;
    vec2 uv2 = uv;
    uv2.x = uv.x - 0.21 *  fract( time * 1.1) * 0.2 + 0.2;
    c += step(0.4 ,uv2.x) * step(abs(uv2.y) , - uv2.x + 0.49);
   
    uv = rot * uv;

    //line
    float lw = 0.04;
    float lh =-0.04;
    
    float line = step(uv.y, lh + lw * 0.5) - step(uv.y, lh - lw * 0.5);
    float inner = (step(0.1,uv.x) + step(uv.x , -0.1) );
    float outer = 1.0 - (step(uv.x , -0.18) + step(0.18 ,  uv.x) );
    c += line * inner * outer;
    
    //ring
    if(abs(uv.y) > 0.02)
    {
	    c += step(length(uv) , r + w) - step(length(uv),r);
    }
	
    gl_FragColor = vec4(vec3(0.0,1.,.9) * c,1.0);
}
