/*{
    "DESCRIPTION": "fractal2",
    "CREDIT": "GLSL Sandbox / various",
    "ISFVSN": "2.0",
    "CATEGORIES": [
        "fractal"
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
        "fractal"
    ]
}*/



#define time ((useFrameIndex ? (float(FRAMEINDEX) / fps) : TIME) * timeScale)
#define mouse vec2(mouseX, mouseY)
#define resolution RENDERSIZE
#ifdef GL_ES
precision mediump float;
#endif

//this is some crazy fractal shit!
//MrOMGWTF

// piersh - change palette, animation range

vec3 Hue(float H)
{
	H *= 6.;
	return clamp(vec3(
		abs(H - 3.) - 1.,
		2. - abs(H - 2.),
		2. - abs(H - 4.)
	), 0., 1.);
}

vec3 HSVtoRGB(float h, float s, float v)
{
    return ((Hue(h) - 1.) * s + 1.) * v;
}

float wave(float t) { return .5 - .5 * cos (t); }

float cabs (const vec2 c) { return dot(c,c); }

void main( void ) {

	vec2 z = ( gl_FragCoord.xy / resolution.xy  * 2.0) - 1.0;
	float color = 0.0;
	float m;
	vec2 c = vec2 (
		sin(time * 0.2312353),
		cos(time * 0.3591752)
	) + 1.5;
	
	//c = (mouse * 2. - 1.) * 3.;
	
	for(int i = 0; i < 10; i++)
	{
		vec2 zold = z;
		z = abs(z);
		vec2 az = atan(z*z)*z;
		m = atan(az.x + az.y);
		z = z / m - c + (mouse * 2. - 1.) * 3.;
		
		color += exp(-cabs(z));
		color += exp(-1./cabs(zold - z));
	}
	
	color /= 20.;
	
	gl_FragColor = vec4(HSVtoRGB(
		wave(color + time / 10.),
		wave(color * 3.7891),
		wave(color * 13.3234)
	), 1.0);	
}
