/*{
    "DESCRIPTION": "DotMatrix-44",
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

#extension GL_OES_standard_derivatives : enable

//based on a gif I saw on imgur. 
//looked easy to duplicate. It was. 
//https://i.imgur.com/zk2rtku.gif

//fix redefinition error

float size = 32.0;
float speed= 1.0;

float randomize(vec2 coords){
	//http://byteblacksmith.com/improvements-to-the-canonical-one-liner-glsl-rand-for-opengl-es-2-0/
	float a = 1.282427;
    	float b = 41.49865;
    	float c = 57721.56649;
    	float dt= dot(coords.xy ,vec2(a,b));
    	float sn= mod(dt,2.685452);
    	return fract(sin(sn) * c);
}

vec3 getColor(vec2 coords){
	coords.x = coords.x-mod(coords.x, size);
	coords.y = coords.y-mod(coords.y, size);
	
	float r = randomize(coords.xx);
	float g = randomize(coords.yy );
	float b = randomize(vec2(r,g));
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
	float _size = (triangleWave((time * speed)+(randomize(box*box)*2.0))/2.0)*(size);
	return (abs(coords.x-center.x) < _size && abs(coords.y-center.y) < _size);
}

void main( void ) {
	vec3 color = vec3(0.0);
	if (inSize(gl_FragCoord.xy)) color += getColor(gl_FragCoord.xy);
	gl_FragColor = vec4( color, 1.0 );

}
