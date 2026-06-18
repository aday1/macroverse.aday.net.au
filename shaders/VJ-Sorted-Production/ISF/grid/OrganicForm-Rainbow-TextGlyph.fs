/*{
    "DESCRIPTION": "OrganicForm-Rainbow-TextGlyph",
    "CREDIT": "GLSL Sandbox / various",
    "ISFVSN": "2.0",
    "CATEGORIES": [
        "grid"
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
        "grid",
        "texture-input"
    ]
}*/




#define time ((useFrameIndex ? (float(FRAMEINDEX) / fps) : TIME) * timeScale)
#define mouse vec2(mouseX, mouseY)
#define resolution RENDERSIZE
// Recreation of http://i.imgur.com/sX9SHHm.gif

#ifdef GL_ES
precision mediump float;
#endif

uniform sampler2D backbuffer;

vec3 hsv(in float h, in float s, in float v)
{
	return mix(vec3(1.0), clamp((abs(fract(h + vec3(3, 2, 1) / 3.0) * 6.0 - 3.0) - 1.0), 0.0 , 1.0), s) * v;
}

float x(float t) { // From http://mathforum.org/kb/message.jspa?messageID=407257
	t = mod(t, 4.0);
	return abs(t) - abs(t-1.0) - abs(t-2.0) + abs(t-3.0) - 1.0;	
}

float map(float t, vec2 p) {
	//return t*2.0+p.x/30.0*p.y;
	//return t*2.0+length(p)/3.0;
	return t*2.0+atan(p.y+0.01,p.x)/1.57*4.0+length(p)/3.0;
}

void main(void)
{
	vec2 uv = -0.5 + gl_FragCoord.xy / resolution.xy;
	uv.x *= resolution.x / resolution.y;
	vec2 p = abs(mod(uv*30.0, 1.0));
	vec2 cell = floor(uv*30.0);
	float t = map(time, cell);
	vec2 s = vec2(x(t), x(t-1.0))*0.35+0.5; 
	float d = max(abs(p.x-s.x), abs(p.y-s.y));
	//float d = length(p-s);
	float c = step(d, 0.15);
	float l = texture2D(backbuffer, gl_FragCoord.xy / resolution.xy).a-0.01;
	gl_FragColor = vec4(hsv(t*0.25, 1.0, c)+(1.0-c)*hsv(t*0.25, 1.0, l), c+l);
}
