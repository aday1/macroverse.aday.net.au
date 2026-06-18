/*{
    "DESCRIPTION": "byrotwang",
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
// by rotwang

#ifdef GL_ES
precision mediump float;
#endif
//getting this to compile on my gpu. -gt

const float PI = 3.1415926535;

float max3(float a,float b,float c)
{
	return max(a, max(b,c));
}

float rect( vec2 p, vec2 b, float smooth )
{
	vec2 v = abs(p) - b;
  	float d = length(max(v,0.0));
	return 1.0-pow(d, smooth);
}

void main( void ) {

	vec2 unipos = (gl_FragCoord.xy / resolution);
	vec2 pos = unipos*2.0-1.0;
	pos.x *= resolution.x / resolution.y;

	float flash = sin(time*0.001);
	float uflash = flash*0.5+0.5;

	// scroll
	//pos.x -= sin(time*0.5)*1.0;
	
	float d1 = rect(pos - vec2(-0.3,0.0), vec2(0.1,1), 0.05*(1.+0.5*sin(time*3.))); 
	vec3 clr1 = vec3(0.2,0.6,1.0) *d1; 
	
	float d2 = rect(pos - vec2(0.0,0.0), vec2(0.1,1), 0.05*(1.+0.5*sin(time*3.))); 
	vec3 clr2 = vec3(0.6,0.99,0.2) *d2; 

	float d3 = rect(pos - vec2(0.3,0.0), vec2(0.1,1), uflash*0.1*(1.+0.5*sin(time*3.))); 
	vec3 clr3 = vec3(1.0,0.0,0.2) *0.75*d3 + (0.8*flash); 
	
	float d4 = rect(pos-vec2(1.0,0.0),vec2(0.1, 0.2),.01*(1.+0.5*sin(time*3.)));
	vec3 clr4 = vec3(d4);

	vec3 clr = vec3(clr1+clr2+clr3+clr4);
	gl_FragColor = vec4( vec3(clr) , 1.0 );

}
