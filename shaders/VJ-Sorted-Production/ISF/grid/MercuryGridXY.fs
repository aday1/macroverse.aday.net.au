/*{
    "DESCRIPTION": "MercuryGridXY",
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
        "grid"
    ]
}*/



#define time ((useFrameIndex ? (float(FRAMEINDEX) / fps) : TIME) * timeScale)
#define mouse vec2(mouseX, mouseY)
#define resolution RENDERSIZE
#ifdef GL_ES
precision mediump float;
#endif

#extension GL_OES_standard_derivatives : enable

float rand(float n){return fract(sin(n) * 43758.5453123);}

float rand(vec2 n) { 
	return fract(sin(dot(n, vec2(12.9898, 4.1414))) * 43758.5453);
}

float noise(float p){
	float fl = floor(p);
  float fc = fract(p);
	return mix(rand(fl), rand(fl + 1.0), fc);
}
	
float noise(vec2 n) {
	vec2 d = vec2(0.0, 1.0);
  vec2 b = floor(n);
	vec2 f = smoothstep(vec2(0.0), vec2(1.0), fract(n));
	float h = mix(
		mix(rand(b), rand(b + d.yx), f.x)
		, mix(rand(b + d.xy), rand(b + d.yy), f.x)
		, f.y);
	return h;
}

void main( void ) {

	vec2 p= ( gl_FragCoord.xy / resolution.xy ) + mouse / 4.0;
	vec2 uv = p*10.;
	uv.x += noise(uv*3.+6.)*clamp(sin(time*0.5-uv.x),0.,1.);
	uv.y += noise(uv*3.-5.)*clamp(sin(time*0.5-uv.x),0.,1.);
	float color = 0.0;
	color += .05/(uv.x-floor(uv.x))+.05/(uv.y-floor(uv.y));
	gl_FragColor = vec4( vec3(1.)*color, 1.0 );

}
