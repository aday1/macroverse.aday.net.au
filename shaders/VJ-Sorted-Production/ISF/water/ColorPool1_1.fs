/*{
    "DESCRIPTION": "ColorPool1",
    "CREDIT": "GLSL Sandbox / various",
    "ISFVSN": "2.0",
    "CATEGORIES": [
        "water"
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
        "water"
    ]
}*/



#define time ((useFrameIndex ? (float(FRAMEINDEX) / fps) : TIME))
#define mouse vec2(0.0)
#define resolution RENDERSIZE
#ifdef GL_ES
precision mediump float;
#endif

#define Epsilon 0.1

#define float2 vec2
#define float3 vec3
#define float4 vec4
#define frac fract

float tri( float x )
{
  return abs( frac(x) - .5 );
}

vec3 tri3( vec3 p )
{
  return vec3( 
      tri( p.z + tri( p.y * 1. ) ), 
      tri( p.z + tri( p.x * 1. ) ), 
      tri( p.y + tri( p.x * 1. ) )
  );
}

float trinoise(vec3 p, float spd, float _time)
{
  float z  = 1.4;
  float rz =  0.;
  vec3  bp =   p;
  for(float i = 0.; i <= 3.; i++) {
    vec3 dg = tri3( bp * 2. );
    p += ( dg + _time * .1 * spd );
    bp *= 1.8;
    z  *= 1.5;
    p  *= 1.2;
    float t = tri(p.z + tri(p.x + tri(p.y)));
    rz += t / z;
    bp += 0.14;
  }
  return rz;
}

float trinoise(vec3 p)
{
	return trinoise(p, 0.0, 0.0);
}

vec3 curl_trinoise(vec3 p, float epsilon)
{
    float ie = 1.0 / (2.0 * epsilon);
    float nx1 = trinoise(vec3(p.x + epsilon, p.y, p.z));
    float nx2 = trinoise(vec3(p.x - epsilon, p.y, p.z));
    float ny1 = trinoise(vec3(p.x, p.y + epsilon, p.z));
    float ny2 = trinoise(vec3(p.x, p.y - epsilon, p.z));
    float nz1 = trinoise(vec3(p.x, p.y, p.z + epsilon));
    float nz2 = trinoise(vec3(p.x, p.y, p.z - epsilon));
    return vec3(
        ((ny1 - ny2) * ie) - ((nz1 - nz2) * ie),
        ((nz1 - nz2) * ie) - ((nx1 - nx2) * ie),
        ((nx1 - nx2) * ie) - ((ny1 - ny2) * ie));
}

void main( void )
{
	vec2 position = ( gl_FragCoord.xy / resolution.xy ) + mouse / 4.0;
	vec3 coord = vec3(position.xy, time*0.1);
	vec3 color = curl_trinoise(coord, Epsilon);
	gl_FragColor = vec4(color, 1.0 );
}
