/*{
    "DESCRIPTION": "CrazyInsaneColorSpectrum1",
    "CREDIT": "GLSL Sandbox / various",
    "ISFVSN": "2.0",
    "CATEGORIES": [
        "color"
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
            "NAME": "speed",
            "TYPE": "float",
            "DEFAULT": 1.0,
            "MIN": 0.0,
            "MAX": 5.0,
            "LABEL": "Speed"
        },
        {
            "NAME": "mouseX",
            "TYPE": "float",
            "DEFAULT": 0.0,
            "MIN": -1.0,
            "MAX": 1.0,
            "LABEL": "Mouse X"
        },
        {
            "NAME": "mouseY",
            "TYPE": "float",
            "DEFAULT": 0.0,
            "MIN": -1.0,
            "MAX": 1.0,
            "LABEL": "Mouse Y"
        },
        {
            "NAME": "zoom",
            "TYPE": "float",
            "DEFAULT": 1.0,
            "MIN": 0.1,
            "MAX": 4.0,
            "LABEL": "Zoom"
        },
        {
            "NAME": "colorR",
            "TYPE": "float",
            "DEFAULT": 1.0,
            "MIN": 0.0,
            "MAX": 2.0,
            "LABEL": "Color Red"
        },
        {
            "NAME": "colorG",
            "TYPE": "float",
            "DEFAULT": 1.0,
            "MIN": 0.0,
            "MAX": 2.0,
            "LABEL": "Color Green"
        },
        {
            "NAME": "colorB",
            "TYPE": "float",
            "DEFAULT": 1.0,
            "MIN": 0.0,
            "MAX": 2.0,
            "LABEL": "Color Blue"
        },
        {
            "NAME": "brightness",
            "TYPE": "float",
            "DEFAULT": 0.0,
            "MIN": -1.0,
            "MAX": 1.0,
            "LABEL": "Brightness"
        },
        {
            "NAME": "saturation",
            "TYPE": "float",
            "DEFAULT": 1.0,
            "MIN": 0.0,
            "MAX": 3.0,
            "LABEL": "Saturation"
        },
        {
            "NAME": "contrast",
            "TYPE": "float",
            "DEFAULT": 1.0,
            "MIN": 0.0,
            "MAX": 3.0,
            "LABEL": "Contrast"
        },
        {
            "NAME": "hueShift",
            "TYPE": "float",
            "DEFAULT": 0.0,
            "MIN": 0.0,
            "MAX": 1.0,
            "LABEL": "Hue Shift"
        },
        {
            "NAME": "invert",
            "TYPE": "bool",
            "DEFAULT": 0,
            "LABEL": "Invert Colors"
        }
    ],
    "TAGS": [
        "color"
    ]
}*/



#define time ((useFrameIndex ? (float(FRAMEINDEX) / fps) : TIME) * speed)
#define mouse vec2(mouseX, mouseY)
#define resolution (RENDERSIZE / zoom)
#ifdef GL_ES
precision mediump float;
#endif

//Random shader generator. anastadunba
float spd = 20.;
#define SEED floor(time*spd)*1.42
#define SEED2 floor(time*spd)*3.22
#define SEED3 floor(time*spd)*1.52
#define SEED4 floor(time*spd)*2.42

float rand(float co){
    return fract(sin(dot(vec2(co) ,vec2(12.9898,78.233))) * 43758.5453);
}

#define w_m 7.
//Get variable
#define w(a,c) ((float(a == 0.)*uv.x)+(float(a == 1.)*uv.y)+(float(a == 2.)*(1.-uv.x))+(float(a == 3.)*(1.-uv.y))+(float(a == 4.)*fract(time))+(float(a == 5.)*c)+(float(a == 6.)*length(uv-.5))+(float(a == 7.)*atan(uv.x-.5,uv.y-.5))) 
//Get color channel
#define g(d) ((float(floor(mod(d,4.)) == 0.)*color.r)+(float(floor(mod(d,4.)) == 1.)*color.g)+(float(floor(mod(d,4.)) == 2.)*color.b))

