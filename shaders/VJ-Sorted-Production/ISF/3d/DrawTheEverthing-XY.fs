/*{
    "DESCRIPTION": "DrawTheEverthing-XY",
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
        "geometric",
        "noise",
        "3d"
    ]
}*/



#define time ((useFrameIndex ? (float(FRAMEINDEX) / fps) : TIME) * speed)
#define mouse vec2(mouseX, mouseY)
#define resolution (RENDERSIZE / zoom)
#ifdef GL_ES
precision mediump float;
#endif

#define RESOLUTION_MIN	min(resolution.x, resolution.y)
#define ASPECT		(resolution.xy/RESOLUTION_MIN)

vec2  project(vec2 position, vec2 a, vec2 b);
float bound(vec2 position, vec2 normal, float translation);
float sphere(vec2 position, float radius);
float torus(vec2 position, vec2 radius);
float cube(vec2 position, vec2 scale);
float simplex(vec2 position, float scale);
float segment(vec2 position, vec2 a, vec2 b);

float contour(float x);
float point(vec2 position, float radius);
float point(vec2 position);
float circle(vec2 position, float radius);
float line(vec2 p, vec2 a, vec2 b);
float box(vec2 position, vec2 scale);
float triangle(vec2 position, vec2 scale);
mat2  rmat(float t);

void _userMain( void ) {

	vec2 uv = gl_FragCoord.xy/resolution.xy;
	vec2 p	= uv - .5;
	p 	*= ASPECT;
	//p 	= normalize(vec3(p, 1.-length(p))).xy;
	
	vec2 m	= mouse - .5;
	m 	*= ASPECT;
	m	*= 2.;
	float c 	= 0.;
	float b 	= 0.;
	float t	= 0.;
	
	vec2 d = normalize(m-p);

	mat2 rm = rmat(m.x*(8.*atan(1.)));
	for(int i = 0; i < 48; i++)
	{
		p = abs(p)-.5;
		p *= rm;
	//	p = p/dot(p,p);
		c += circle(p, .25);
		b += box(p, vec2(.5));
		t += triangle(p, vec2(.5));
		p *= 1. + 32.*fract(.001);

	}
	
	vec4 result = vec4(0.);

	result.x		+= c;
	result.z		+= t;
	result.y		+= b;	
	
	result.w 	= 1.;
	
	gl_FragColor = result;

}

float contour(float x)
{
	return 1.-clamp(.6 * x * RESOLUTION_MIN, 0., 1.);
}
			       
vec2 project(vec2 position, vec2 a, vec2 b)
{
	vec2 q	 	= b - a;	
	float u 		= dot(position - a, q)/dot(q, q);
	u 		= clamp(u, 0., 1.);
	return mix(a, b, u);
}

float bound(vec2 position, vec2 normal, float translation)
{
  return dot(position, normal) + translation;
}

float sphere(vec2 position, float radius)
{
	return length(position)-radius;
}

float torus(vec2 position, vec2 radius)
{
	
	return abs(abs(length(position)-radius.x)-radius.y);
}

float cube(vec2 position, vec2 scale)
{
	vec2 vertex 	= abs(position) - scale;
	vec2 edge 	= max(vertex, 0.);
	float interior	= max(vertex.x, vertex.y);
	return min(interior, 0.) + length(edge);
}

float simplex(vec2 position, float scale)
{		
	const float r3	= 1.73205080757;//sqrt(3.);
	
	position.y	/= r3; 
	
	vec3 edge	= vec3(0.);
	edge.x		= position.y + position.x;
	edge.y		= position.x - position.y;
	edge.z		= position.y + position.y;
	edge		*= .86602540358; //cos(pi/6.);
	
	return max(edge.x, max(-edge.y, -edge.z))-scale/r3;
}

float segment(vec2 position, vec2 a, vec2 b)
{
	return distance(position, project(position, a, b));
}

float point(vec2 position, float radius)
{
	return contour(sphere(position*RESOLUTION_MIN, radius));	
}

float point(vec2 position)
{
	return point(position, 3.);	
}

float circle(vec2 position, float radius)
{
	return contour(torus(position, vec2(radius,0.)));
}

float line(vec2 p, vec2 a, vec2 b)
{
	return contour(segment(p, a, b));
}

float box(vec2 position, vec2 scale)
{
	return contour(abs(cube(position, scale)));	
}

float triangle(vec2 position, vec2 scale)
{
	return contour(abs(simplex(position, scale.x)));	
}
			       
mat2 rmat(float t)
{
	float c = cos(t);
	float s = sin(t);
	return mat2(c, s, -s, c);
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