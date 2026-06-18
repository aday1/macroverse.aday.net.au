/*{
    "DESCRIPTION": "DotMatrix-Emerald-ElectricArc-3",
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
            "NAME": "speed",
            "TYPE": "float",
            "DEFAULT": 1.0,
            "MIN": 0.0,
            "MAX": 5.0,
            "LABEL": "Speed"
        },
        {
            "NAME": "mouseX",
            "TYPE": "float",
            "DEFAULT": 0.0,
            "MIN": -1.0,
            "MAX": 1.0,
            "LABEL": "Mouse X"
        },
        {
            "NAME": "mouseY",
            "TYPE": "float",
            "DEFAULT": 0.0,
            "MIN": -1.0,
            "MAX": 1.0,
            "LABEL": "Mouse Y"
        },
        {
            "NAME": "zoom",
            "TYPE": "float",
            "DEFAULT": 1.0,
            "MIN": 0.1,
            "MAX": 4.0,
            "LABEL": "Zoom"
        },
        {
            "NAME": "colorR",
            "TYPE": "float",
            "DEFAULT": 1.0,
            "MIN": 0.0,
            "MAX": 2.0,
            "LABEL": "Color Red"
        },
        {
            "NAME": "colorG",
            "TYPE": "float",
            "DEFAULT": 1.0,
            "MIN": 0.0,
            "MAX": 2.0,
            "LABEL": "Color Green"
        },
        {
            "NAME": "colorB",
            "TYPE": "float",
            "DEFAULT": 1.0,
            "MIN": 0.0,
            "MAX": 2.0,
            "LABEL": "Color Blue"
        },
        {
            "NAME": "brightness",
            "TYPE": "float",
            "DEFAULT": 0.0,
            "MIN": -1.0,
            "MAX": 1.0,
            "LABEL": "Brightness"
        },
        {
            "NAME": "saturation",
            "TYPE": "float",
            "DEFAULT": 1.0,
            "MIN": 0.0,
            "MAX": 3.0,
            "LABEL": "Saturation"
        },
        {
            "NAME": "contrast",
            "TYPE": "float",
            "DEFAULT": 1.0,
            "MIN": 0.0,
            "MAX": 3.0,
            "LABEL": "Contrast"
        },
        {
            "NAME": "hueShift",
            "TYPE": "float",
            "DEFAULT": 0.0,
            "MIN": 0.0,
            "MAX": 1.0,
            "LABEL": "Hue Shift"
        },
        {
            "NAME": "invert",
            "TYPE": "bool",
            "DEFAULT": 0,
            "LABEL": "Invert Colors"
        }
    ],
    "TAGS": [
        "space",
        "3d"
    ]
}*/




#define time ((useFrameIndex ? (float(FRAMEINDEX) / fps) : TIME) * speed)
#define mouse vec2(mouseX, mouseY)
#define resolution (RENDERSIZE / zoom)
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

void _userMain( void ) {
	
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

void main() {
    _userMain();
    vec3 c = gl_FragColor.rgb;
    float a = gl_FragColor.a;
    float luma = dot(c, vec3(0.299, 0.587, 0.114));
    c = mix(vec3(luma), c, saturation);
    c = (c - 0.5) * contrast + 0.5;
    c *= vec3(colorR, colorG, colorB);
    c += brightness;
    if (hueShift > 0.001) {
        float cosH = cos(hueShift * 6.28318);
        float sinH = sin(hueShift * 6.28318);
        c = vec3(
            c.r * (0.299 + 0.701*cosH + 0.168*sinH) + c.g * (0.587 - 0.587*cosH + 0.330*sinH) + c.b * (0.114 - 0.114*cosH - 0.497*sinH),
            c.r * (0.299 - 0.299*cosH - 0.328*sinH) + c.g * (0.587 + 0.413*cosH + 0.035*sinH) + c.b * (0.114 - 0.114*cosH + 0.292*sinH),
            c.r * (0.299 - 0.300*cosH + 1.250*sinH) + c.g * (0.587 - 0.588*cosH - 1.050*sinH) + c.b * (0.114 + 0.886*cosH - 0.203*sinH)
        );
    }
    if (invert) c = 1.0 - c;
    gl_FragColor = vec4(clamp(c, 0.0, 1.0), a);
}