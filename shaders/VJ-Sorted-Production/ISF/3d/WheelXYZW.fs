/*{
    "DESCRIPTION": "WheelXYZW",
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
        },
        {
            "NAME": "inputColour",
            "TYPE": "vec4",
            "LABEL": "Input Colour"
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

uniform vec4 inputColour;

#define q(a,b,c) (abs(fract(a/b)-.5)-c*.5)*b
#define u resolution
void main()
{
	vec3 p=vec3(0,-0,time);
	for(float i=-0.;i<1.;i+=.005){
		float r=length(p.xy),
		      d=1.+.3*cos(time/3.),
			a=q(r,3.,.2),b=q(p.z,1.,d),c=q((atan(p.y,p.x)+time*.3*cos(floor(r/3.))*cos(floor(p.z)*13.73))*r,(acos(mouse.y)*r),d),
		      e=min(max(a,max(b,c)),.25),
		      s=3.*i*i-2.*i*i*i-i;
		p+=e*normalize(vec3((2.*gl_FragCoord.xy-u)/max(u.x,u.y),mouse.x));
		gl_FragColor=vec4(s,i*(i-1.),-2.*s,1)*.5+i+pow(2./(1.+abs(max(max(min(a,b),min(b,c)),min(a,c))*20.)*2.),7.);
		if(e<.001)break;
	}
}

