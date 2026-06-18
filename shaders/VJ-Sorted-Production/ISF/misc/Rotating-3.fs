/*{
    "DESCRIPTION": "Rotating-3",
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

float pi = atan(1.)*4.;
float m = 1.; // Change the speed easily with this multiplier!

mat2 rotate(float a)
{
	a = mod(a,2.*pi);
	return mat2(cos(a),-sin(a),
		    sin(a), cos(a));
}

float clength(vec2 v)
{
	return max(abs(v.x),abs(v.y));
}

void main( void ) {

	vec2 res = vec2(resolution.x/resolution.y,1.0);
	vec2 p = ( gl_FragCoord.xy / resolution.y );
	p -= res/2.;
	
	vec2 rp;
	vec3 c;
	
	rp = p * rotate(time * m);
	c.r = smoothstep(0.35,0.345,clength(rp))-smoothstep(0.21,0.205,clength(rp));
	
	rp = p * rotate(time*.75 * m);
	c.g = smoothstep(0.35,0.345,clength(rp))-smoothstep(0.21,0.205,clength(rp));
	
	rp = p * rotate(time*.5 * m);
	c.b = smoothstep(0.35,0.345,clength(rp))-smoothstep(0.21,0.205,clength(rp));
	
	gl_FragColor = vec4( c , 1.0 );

}
