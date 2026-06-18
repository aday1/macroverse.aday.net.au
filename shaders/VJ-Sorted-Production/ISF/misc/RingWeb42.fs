/*{
    "DESCRIPTION": "RingWeb42",
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

#define PI 3.14159265358979323
float shrp = 500.;
void main( void ) {

	vec2 uv = ( gl_FragCoord.xy / resolution.xy );
	vec2 suv = vec2(((uv.x-0.5)*(resolution.x / resolution.y))+0.5,uv.y);
	vec2 suv2 = suv+(sin((uv.y+uv.x*20.)+time)/20.);
	float atans2 = (atan(suv2.x-0.5,suv2.y-0.5)+PI)/(PI*2.);
	float matan = fract(atans2+(time/20.));
	float a = sin(matan*70.)*300.;
	float b = sin((matan+(time*0.1))*70.)*shrp;
	float c = sin((matan+0.2)*70.)*shrp;
	float d = clamp((1.-(length(suv-0.5))-0.6)*shrp,0.,1.);
	d -= clamp((1.-(length(suv-0.5))-0.65)*shrp,0.,1.);
	//d += clamp((1.-(length(suv-0.5))-0.7)*shrp,0.,1.);
	d += clamp(clamp((1.-(length(suv-vec2(0.5,0.2)))-0.7)*shrp,0.,1.)-step(suv.y,0.35),0.,1.);
	d += clamp((1.-(length(suv-vec2(0.3,0.6)))-0.95)*shrp,0.,1.);
	d += clamp((1.-(length(suv-vec2(0.7,0.6)))-0.95)*shrp,0.,1.);
	d = clamp(d,0.,1.);
	vec3 colors = clamp(vec3(a,b,c),0.,1.);
	gl_FragColor = vec4((colors*d), 1.0 );

}
