/*{
    "DESCRIPTION": "PullingShapes1",
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
        "3d"
    ]
}*/



#define time ((useFrameIndex ? (float(FRAMEINDEX) / fps) : TIME) * timeScale)
#define mouse vec2(mouseX, mouseY)
#define resolution RENDERSIZE
#ifdef GL_ES
precision mediump float;
#endif

#extension GL_OES_standard_derivatives : enable

#define Thickness 0.005
#define PI 3.14159265359

vec3 color;
vec2 uv;
float res = 64.0;
float theta = PI * 2.0 / res;
float aspect = resolution.x / resolution.y;
vec2 pos = vec2(0.5 * aspect, 0.5);
float rad = 0.4;

float drawLine(vec2 p1, vec2 p2) {
  vec2 uv = gl_FragCoord.xy / resolution.xy;
  uv.x *= aspect;

  float a = abs(distance(p1, uv));
  float b = abs(distance(p2, uv));
  float c = abs(distance(p1, p2));

  if ( a >= c || b >=  c ) return 0.0;

  float p = (a + b + c) * 0.5;

  // median to (p1, p2) vector
  float h = 2.0 / c * sqrt( p * ( p - a) * ( p - b) * ( p - c));

  return mix(1.0, 0.0, smoothstep(0.5 * Thickness, 1.5 * Thickness, h));
}
	
void main( void ) {
 	
	uv = gl_FragCoord.xy / resolution;
	color = vec3(0.0);
	
	for(float i = 0.0; i < 64.0; i+=1.0){
		float hit = drawLine(vec2(cos(theta * i + time), sin(theta * i)) * rad + pos, 
				     vec2(cos(theta * (i+1.0)), sin(theta * (i+1.0) + time)) * rad + pos);
		if(hit > 0.0)
			color = vec3(hit);
	}
	
	gl_FragColor = vec4(color,1.0);

}
