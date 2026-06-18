/*{
    "DESCRIPTION": "DriftLens05",
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

vec3 color;

float loopto(float a, float top) {
	a = mod(a, top*2.0);
	if(a < top)
		return a;
	else
		return top*2.0-a;
}

void main( void ) {
	float a = time, cosa = cos(a), sina = sin(a);
	vec2 position = gl_FragCoord.xy / resolution.xy;
	position = vec2(position.x * cosa - position.y * sina, position.y*cosa + position.x * sina); 
	vec3 color = vec3(1, abs(sin(position.x*loopto(time, 2.3) / 0.04)), sin(position.x*position.y*loopto(time, 2.3) / 0.04) ); 
	//float dist = mod(distance(position, mouse), 10.0);
	vec3 color2 = vec3(abs(cos(position.y*loopto(time, 2.3) / 0.04)), 0.5, abs(cos(position.y*loopto(time, 2.3) / 0.04))); 
	vec3 color3 = vec3(abs(cos(position.x*loopto(time, 2.3) / 0.04)), cos(position.x*loopto(time, 2.3) / 0.04), 0.1); 
	vec3 color4 = vec3(0.3, abs(sin(position.y*loopto(time, 2.3) / 0.04)), 0.4); 
	color = cross(color, color2) * 1.5;
	color = cross(color, color3) * 5.0;
	color = cross(color, color4) * 3.0;
	gl_FragColor = vec4(color.r, color.g, color.b, 1.0);
}
