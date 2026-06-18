/*{
    "DESCRIPTION": "UNIVERSE-SPACE-EXPLORER1",
    "CREDIT": "GLSL Sandbox / various",
    "ISFVSN": "2.0",
    "CATEGORIES": [
        "space"
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
            "NAME": "inputColour",
            "TYPE": "vec4",
            "LABEL": "Input Colour"
        },
        {
            "NAME": "contrast",
            "TYPE": "float",
            "DEFAULT": 1.0,
            "MIN": 0.0,
            "MAX": 5.0,
            "LABEL": "Contrast"
        }
    ],
    "TAGS": [
        "space",
        "geometric",
        "3d"
    ]
}*/
#define E 2.71828182846

uniform vec4 color;




#define time ((useFrameIndex ? (float(FRAMEINDEX) / fps) : TIME) * speed)
#define mouse vec2(0.0)
#define resolution RENDERSIZE

uniform vec4 inputColour;

#define iterations 17
#define formuparam 0.53

#define volsteps 50
#define stepsize 0.09

#define zoom   0.800
#define tile   0.850
#define speed  0.010 

#define brightness 0.0015
#define darkmatter 0.300
#define distfading 0.730
#define saturation 0.850

void main(void)
{
	//get coords and direction
	vec2 uv=gl_FragCoord.xy/resolution.xy-mouse.y;
	uv.y*=resolution.y/resolution.x;
	vec3 dir=vec3(uv*zoom,inputColour.x);
	float localTime =time*speed+.25;

	//mouse rotation
	float a1=.1+mouse.x/resolution.x*2.;
	float a2=.2+mouse.y/resolution.y*2.;
	mat2 rot1=mat2(cos(a1),sin(a1),-sin(a1),cos(a1));
	mat2 rot2=mat2(cos(a2),sin(a2),-sin(a2),cos(a2));
	dir.xz*=rot1;
	dir.xy*=rot2;
	vec3 from=vec3(1.,.5,mouse.x);
	from+=vec3(localTime*2.,localTime,-2.);
	from.xz*=rot1;
	from.xy*=rot2;
	
	//volumetric rendering
	float s=0.05,fade=3.;
	vec3 v=vec3(0.);
	vec3 c = vec3(mouse.y);
	for (int r=0; r<volsteps; r++) {
		vec3 p=from+s*dir*inputColour.z;
		p = abs(vec3(tile)-mod(p,vec3(tile*inputColour.w))); // tiling fold
		float pa,a=pa=0.;
		for (int i=0; i<iterations; i++) { 
			p=abs(p)/dot(p,p)-formuparam; // the magic formula
			a+=abs(length(p)-pa); // absolute sum of average change
			pa=length(p);
			c.r += inputColour.z * pa;
		}
		float dm=max(0.,darkmatter-a*a*.001); //dark matter
		a*=a*a; // add contrast
		c.g = a/250.;
		c.b = dm * 2000.;
		if (r>6) fade*=inputColour.y-dm; // dark matter, don't render near
		//v+=vec3(dm,dm*.5,0.);
		v+=fade;
		v+=vec3(s,s*s,s*s*s*s)*a*brightness*fade; // coloring based on distance
		fade*=distfading; // distance fading
		s+=stepsize;
	}
	v=mix(vec3(length(v)),v,saturation); //color adjust
        v=mix(v,c,sin(localTime *inputColour.y));
	gl_FragColor = vec4(v*.01,1.);	
	
}
