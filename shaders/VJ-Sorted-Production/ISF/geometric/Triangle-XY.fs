/*{
    "DESCRIPTION": "Triangle-XY",
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
// http://glslsandbox.com/e#12228.1
// Triangle bits
#ifdef GL_ES
precision mediump float;
#endif

vec3 Line(vec2 p1,vec2 p2,float r,vec2 px)
{
	float c = 0.0;
	vec2 n = normalize((p2-p1).yx)*vec2(-1,1);	
	vec2 d = normalize((p2-p1));	
	c = 1.0 - abs( dot(n,px-p1) / r );
	c *= clamp( (dot(d,px-p1) * dot(-d,px-p2)) * 0.1 , 0.0, 1.0);
	c = clamp(c, 0.0, 1.0);	
	return vec3(c);
}

void main( void ) {

	vec2 p = ( gl_FragCoord.xy );
	
	vec2 m = mouse*resolution;

	vec3 c = vec3(0.0);
	
	vec2 p1 = resolution/2.;
	vec2 p2 = m;
	
	vec2 mid = p1-(p1-p2)/2.0;
	vec2 perp = mid+((p2-p1).yx)*vec2(-1,1);
	
	c = Line(p1,p2,1.5,p);
	c += Line(mid,perp,1.5,p)*vec3(0,1,1);
	c += Line(p1,perp,1.5,p)*vec3(1,0,1);
	c += Line(p2,perp,1.5,p)*vec3(1,0,1);

	gl_FragColor = vec4( vec3( c ), 1.0 );

}
