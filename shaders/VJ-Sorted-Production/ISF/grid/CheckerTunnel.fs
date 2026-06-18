/*{
    "DESCRIPTION": "CheckerTunnel",
    "CREDIT": "GLSL Sandbox / various",
    "ISFVSN": "2.0",
    "CATEGORIES": [
        "grid"
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
        "grid"
    ]
}*/



#define time ((useFrameIndex ? (float(FRAMEINDEX) / fps) : TIME) * timeScale)
#define mouse vec2(mouseX, mouseY)
#define resolution RENDERSIZE
#ifdef GL_ES
precision mediump float;
#endif

#extension GL_OES_standard_derivatives : enable

varying vec2 surfacePosition; 

vec4 tex( vec2 g )
{
	vec2 p = g, a; vec4 f = vec4(9.);
    
    for(int x=-3;x<=3;x++)
    for(int y=-3;y<=3;y++)
        p = vec2(x,y),
       	a = sin( time*3.5 + 9. * fract(sin((floor(g)+p)*mat2(2,5,5,2)))),
        p += .5 + .5 * a - fract(g),
        f = min(f, length(p+p * cos(a.x*1.5)));
    
    return sqrt(0.5-vec4(10,10,1,1)*f);
}

vec4 tex2( vec2 g )
{
    g /= 4.;
    float color = sign((mod(g.x, 0.1) - 0.05) * (mod(g.y, 0.1) - 0.05));
    
    return sqrt(vec4(color));
}

void main()
{
    vec2 uv = surfacePosition * 2.5;
    
    float t = time * 0.7;
    uv.y += 0.0;//sin(t) * .5;
    uv.x += 0.0;//cos(t) * .5;
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
