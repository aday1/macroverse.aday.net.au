uniform float val_n0_161; // @expose -0.839 1.161
uniform float val_n0_467; // @expose -0.5329999999999999 1.467
/*{
    "DESCRIPTION": "InkWash-TextGlyph",
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
        "misc"
    ]
}*/




#define time ((useFrameIndex ? (float(FRAMEINDEX) / fps) : TIME) * timeScale)
#define mouse vec2(mouseX, mouseY)
#define resolution RENDERSIZE
#ifdef GL_ES
precision mediump float;
#endif

vec3 squereTexture(vec2 uv, vec3 firstColor, vec3 secondColor) { // Flag texture
	if(bool(mod(floor(20.0 * uv.x), 2.0)) ^^ bool(mod(floor(20.0 * uv.y), 2.0))) // I made a true table and this is the result
		return secondColor;
	else
		return firstColor;
}

void main(){
	vec2 uv= gl_FragCoord.xy / resolution.xy; // Calculates UV screen coordinates
	vec2 mousePos = mouse;
	vec3 colorA = vec3(val_n0_467, val_n0_161, 0.325);
	vec3 colorB = vec3(0.867, 0.282, 0.078);
	float radio = 0.2;
	
	uv.y *= resolution.y / resolution.x;
	mousePos.y *= resolution.y / resolution.x;
	
	//if(length(uv - mousePos) <= radio) { // Fish eye effect!!
		float newLength;
		
		uv -= mousePos;
		newLength = pow(length(uv) / radio, 0.5) * length(uv) * (1. + .05 * sin(24.*length(uv) - mod(time, 1.)*2.*3.1415));
		uv /= length(uv);
		uv *= newLength;
		uv += mousePos;
	//}
	
	gl_FragColor = vec4(squereTexture(uv, colorA, colorB), 1.0); // Painting flag!
}
