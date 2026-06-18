/*{
    "DESCRIPTION": "3D-WindowsScreensaverEsque1",
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
        "3d",
        "texture-input"
    ]
}*/



#define time ((useFrameIndex ? (float(FRAMEINDEX) / fps) : TIME) * timeScale)
#define mouse vec2(mouseX, mouseY)
#define resolution RENDERSIZE
precision mediump float;

uniform sampler2D prevScene;

float dot2( in vec3 v ) { return dot(v,v); }
float udPentagon( vec3 p, vec3 a, vec3 b, vec3 c, vec3 d, vec3 e )
{
	vec3 ba = b - a; vec3 pa = p - a;
	vec3 cb = c - b; vec3 pb = p - b;
	vec3 dc = d - c; vec3 pc = p - c;
	vec3 ad = e - d; vec3 pd = p - d;
	vec3 ae = a - e; vec3 pe = p - e;
	vec3 nor = cross( ba, ad );

	return sqrt(
	(sign(dot(cross(ba,nor),pa)) +
		sign(dot(cross(cb,nor),pb)) +
		sign(dot(cross(dc,nor),pc)) +
		sign(dot(cross(ad,nor),pd)) +
		sign(dot(cross(ae,nor),pe)) < 4.0)
		?
		min( min( min( min(
		dot2(ba*clamp(dot(ba,pa)/dot2(ba),0.0,1.0)-pa),
		dot2(cb*clamp(dot(cb,pb)/dot2(cb),0.0,1.0)-pb) ),
		dot2(dc*clamp(dot(dc,pc)/dot2(dc),0.0,1.0)-pc) ),
		dot2(ad*clamp(dot(ad,pd)/dot2(ad),0.0,1.0)-pd) ),
		dot2(ae*clamp(dot(ae,pe)/dot2(ae),0.0,1.0)-pe) )
		:
		dot(nor,pa)*dot(nor,pa)/dot2(nor)
	);
}

float pentagonToSphere(in vec3 p)
{
	p = mat3(cos(time),-sin(time),0, sin(time), cos(time),0 ,0,0,1)*p;
	return (length(p)-1.5)*abs(sin(time))+udPentagon(p,vec3(1.0, 0.0, 1.0), vec3(0.3090169943749474241023, 0.9510565162951535721164, 1.0), vec3(-0.8090169943749474241023, 0.5877852522924731291687, 1.0), vec3(-0.8090169943749474241023, -0.5877852522924731291687, 1.0), vec3(0.3090169943749474241023, -0.9510565162951535721164, 1.0))*(1.0-abs(sin(time)));
}

float distanceHub(vec3 p){
	return pentagonToSphere(p)*0.3;
}

vec3 genNormal(vec3 p){
	float d = 0.001;
	return normalize(vec3(
		distanceHub(p + vec3(  d, 0.0, 0.0)) - distanceHub(p + vec3( -d, 0.0, 0.0)),
		distanceHub(p + vec3(0.0,   d, 0.0)) - distanceHub(p + vec3(0.0,  -d, 0.0)),
		distanceHub(p + vec3(0.0, 0.0,   d)) - distanceHub(p + vec3(0.0, 0.0,  -d))
	));
}

vec3 doColor(vec3 p){
	float e = 0.001;
	if (pentagonToSphere(p)<e){
		vec3 normal = genNormal(p);
		vec3 light  = normalize(vec3(1.0, 1.0, 1.0));
		float diff  = max(dot(normal, light), 0.1);
		return vec3(diff*abs(cos(time)*cos(time/2.0)), diff*abs(cos(time)*sin(time/2.0)), diff*abs(sin(time)));
	}
	return vec3(0.0);
}

void main(){
	vec2 p = (gl_FragCoord.xy * 2.0 - resolution) / min(resolution.x, resolution.y);
	vec3 cPos  = vec3(0.0, 0.0,  3.0);
	vec3 cDir  = vec3(0.0, 0.0, -1.0);
	vec3 cUp   = vec3(0.0, 1.0,  0.0);
	vec3 cSide = cross(cDir, cUp);
	float targetDepth = 1.0;

	vec3 ray = normalize(cSide * p.x + cUp * p.y + cDir * targetDepth);
	float dist = 0.0;
	float rLen = 0.0;
	vec3 rPos = cPos;

	for(int i = 0; i < 32; ++i){
		dist = distanceHub(rPos);
		rLen += dist;
		rPos = cPos + ray * rLen;
	}

	vec3 color = doColor(rPos);
	gl_FragColor = vec4(color, 1.0);
}

