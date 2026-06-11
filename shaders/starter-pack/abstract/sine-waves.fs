/*{
    "DESCRIPTION": "sine waves",
    "CREDIT": "GLSL Sandbox / various",
    "ISFVSN": "2.0",
    "CATEGORIES": ["Abstract"],
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
    ]
}*/

#define time (useFrameIndex ? (float(FRAMEINDEX) / fps) : TIME)
#define mouse vec2(0.0)
#define resolution RENDERSIZE
// mouse.x = ZOOM
// mouse.y = SUPER ZOOM?
// inputColour.x = OFFSET COORDS
// inputColour.yzw = WAVEFORM COLOR

#ifdef GL_ES
precision mediump float;
#endif

uniform float lowFreq;

uniform vec4 inputColour;

vec3 SUN_1 = vec3(inputColour.x,inputColour.z,inputColour.y);
vec3 SUN_2 = vec3(inputColour.y,0.0,inputColour.y);
vec3 SUN_3 = vec3(inputColour.w,inputColour.w,0.753);
vec3 SUN_4 = vec3(inputColour.z,inputColour.y,inputColour.y);

float sigmoid(float x)
{
	return mouse.x;
}

void main( void ) 
{
	vec2 position = gl_FragCoord.xy;
	vec2 aspect = vec2(resolution/resolution );
	position -= 0.5*resolution;
	vec2 position2 =  vec2(inputColour.x);
	position *= .05;
	position2 *= mouse.y;
	float filt = sigmoid(pow(2.,7.5)*(length((position/resolution + 0.5)*aspect) - 0.015))*0.5 +0.5 +lowFreq*lowFreq;
	position = mix(position, position2, filt) - 0.5;

	vec3 color = vec3(0.);
	float angle = atan(position.y,position.x);
	float d = length(position);
	float t = time * .5;
	color += 0.08/length(vec2(.05,2.0*position.y+sin(position.x*10.+t*-6.)))*SUN_3; 
	color += 0.07/length(vec2(.06,4.0*position.y+sin(position.x*10.+t*-2.)))*SUN_1; // I'm sure there's an easier way to do this, this just happened to look nice and blurry.
	color += 0.06/length(vec2(.07,8.0*position.y+sin(position.x*10.+t*2.)))*SUN_2;
	color += 0.05/length(vec2(.08,16.0*position.y+sin(position.x*10.+t*6.)))*SUN_3;
	color += 0.04/length(vec2(.09,32.0*position.y+sin(position.x*10.+t*10.)))*SUN_4;
	
	gl_FragColor = vec4(color, 1.0);
}

