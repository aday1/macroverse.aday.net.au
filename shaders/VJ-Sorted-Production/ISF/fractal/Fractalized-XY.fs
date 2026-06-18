/*{
    "DESCRIPTION": "Fractalized-XY",
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
        },
        {
            "NAME": "inputColour",
            "TYPE": "vec4",
            "LABEL": "Input Colour"
        }
    ],
    "TAGS": [
        "fractal"
    ]
}*/
uniform float scale;
#define E 2.71828182846

varying vec2 position;
uniform float timeScale;




#define time ((useFrameIndex ? (float(FRAMEINDEX) / fps) : TIME) * timeScale)
#define mouse vec2(mouseX, mouseY)
#define resolution RENDERSIZE
#ifdef GL_ES
precision mediump float;
#endif

uniform vec4 inputColour;

// mouse.x
// mouse.y
// inputColour.x
// inputColour.y
// inputColour.z
// inputColour.w

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

void main( void ) {

	vec2 uv = gl_FragCoord.xy/resolution.xy;
	vec2 p	= uv - mouse.x;
	p 	*= ASPECT;
	//p 	= normalize(vec3(p, 1.-length(p))).xy;
	
	vec2 m	= mouse - mouse.y;
	m 	*= ASPECT;
	m	*= 2.;
	float c 	= 0.;
	float b 	= 0.;
	float t	= 0.;
	
	vec2 d = normalize(m-p);

	mat2 rm = rmat(m.x*(inputColour.x*atan(1.)));
	for(int i = 0; i < 48; i++)
	{
		p = abs(p)-inputColour.y;
		p *= rm;
	//	p = p/dot(p,p);
		c += circle(p, .25);
		b += box(p, vec2(inputColour.x));
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
	return 1.-clamp(inputColour.w * x * RESOLUTION_MIN, 0., 1.);
}
			       
vec2 project(vec2 position, vec2 a, vec2 b)
{
	vec2 q	 	= b - a;	
	float u 		= dot(position - a, q)/dot(q, q);
	u 		= clamp(u, inputColour.w, inputColour.z);
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
	vec2 edge 	= max(vertex, inputColour.z);
	float interior	= max(vertex.x, vertex.y);
	return min(interior, inputColour.w) + length(edge);
}

float simplex(vec2 position, float scale)
{		
	const float r3	= 1.73205080757;//sqrt(3.);
	
	position.y	/= r3; 
	
	vec3 edge	= vec3(inputColour.y);
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