#define interact_m 12.
float interact(float a, float b, float type) {
	type = mod(floor(type*interact_m),interact_m+1.);
	float j = 0.;
	if (type == 0.) { j = a+b; }
	if (type == 1.) { j = a-b; }
	if (type == 2.) { j = a*b; }
	if (type == 3.) { j = a/b; }
	if (type == 4.) { j = pow(a,b*2.); }
	if (type == 5.) { j = mod(a,b); }
	if (type == 6.) { j = step(a,b); }
	if (type == 7.) { j = rand(a)*b; }
	if (type == 8.) { if (a*3. > b*3.) j = a; }
	if (type == 9.) { if (a*3. < b*3.) j = a; }
	if (type == 10.) { j = sqrt(pow(a,2.)+pow(b,2.)); }
	if (type == 11.) { j = floor(a*(b*7.))/(b*7.); }
	if (type == 12.) { j = sin(a*b*10.); }
	return j;
}

void _userMain( void ) {

	vec2 uv = ( gl_FragCoord.xy / resolution.xy );
	const int loops = 14;
	vec3 color = vec3(w(SEED3,uv.x),w(SEED3+1.,uv.y),w(SEED3+2.,uv.x*uv.y));
	float d = 0.;
	float d2 = 0.;
	for (int j = 0; j < loops; j++) {
	    float i = float(j);
	    d2 += rand(i+SEED3);
	    d += rand(i+SEED4);
	    color.r = interact(w(mod(floor(i*SEED),w_m+1.),g(rand(i+d2)*4.)) , w(mod(floor(i*SEED2),w_m+1.),g(rand(i+d2)*4.)) , rand(SEED3+i+d));
	    d2 += rand(i+SEED3);
	    d += rand(i+SEED4);
	    color.g = interact(w(mod(floor(i*SEED),w_m+1.),g(rand(i+d2)*4.)) , w(mod(floor(i*SEED2),w_m+1.),g(rand(i+d2)*4.)) , rand(SEED3+i+d));
	    d2 += rand(i+SEED3);
	    d += rand(i+SEED4);
	    color.b = interact(w(mod(floor(i*SEED),w_m+1.),g(rand(i+d2)*4.)) , w(mod(floor(i*SEED2),w_m+1.),g(rand(i+d2)*4.)) , rand(SEED3+i+d));
	}
	
	gl_FragColor = vec4(fract(color), 1.0 );

}

void main() {
    _userMain();
    vec3 c = gl_FragColor.rgb;
    float a = gl_FragColor.a;
    float luma = dot(c, vec3(0.299, 0.587, 0.114));
    c = mix(vec3(luma), c, saturation);
    c = (c - 0.5) * contrast + 0.5;
    c *= vec3(colorR, colorG, colorB);
    c += brightness;
    if (hueShift > 0.001) {
        float cosH = cos(hueShift * 6.28318);
        float sinH = sin(hueShift * 6.28318);
        c = vec3(
            c.r * (0.299 + 0.701*cosH + 0.168*sinH) + c.g * (0.587 - 0.587*cosH + 0.330*sinH) + c.b * (0.114 - 0.114*cosH - 0.497*sinH),
            c.r * (0.299 - 0.299*cosH - 0.328*sinH) + c.g * (0.587 + 0.413*cosH + 0.035*sinH) + c.b * (0.114 - 0.114*cosH + 0.292*sinH),
            c.r * (0.299 - 0.300*cosH + 1.250*sinH) + c.g * (0.587 - 0.588*cosH - 1.050*sinH) + c.b * (0.114 + 0.886*cosH - 0.203*sinH)
        );
    }
    if (invert) c = 1.0 - c;
    gl_FragColor = vec4(clamp(c, 0.0, 1.0), a);
}