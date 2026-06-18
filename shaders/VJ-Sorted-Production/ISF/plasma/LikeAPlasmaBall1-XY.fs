/*{
    "DESCRIPTION": "LikeAPlasmaBall1-XY",
    "CREDIT": "GLSL Sandbox / various",
    "ISFVSN": "2.0",
    "CATEGORIES": [
        "plasma"
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
        "plasma"
    ]
}*/



#define time ((useFrameIndex ? (float(FRAMEINDEX) / fps) : TIME) * timeScale)
#define mouse vec2(mouseX, mouseY)
#define resolution RENDERSIZE
#ifdef GL_ES
precision highp float;
#endif

//Rolling shutter effect on a propeller
//Move the mouse left/right to increase/decrease the shutter time

#define NUM_BLADES 3.0
#define BLADE_WIDTH 0.02
#define BLADE_LENGTH 0.40
#define HUB_SIZE 0.08

#define PROP_SPEED 0.2
#define MAX_SHUTTER_TIME 20.0

float pi = atan(1.0)*8.0;

mat2 Rotate2D(float angle)
{
	return mat2(cos(angle),sin(angle),-sin(angle),cos(angle));	
}

void main( void ) 
{
	vec2 res = resolution / resolution.y;
	vec2 uv = gl_FragCoord.xy / resolution.y - res / 2.0;
	
	float shutterTime = uv.y * mouse.x*MAX_SHUTTER_TIME;
	float angle = 2.0 * pi * (time - shutterTime) * PROP_SPEED;
	
	uv *= Rotate2D(angle);
	
	float d = 0.0;	
	
	float bladeAng = (pi/NUM_BLADES);
	
	//Blades
	d = abs(mod(atan(uv.y, uv.x) + bladeAng/2.0, bladeAng) - bladeAng/2.0) * length(uv);
	d -= BLADE_WIDTH;
	
	//Hub / Blade length
	d = min(d, length(uv) - HUB_SIZE);
	d = max(d, length(uv) - BLADE_LENGTH);
	
	float c = smoothstep(1.0/resolution.y, 0.000, d);
	
	gl_FragColor = vec4(vec3(c), 1.0);

}
