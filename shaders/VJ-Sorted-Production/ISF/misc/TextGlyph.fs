/*{
    "DESCRIPTION": "TextGlyph",
    "CREDIT": "GLSL Sandbox / various",
    "ISFVSN": "2.0",
    "CATEGORIES": [
        "misc"
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
        "misc",
        "texture-input"
    ]
}*/




#define time ((useFrameIndex ? (float(FRAMEINDEX) / fps) : TIME) * timeScale)
#define mouse vec2(mouseX, mouseY)
#define resolution RENDERSIZE
#ifdef GL_ES
precision mediump float;
#endif

uniform sampler2D backbuffer;

const int num_x = 5;
const int num_y = 5;
float w = resolution.x;
float h = resolution.y;
 
vec4 draw_ball(int i, int j) {
	float t = time;
	float x = w/2.0 * (1.0 + cos(1.5 * t + float(3*i+4*j)));
	float y = h/2.0 * (1.0 + sin(2.3 * t + float(3*i+4*j)));
	float size = 3.0 - 2.0 * sin(t);
	vec2 pos = vec2(x, y);
	float dist = length(gl_FragCoord.xy - pos);
	float intensity = pow(size/dist, 2.0);
	vec4 color = vec4(0.0);
	color.r = 0.5 + cos(t*float(i));
	color.g = 0.5 + sin(t*float(j));
	color.b = 0.5 + sin(float(j));
	return color*intensity;
}

void main() {
	float t = time;
	float x = gl_FragCoord.x/w;
	float y = gl_FragCoord.y/h;
	float v = sin(y*3.141592*10.0*t/2.0+t);
	v = sin(x*3.141592*10.0+t+v);
	vec4 color = vec4(v,1.0-v*v,v,1.0);
	vec2 texPos = vec2(gl_FragCoord.xy/resolution);
	vec4 shadow = texture2D(backbuffer, texPos)*0.7;
	gl_FragColor = color;
}

