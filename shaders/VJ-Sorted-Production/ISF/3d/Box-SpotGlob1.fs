/*{
    "DESCRIPTION": "Box-SpotGlob1",
    "CREDIT": "GLSL Sandbox / various",
    "ISFVSN": "2.0",
    "CATEGORIES": [
        "3d"
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
            "NAME": "inputColour",
            "TYPE": "vec4",
            "LABEL": "Input Colour"
        }
    ],
    "TAGS": [
        "3d"
    ]
}*/



#define time ((useFrameIndex ? (float(FRAMEINDEX) / fps) : TIME))
#define mouse vec2(0.0)
#define resolution RENDERSIZE
//https://www.shadertoy.com/view/XsyGDV
// MOUSE.X = COLOR AND SQUARES!!!
// MOUSE.Y = 'alpha (a.k.a dots or lines)'
// inputColour.* NOT USED :(

#ifdef GL_ES
precision mediump float;
#endif

#extension GL_OES_standard_derivatives : enable

uniform vec4 inputColour;

const int LOOPS = 400;
float DEPTH_DIV = 20.;
float SCALE_DIV = 20.;

#define PI 3.1415926535897932384626433832795028

//Functions
vec3 rX(vec3 p, float a) { //YZ
	float c,s;vec3 q=p;
	c = cos(a); s = sin(a);
	p.y = c * q.y - s * q.z;
	p.z = s * q.y + c * q.z;
    return p;
}

vec3 rY(vec3 p, float a) { //XZ
	float c,s;vec3 q=p;
	c = cos(a); s = sin(a);
	p.x = c * q.x + s * q.z;
	p.z = -s * q.x + c * q.z;
    return p;
}

vec3 rZ(vec3 p, float a) { //XY
	float c,s;vec3 q=p;
	c = cos(a); s = sin(a);
	p.x = c * q.x - s * q.y;
	p.y = s * q.x + c * q.y;
    return p;
}

float rand(float co) { return fract(sin(co*(91.3458)) * 47453.5453); }
float rand(vec2 co){ return fract(sin(dot(co.xy ,vec2(12.9898,78.233))) * 43758.5453); }
float rand(vec3 co){ return rand(co.xy+rand(co.z)); }

vec3 hsl2rgb( in vec3 c )
{
    vec3 rgb = clamp( abs(mod(c.x*6.0+vec3(0.0,4.0,2.0),6.0)-3.0)-1.0, 0.0, 1.0 );

    return c.z + c.y * (rgb-0.5)*(1.0-abs(2.0*c.z-1.0));
}

//Map, 1 is wall, 0 is empty.
float map(vec3 p) { 
    p = floor((p+vec3(0.,0.,time))*10.)/10.;
	return float(rand(p)>0.96);
}
#define clamps(x) clamp(x,0.,1.)
float test(vec2 uv) {
    vec2 a = vec2(
        cos(time*.58)*.4
        ,
        sin(time*.52)*.4
        );
    
    vec2 b = vec2(
        cos(time*.55)*.4
        ,
        sin(time*.44)*.4
        );
    
    return clamps((length(uv-a)*length(uv-b))-.05);
}

void main( void ) {
	vec2 uv = (gl_FragCoord.xy / resolution.xy)-.5;
    uv.x *= resolution.x / resolution.y;
    
    //Raycaster / Layering
    vec3 p; //Position
    float a = mouse.y; //"Alpha"
    
    // Change the +1 to something cool for a black hole
    float b = floor(test(uv)*30.)+1.;

    if (mod(floor(gl_FragCoord.x),b)==b-1.&&mod(floor(gl_FragCoord.y),b)==b-1.) {
        for (int i = 0; i < LOOPS; i++) {
            vec3 pos = vec3(uv*((float(i)/SCALE_DIV)+1.),float(i)/DEPTH_DIV);
            if (map(pos) >= mouse.x) {
                a = 1.;
                break; //Pixel doesn't need to be filled anymore. Stop the loop.
            } else {
                p = pos;
            }
        }
    }
    float fog = (p.z*DEPTH_DIV)/float(LOOPS);
    
	gl_FragColor = vec4(vec3(1.-fog)*a*hsl2rgb(vec3((mouse.x+(p.z*.1)),sin((p.x*9.)+mouse.x),1.5+sin((mouse.x*3.)+mouse.x))),0);
}
