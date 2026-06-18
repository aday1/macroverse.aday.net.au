/*{
    "DESCRIPTION": "DotMatrix-14",
    "CREDIT": "GLSL Sandbox / various",
    "ISFVSN": "2.0",
    "CATEGORIES": [
        "geometric"
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
        "geometric"
    ]
}*/




#define time ((useFrameIndex ? (float(FRAMEINDEX) / fps) : TIME) * speed)
#define mouse vec2(0.0)
#define resolution RENDERSIZE

#ifdef GL_ES
precision mediump float;
#endif
 
#extension GL_OES_standard_derivatives : enable
 
//based on a gif I saw on imgur. 
//looked easy to duplicate. It was. 
//https://i.imgur.com/zk2rtku.gif
 
//fix redefinition error

float size = 20.0;
float speed= 0.75;
 
float randomize(vec2 coords){
	//http://byteblacksmith.com/improvements-to-the-canonical-one-liner-glsl-rand-for-opengl-es-2-0/
	highp float a = 12.9898;
    	highp float b = 78.233;
    	highp float c = 43758.5453;
    	highp float dt= dot(coords.xy ,vec2(a,b));
    	highp float sn= mod(dt,3.14);
    	return fract(sin(sn) * c);
}
 
vec3 getColor(vec2 coords){
	coords.x = coords.x-mod(coords.x, size);
	coords.y = coords.y-mod(coords.y, size);
	
	float r = randomize(coords.xy);
	float g = randomize(coords.xy * 20.0);
	float b = randomize(coords.xy * 37.0);
	return vec3(r,g,b);
}
 
float triangleWave(float x){
	x = mod(x,2.0);
	if (x > 1.0) x = -x+2.0;
	return x;
}
 
bool inSize(vec2 coords){
	vec2 box = coords.xy-mod(coords.xy, size);
	vec2 center = box+(size/2.0);
	float _size = (triangleWave((time * speed)+(randomize(box)*2.0)))*(size);
	return (distance(coords, center) < _size);
}
 
void main( void ) {
	size = mouse.x*50.;
	vec3 color = vec3(0.0);
	if (inSize(gl_FragCoord.xy)) color = getColor(gl_FragCoord.xy);
	gl_FragColor = vec4( color, 1.0 );
 
}

