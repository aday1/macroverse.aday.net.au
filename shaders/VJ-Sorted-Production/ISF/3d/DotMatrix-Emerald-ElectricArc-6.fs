/*{
    "DESCRIPTION": "DotMatrix-Emerald-ElectricArc-6",
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
        }
    ],
    "TAGS": [
        "space",
        "3d"
    ]
}*/




#define time ((useFrameIndex ? (float(FRAMEINDEX) / fps) : TIME))
#define mouse vec2(0.0)
#define resolution RENDERSIZE
#ifdef GL_ES
precision mediump float;
#endif

const float focal = 0.75;
const float fogCoeff = 5.0;
const float eps = 0.01;
const float inf = 999999.0;
const vec3 spherePos = vec3(1.0, 0.5, 4.5);
const float sphereR = 1.7;
const float reflSphere = .4;

const vec3 planeNormal = vec3(0.0, 1.0, 0.0);
const float planeD = 1.5;
const vec3 planeTangent = vec3(0.0, 0.0, 1.0);

const vec3 lightPos = vec3(-1.0, 1.6, 2.0);
const float lightStrength = 15.5;

float marchRay(vec3 v, vec3 v0);
vec3 colorPlane(vec3 p);
vec3 colorAt(vec3 p);

float planeAmbOccl(vec3 p)
{
	float q;

	return q;
}

float getShadow(vec3 p)
{
	vec3 v = p - lightPos;
	float t = marchRay(normalize(v), lightPos);
	return float(t >= length(v)-eps*10.);
}

float getLightness(vec3 p, vec3 n)
{
	float relaxed = exp(-length(p-lightPos)/lightStrength);
	return relaxed*max(dot(normalize(p-lightPos), -n), 0.0)*getShadow(p)*0.95+0.05;
}

//color of surface's point
vec3 colorSphere(vec3 coord)
{
	vec3 n = normalize(coord-spherePos);
	vec3 s = coord+n*1.1*eps;
	float t = marchRay(n, s);
	
	vec3 color = vec3(1.0, 0., 0.)*getLightness(coord, n);
	return color;
}

vec3 colorPlane(vec3 p)
{
	vec3 color = vec3(0.0,
		          mod(p.x+time, 1.6) < 0.15,
		          mod(p.z, 2.3) < 0.15);
	color = vec3(0.,
		     (1.-color[1])*color[2]+color[1]*(1.-color[2]),
		     color[1]*color[2]+(1.-color[1])*(1.-color[2])
		     );
	return color*getLightness(p, normalize(planeNormal));
}

//distance from point to sphere centered at (0,0,0)
float distToSphere(vec3 p, vec3 pos, float r)
{
	return length(p-pos) - r;
}

//distance from point to endless plane 
float distToPlane(vec3 p, vec3 n, float d)
{
	return dot(p, n)	 + d;
}

//distance from point to the world's objects
float distToWorld(vec3 p)
{
	float s1 = distToSphere(p, spherePos, sphereR);
	float p1 = distToPlane(p, planeNormal, planeD);
	
	return min(inf, min(p1, s1));
}

float marchRay(vec3 v, vec3 v0)
{
	float t = 0.0;
	
	float dist = inf;
	for(int i = 0; i < 64; ++i)
	{
		if(dist < eps) break;
		dist = distToWorld(t*v+v0);
		t += dist;
	}
	
	return t;
}

//color at space point
vec3 colorAt(vec3 p)
{
	float ds = distToSphere(p, spherePos, sphereR);
	float dp = distToPlane(p, planeNormal, planeD);
	
	if( ds < eps)
	{
		return colorSphere(p);
	}
	if( dp < eps)
	{
		return colorPlane(p);	   
	}
	return vec3(0.);
}

void main( void ) {
	
	vec2 position = ( gl_FragCoord.xy / resolution.xy )*2.0 - 1.0;
	position = vec2(position.x*resolution.x/resolution.y, position.y);
	
	vec3 ray = normalize(vec3(position, focal));
	
	float t = marchRay(ray, vec3(0., 0., 0.));
	float dist = t; //length(t*ray);
	
	vec3 color = colorAt(t*ray);
	
	float a = 1.0; //exp(-max((dist/fogCoeff), 0.));
	vec3 fogged = color*a + vec3(0.05, 0.05, 0.05)*(1.0-a);

	gl_FragColor = vec4(fogged, 1.0 );
}
