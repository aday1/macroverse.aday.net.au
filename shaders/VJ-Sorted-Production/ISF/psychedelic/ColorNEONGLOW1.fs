/*{
    "DESCRIPTION": "ColorNEONGLOW1",
    "CREDIT": "GLSL Sandbox / various",
    "ISFVSN": "2.0",
    "CATEGORIES": [
        "psychedelic"
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
        "psychedelic"
    ]
}*/



#define time ((useFrameIndex ? (float(FRAMEINDEX) / fps) : TIME) * timeScale)
#define mouse vec2(mouseX, mouseY)
#define resolution RENDERSIZE
precision mediump float;

// ehh. colors
void main(void){
	vec2 p = gl_FragCoord.xy / resolution.xy;
	vec4 dmin = vec4(1000.);
	vec2 z = (-1.0 + 2.0*p)*vec2(1.7,1.0);
	float w = .1 * p.x * p.y;
	vec2 op = 1./p; //(p - 0.5) * 2.;
	vec2 mv = ((mouse-vec2(0.5))+vec2(1./acos(op.x*w),1.-atan(op.y*w)));
	for( int i=0; i<4; i++ ){
		z = mv+vec2(z.x*z.x-z.y*z.y,2.0*z.x*z.y);
		mv /= mv*.0005;
		z += z*0.05;
		dmin=min(dmin,vec4(abs(0.0+z.y+0.5*sin(z.x)),abs(1.0+z.x+0.5*sin(z.y)),dot(z,z),length(fract(z)-0.5)));
	}	
	vec3 color = vec3( mix(vec3(dot(dmin.rgb, -dmin.gba)), dmin.rgb, 1.0-dmin.a) );
	color = mix( color, vec3(1.00,1.00,0.00),  1.00-min(1.0,pow(dmin.z*1.00,0.15)));
	color = mix( color, vec3(0.00,1.00,1.00),  1.00-min(1.0,pow(dmin.x*0.25,0.20)));
	color = mix( color, vec3(1.00,0.00,1.00),  1.00-min(1.0,pow(dmin.y*0.50,0.50)));
	color = mix( color, vec3(1.00,1.00,0.00),  1.00-min(1.0,pow(dmin.z*1.00,0.15)));
	color = 1.25 * color*color;
	gl_FragColor = vec4(1.0-color*(0.5 + 0.5*pow(16.0*p.x*(1.0-p.x)*p.y*(1.0-p.y),0.15)),1.0);}

// http://glslsandbox.com/e#29622.0
// Created by inigo quilez - iq/2013 // glslsandbox mod by Robert Schütze - trirop/2015 - absurd tweaks by bpt
// License Creative Commons Attribution-NonCommercial-ShareAlike 3.0 Unported License.

