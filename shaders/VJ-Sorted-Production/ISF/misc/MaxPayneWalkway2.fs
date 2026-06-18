/*{
    "DESCRIPTION": "MaxPayneWalkway2",
    "CREDIT": "GLSL Sandbox / various",
    "ISFVSN": "2.0",
    "CATEGORIES": [
        "misc"
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
        "misc"
    ]
}*/



#define time ((useFrameIndex ? (float(FRAMEINDEX) / fps) : TIME) * timeScale)
#define mouse vec2(mouseX, mouseY)
#define resolution RENDERSIZE
#ifdef GL_ES
precision mediump float;
#endif

//#extension GL_OES_standard_derivatives : enable

varying vec2 surfacePosition;
float saturate(float x){return clamp(x,0.,1.);}
vec4 tex(vec2 u){
	vec2 p=fract(fract(u)+(sin(u.y+time)/4.));
	float f=1.-saturate((max(length(p-vec2(0.25,0.5)),length(p-vec2(0.75,0.5)))-.5)*50.);
	f-=1.-saturate((length(p-.5)-(((sin(time+u.x)+2.)/3.)*.25))*50.);
	return vec4(f,f,f,0.);
}

vec4 tex2( vec2 g )
{
    g /= 10.;
    float color = sign((mod(g.x, 0.1) - 0.05) * (mod(g.y, 0.1) - 0.05));
    
    return sqrt(vec4(color));
}

void main()
{
    vec2 uv = surfacePosition * 2.;
    
    float t = time * .5;
    uv.y += sin(t) * .5;
    uv.x += cos(t) * .5;
    float a = atan(uv.x,uv.y)/1.57;
    float d = max(max(abs(uv.x),abs(uv.y)), min(abs(uv.x)+uv.y, length(uv)));
   
    vec2 k = vec2(a,.8/d + t);
    
    vec4 tx = tex(k*6.);
    vec4 tx2 = tex2(k*2.);
    
    // ground
    gl_FragColor = tx2;
    
    // wall
    if (d<=abs(uv.x)+0.05||d<=abs(uv.x)+uv.y)
        gl_FragColor = tx;
    
    gl_FragColor *= d;
	gl_FragColor.a = 1.;
	
}
