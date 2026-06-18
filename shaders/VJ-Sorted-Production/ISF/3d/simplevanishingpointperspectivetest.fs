/*{
    "DESCRIPTION": "simplevanishingpointperspectivetest",
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
        "geometric",
        "3d"
    ]
}*/



#define time ((useFrameIndex ? (float(FRAMEINDEX) / fps) : TIME) * timeScale)
#define mouse vec2(mouseX, mouseY)
#define resolution RENDERSIZE
//simple vanishing point perspective test
//testing camera and rotation
//isometric

#ifdef GL_ES
precision mediump float;
#endif

float pi = atan(1.)*4.;

vec3 camPos = vec3(0,0,0);
vec2 camAng = vec2(0,0);

mat3 rotate(vec2 r) 
{
	mat3 rxmat = mat3(1,   0    ,    0    ,
			  0,cos(r.y),-sin(r.y),
			  0,sin(r.y), cos(r.y));
	mat3 rymat = mat3(cos(r.x), 0,-sin(r.x),
			     0    , 1,    0    ,
			  sin(r.x), 0,cos(r.x));

	return rxmat*rymat;
}

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

float ic = cos(radians(30.));
float is = sin(radians(30.));
vec2 ix = vec2(ic,is);
vec2 iy = vec2(0,1);
vec2 iz = vec2(-ic,is);
	
vec2 ProjectIso(vec3 v)
{	
	return ix*v.x + iy*v.y + iz*v.z;	
}

vec3 Line3d(vec3 p1,vec3 p2,vec2 px)
{	
	mat3 r = rotate(camAng);

	p1 += camPos;
	p2 += camPos;
	p1 *= r;
	p2 *= r;
	
	return Line(ProjectIso(p1),ProjectIso(p2),1.5,px);
}
#define SPIRALINC (1./8.)
void main( void ) {

	vec2 p = ( gl_FragCoord.xy - resolution/2. );
	vec3 m = vec3(mouse*resolution-resolution/2.,0);
	vec3 col = vec3(0.0);
	
	mat3 r = rotate(vec2(0,0));
	
	vec3 v1 = vec3(-64,-64,64);
	vec3 v2 = vec3(64,-64,64);
	vec3 v3 = vec3(64,64,64);
	vec3 v4 = vec3(-64,64,64);
	vec3 v5 = vec3(-64,-64,-64);
	vec3 v6 = vec3(64,-64,-64);
	vec3 v7 = vec3(64,64,-64);
	vec3 v8 = vec3(-64,64,-64);
	
	camPos.y = sin(time)*32.;
	camAng.x = -time*.5;

	col += Line3d(v1,v2,p);
	col += Line3d(v2,v3,p);
	col += Line3d(v3,v4,p);
	col += Line3d(v4,v1,p);
	
	col += Line3d(v5,v6,p);
	col += Line3d(v6,v7,p);
	col += Line3d(v7,v8,p);
	col += Line3d(v8,v5,p);
	
	col += Line3d(v1,v5,p);
	col += Line3d(v2,v6,p);
	col += Line3d(v3,v7,p);
	col += Line3d(v4,v8,p);
	
	for(float i = -8.;i <= 8.;i ++)
	{
		col += Line3d(vec3(-128,-64,i*16.),vec3(128,-64,i*16.),p)*vec3(1,0,0);
		col += Line3d(vec3(i*16.,-64,-128),vec3(i*16.,-64,128),p)*vec3(1,0,0);
	}
	
	float la = (1./16.), a = 0.;
	
	for(float i = 0.;i < 1.;i += SPIRALINC)
	{
		a = (i+SPIRALINC)*pi*2.;
		la = i*pi*2.;
		col += Line3d(vec3(cos(la)*64.,-64.+i*128.,sin(la)*64.),vec3(cos(a)*64.,-64.+(i+SPIRALINC)*128.,sin(a)*64.),p)*vec3(0,1,1);
	}

	gl_FragColor = vec4( col, 1.0 );

}
