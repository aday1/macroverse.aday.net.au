/*{
    "DESCRIPTION": "ColoroxBoxes-XY",
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

//based on a gif I saw on imgur. 
//looked easy to duplicate. It was. 
//https://i.imgur.com/zk2rtku.gif

// mouse.x
// mouse.y
// inputColour.x
// inputColour.y
// inputColour.z
// inputColour.w

float size = 30.0;
float speed= .75;

float random(vec2 co){
    return fract(sin(dot(co.xy ,vec2(12.9898,78.233))) * 43758.5453);
}
vec3 random_color(vec2 coords){
	float a = floor(random(coords.xy*6.896)*7.);
	//(2^3)-1
	//           { return vec3(0.,0.,0.); } //BLACK
	if (a == 0.) { return vec3(1.,0.,0.); } //RED
	if (a == 1.) { return vec3(0.,1.,0.); } //GREEN
	if (a == 2.) { return vec3(1.,1.,0.); } //YELLOW
	if (a == 3.) { return vec3(0.,0.,1.); } //BLUE
	if (a == 4.) { return vec3(1.,0.,1.); } //MAGENTA
	if (a == 5.) { return vec3(0.,1.,1.); } //CYAN
	else         { return vec3(1.,1.,1.); } //WHITE
}
float tri(float x){
	x = mod(x,mouse.x);
	if (x > mouse.y) x = -x+2.0;
	return x;
}
float chess_dist(vec2 uv) {
    return max(abs(uv.x),abs(uv.y));
}
void main( void ) {
	vec2 uv = -1.0 + 2.0 * gl_FragCoord.xy / resolution.xy;
	uv.y *= resolution.y/resolution.x;
	vec3 colors = random_color(floor(uv*size))*step(chess_dist((fract(uv*size)-.5)*2.),tri((((time*speed)+((random(floor(uv*size)))*2.)))));
	gl_FragColor = vec4(colors, 1.0 );

}
