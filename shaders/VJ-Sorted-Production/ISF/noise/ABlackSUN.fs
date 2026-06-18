/*{
    "DESCRIPTION": "ABlackSUN",
    "CREDIT": "GLSL Sandbox / various",
    "ISFVSN": "2.0",
    "CATEGORIES": [
        "noise"
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
        "noise"
    ]
}*/



#define time ((useFrameIndex ? (float(FRAMEINDEX) / fps) : TIME) * timeScale)
#define mouse vec2(mouseX, mouseY)
#define resolution RENDERSIZE
#ifdef GL_ES
precision mediump float;
#endif

float snoise(vec3 uv, float res)
{
	const vec3 s = vec3(1e0, 1e2, 1e3);
	
	uv *= res;
	
	vec3 uv0 = floor(mod(uv, res))*s;
	vec3 uv1 = floor(mod(uv+vec3(1.), res))*s;
	
	vec3 f = fract(uv); f = f*f*(3.0-2.0*f);

	vec4 v = vec4(uv0.x+uv0.y+uv0.z, uv1.x+uv0.y+uv0.z,
		      	  uv0.x+uv1.y+uv0.z, uv1.x+uv1.y+uv0.z);

	vec4 r = fract(sin(v*1e-1)*1e3);
	float r0 = mix(mix(r.x, r.y, f.x), mix(r.z, r.w, f.x), f.y);
	
	r = fract(sin((v + uv1.z - uv0.z)*1e-1)*1e3);
	float r1 = mix(mix(r.x, r.y, f.x), mix(r.z, r.w, f.x), f.y);
	
	return mix(r0, r1, f.z)*2.-1.;
}

void main(  ) 
{
	vec2 p1 = -0.5 + gl_FragCoord.xy / resolution.xy;
	p1.x *= resolution.x/resolution.y;
    
    vec2 p2 = -0.5 + gl_FragCoord.xy / resolution.xy;
	p2.x *= resolution.x/resolution.y;
//===============================================	
    
	float color1;
    if(length(2.*p1)<=mouse.y){
        color1=0.;
    }
    else color1= 3.0 - ((2.+cos(time)*mouse.x)*length(2.*p1));
    
    float color2;
    if(length(2.*p2)>=0.6||length(2.*p2)<=0.37){
        color2=0.;
    }
    
    else color2= -3.0 + ((8.+sin(time*2.)*1.5)*length(2.*p2));
//===============================================   
	
	vec3 coord1 = vec3(atan(p1.x,p1.y)/6.2832+.5, length(p1)*.4, .5);
	vec3 coord2 = vec3(atan(p2.x,p2.y)/6.2832+.5, 2.4-length(p2)*.4, .5);
    
//===============================================
    
	for(int i = 1; i <= 7; i++)
	{
		float power = pow(2.0, float(i));
		color1 += (1.5 / power) * snoise(coord1 + vec3(0.,-time*.05, time*.01), power*16.);
		color2 += (1.5 / power) * snoise(coord2 + vec3(0.,-time*.05, time*.01), power*16.);
    }
	gl_FragColor = vec4( max(pow(color2,2.)*1.,0.), pow(max(color1,0.),2.)*0.4+pow(max(color2,0.),2.)*0.3, color1, mouse.x);
}
