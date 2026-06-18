/*{
    "DESCRIPTION": "SmashNoise",
    "CREDIT": "GLSL Sandbox / various",
    "ISFVSN": "2.0",
    "CATEGORIES": [
        "noise"
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
        }
    ],
    "TAGS": [
        "geometric",
        "noise",
        "3d",
        "texture-input"
    ]
}*/



#define time ((useFrameIndex ? (float(FRAMEINDEX) / fps) : TIME))
#define mouse vec2(0.0)
#define resolution RENDERSIZE
#ifdef GL_ES
precision mediump float;
#endif

uniform sampler2D prevScene;

float sdEllipsoid( in vec3 p, in vec3 r )
{
    return (length( p/r ) - 1.0) * min(min(r.x,r.y),r.z);
}

float sdInstanceEllipsoid(in vec3 p)
{
    return sdEllipsoid(p, vec3(0.1+0.9*cos(time)*cos(time*2.0),0.1+0.9*cos(time)*sin(time*2.0),0.1+0.9*sin(time)));
}

float distanceHub(vec3 p){
    return sdInstanceEllipsoid(p);
}

vec3 genNormal(vec3 p){
	float d = 0.001;
	return normalize(vec3(
		distanceHub(p + vec3(	d, 0.0, 0.0)) - distanceHub(p + vec3( -d, 0.0, 0.0)),
		distanceHub(p + vec3(0.0,	 d, 0.0)) - distanceHub(p + vec3(0.0,	-d, 0.0)),
		distanceHub(p + vec3(0.0, 0.0,	 d)) - distanceHub(p + vec3(0.0, 0.0,	-d))
	));
}

vec3 doColor(vec3 p){
	float e = 0.001;
	if (sdInstanceEllipsoid(p)<e){
		vec3 normal	= genNormal(p);
		vec3 light	 = normalize(vec3(1.0, 1.0, 1.0));
		float diff	 = max(dot(normal, light), 0.1);
		return vec3(diff*cos(time)*cos(2.0*time), diff*cos(time)*sin(2.0*time), diff*sin(time));
	}
	return vec3(0.0);
}

void main(){
	vec2 p = (gl_FragCoord.xy * 2.0 - resolution) / min(resolution.x, resolution.y);
	vec3 cPos  = vec3(0.0,	0.0,	3.0);
	vec3 cDir  = vec3(0.0,	0.0, -1.0);  
	vec3 cUp   = vec3(0.0,	1.0,	0.0);
	vec3 cSide = cross(cDir, cUp);       
	float targetDepth = 1.0;             

	vec3 ray = normalize(cSide * p.x + cUp * p.y + cDir * targetDepth);
	float dist = 0.0;	float rLen = 0.0;	vec3 rPos = cPos;

	for(int i = 0; i < 32; ++i){
		dist = distanceHub(rPos);
		rLen += dist;
		rPos = cPos + ray * rLen;
	}

	vec3 color = doColor(rPos);
	gl_FragColor = vec4(color, 1.0);
}

