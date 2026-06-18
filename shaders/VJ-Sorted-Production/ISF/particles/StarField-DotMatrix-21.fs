/*{
    "DESCRIPTION": "StarField-DotMatrix-21",
    "CREDIT": "GLSL Sandbox / various",
    "ISFVSN": "2.0",
    "CATEGORIES": [
        "particles"
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
        "particles"
    ]
}*/




#define time ((useFrameIndex ? (float(FRAMEINDEX) / fps) : TIME) * timeScale)
#define mouse vec2(mouseX, mouseY)
#define resolution RENDERSIZE
#ifdef GL_ES
precision mediump float;
#endif

float fade(float value, float start, float end)
{
    //return (clamp(value,start,end)-start)/(end-start);
	return (value-start)/(end-start);
}

float rand(vec2 co){
    return fract(sin(dot(co.xy ,vec2(12.9898,78.233))) * 43758.5453);
}

vec3 texture(vec2 uv) {
	return vec3(rand(floor(uv*10.)/10.),rand(floor(uv*10.)/12.),rand(floor(uv*10.)/14.));
}

void main( void ) {
	#define PI 3.1415926535897932384
	vec2 uv = ( gl_FragCoord.xy / resolution.xy );
	vec2 road_uv = vec2(fade(uv.x,uv.y,0.5)*fade(1.-uv.x,uv.y,0.5),(uv.y*1.2)+(time*3.));
	float darken = pow(fade(uv.y,0.,0.5),6.);
	vec3 outputs = texture(road_uv)-darken;
	gl_FragColor = vec4(outputs, 1.0 );

}
