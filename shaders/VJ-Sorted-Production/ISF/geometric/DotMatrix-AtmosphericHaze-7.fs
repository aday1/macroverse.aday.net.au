/*{
    "DESCRIPTION": "DotMatrix-AtmosphericHaze-7",
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
        "geometric"
    ]
}*/




#define time ((useFrameIndex ? (float(FRAMEINDEX) / fps) : TIME) * timeScale)
#define mouse vec2(mouseX, mouseY)
#define resolution RENDERSIZE
#ifdef GL_ES
precision mediump float;
#endif

float two_pi = 4.0 * asin(1.0);
const float proj_coeff = 0.2;
const float rot_coeff = 0.05;
const float move_coeff = 0.3;
const float fog_coeff = 1.8;
const float fog_exp = 0.1;

float chessboard(vec2 uv, vec2 nuv)
{
	bool pu = (mod(ceil(uv.x * nuv.x), 2.0) == 0.0);
	bool pv = (mod(ceil(uv.y * nuv.y), 2.0) == 0.0);
	
	return (pu && !pv) || (!pu && pv) ? 0.0 : 1.0;
}

vec4 texture(vec2 pos)
{
	float fval = chessboard(pos, vec2(12.0, 30.0));
	return vec4(fval, fval, fval, 1.0);
}

void main( void ) {

	vec2 mouse_pixel = mouse * resolution.xy;
	float res_min = min(resolution.x, resolution.y);
	vec2 away_from_center = (mouse_pixel - gl_FragCoord.xy) / res_min;
	
	float phi01 = mod(atan(away_from_center.y, away_from_center.x) / two_pi + rot_coeff * time, 1.0);
	float r01 = mod(0.1*proj_coeff / dot(away_from_center*away_from_center, away_from_center*away_from_center) + move_coeff * time, 1.0);
	
	vec2 pos = vec2(phi01, r01);
	gl_FragColor = fog_coeff * pow(length(away_from_center), fog_exp) * texture(pos * 2.0 - 3.0);
}
