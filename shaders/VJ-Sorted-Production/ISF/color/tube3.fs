/*{
    "DESCRIPTION": "tube3",
    "CREDIT": "GLSL Sandbox / various",
    "ISFVSN": "2.0",
    "CATEGORIES": [
        "color"
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
        "color"
    ]
}*/



#define time ((useFrameIndex ? (float(FRAMEINDEX) / fps) : TIME) * timeScale)
#define mouse vec2(mouseX, mouseY)
#define resolution RENDERSIZE
#ifdef GL_ES
precision mediump float;
#endif

// posted by Trisomie21

const float pi_2_f = 3.1416*2.0;
float lineTickness = mouse.y;
const float lumaCount = 50.0;
const float hueCount = 50.0;

void main( void ) {

	vec2 position =  gl_FragCoord.xy - (resolution.xy/2.0);
	
	float d = length(position);
	
	float v = mod(d + time/20.0*resolution.x + resolution.x/(lumaCount*2.0), resolution.x/lumaCount);
	v = abs(v - resolution.x/(lumaCount*2.0)) - lineTickness;
	
	float angle = acos(position.y/d) / pi_2_f;
	if(position.x>0.0) angle += (0.5-angle)*2.0;
	angle += mouse.x*10.*time/60.0;
	
	float unit = d * pi_2_f;
	float a = mod(angle*unit + unit/(hueCount*2.0) , unit/hueCount);
	a = abs(a - unit/(hueCount*2.0)) - lineTickness;
	a = clamp(a, 0.0, 1.0);

	angle += .05;
	float u = 100.0;
	float r = mod(angle*u + u/(5.0*2.0) , u/5.0);
	r = abs(r - u/(5.0*2.0)) - 1.0;
	r = clamp(r, 0.0, 1.0);

	d /= length(resolution.xy);
	v = min(v, a)*d;

	float dr = .2+(sin(time*1.1)+1.0)/8.0;
	float dg = .2+(sin(time*1.2)+1.0)/8.0;
	float db = .2+(sin(time*1.4)+1.0)/8.0;
	vec3 fCol = vec3(v/(r+dr),v/(r+dg),v/(r+db));
	gl_FragColor = vec4(fCol,max(fCol.r, max(fCol.g, fCol.b)));
}
