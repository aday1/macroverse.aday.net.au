/*{
    "DESCRIPTION": "Basicfractal",
    "CREDIT": "GLSL Sandbox / various",
    "ISFVSN": "2.0",
    "CATEGORIES": [
        "fractal"
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
        "fractal"
    ]
}*/



#define time ((useFrameIndex ? (float(FRAMEINDEX) / fps) : TIME) * timeScale)
#define mouse vec2(mouseX, mouseY)
#define resolution RENDERSIZE
// Basic fractal by @paulofalcao
// Forked by ddrcoder
// Forked again
// Adapted from https://www.shadertoy.com/view/Xl2XDm by J.

#ifdef GL_ES
precision mediump float;
#endif

const int maxIterations= 8;//a nice value for fullscreen is 8

float tri( float x ){
	return abs(fract(x)-.5)*2.;
}

mat2 rmat(in float t)
{
	float c = cos(t);
	float s = sin(t);
	return mat2(c, s, -s, c);	
}

mat3 rmat(in vec3 r)
{
	vec3 a = vec3(cos(r.x)*cos(r.y),sin(r.y),sin(r.x)*cos(r.y));
	
	float c = cos(r.z);
	float s = sin(r.z);
	vec3 as  = a*s;
	vec3 ac  = a*a*(1.- c);
	vec3 ad  = a.yzx*a.zxy*(1.-c);
	mat3 rot = mat3(
		c    + ac.x, 
		ad.z - as.z, 
        	ad.y + as.y,
		ad.z + as.z, 
		c    + ac.y, 
		ad.x - as.x,
		ad.y - as.y, 
		ad.x + as.x, 
		c    + ac.z);
	return rot;	
}

void main(void){
	//normalize stuff
	vec2 size 	= resolution.xy;
    	vec2 uv 	= -.5 * (size - 2.0 * gl_FragCoord.xy) / size.x;
	
	uv.y		/= 1.73205080757;
	vec3 uvw	= vec3(uv.y - uv.x, uv.y + uv.x, -(uv.y * 2.));
	uvw		*= .86602540358;
	
	vec3 o		= uvw;
	
    	float t 	= time * .05;
	float tau	= (8. * atan(1.));
	vec2 m 		= (mouse-.5) * tau;
	
//	vec3 r 		= vec3(t) * vec3(.3, .5, .17);
	vec3 r 		= vec3(m.x, tau/2., m.y);

	//global rotation and zoom
	mat3 rm3	= rmat(r);
	uvw		*= tau/2.;
	
	//mirror, rotate and scale N times...
	float s		= 1./3.;
	
	vec3 d		= vec3(uvw);
	
	float w 	= 2./size.x;
	vec3 color	= vec3(0.);

	for(int i=0;i<maxIterations;i++)
	{
		uvw	= abs(uvw)-s;
		uvw 	*= rm3;
		
		//uvw	= uvw/dot(uvw, uvw);
			
		d	= normalize(o-uvw);
		o 	= uvw;
		s	*= .5;
		
		//color	+= float(fract(uvw.x)<w^^fract(uvw.y)<w^^fract(uvw.z)<w);
		color += smoothstep(1.-w*4.,1.,vec3(tri(uvw.x),tri(uvw.y),tri(uvw.z)));
	}
	//color = normalize(color);	
	//color *= normalize(uvw);
	
	gl_FragColor = vec4(color*2., 1.);
}
