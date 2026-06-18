/*{
    "DESCRIPTION": "Colors",
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
precision highp int;
#endif

// Colors
const vec3 cGreen = vec3(0.000, 0.847, 0.094);
const vec3 cAzure = vec3(0.359, 0.578, 0.984);
const vec3 cBeige = vec3(0.984, 0.734, 0.688);
const vec3 cBrown = vec3(0.781, 0.297, 0.047);
const vec3 cBlack = vec3(0.000, 0.000, 0.000);
const vec3 cWhite = vec3(1.000, 1.000, 1.000);

vec3 brick(vec2 p) {
	if (p.y >= 6.0) {
		if (p.x < 10.0) {
			if (p.x < 9.0) {
				int n = 0;
				if (p.x == 0.0) n += 1;
				if (p.y == 15.0) n += 1;
				return (n == 1) ? cBeige : cBrown;
			} else return cBlack;
		} else {
			// Top-right
			vec3 col;
			col = (p.x == 10.0) ? cBeige : p.x == 15.0 ? cBlack : cBrown;
			if (p.y == 15.0) return (col == cBrown ? cBeige : cBrown);
			if (p.y == 10.0) return (col == cBrown ? cBlack : cBrown);
			if (p.y == 9.0)  return (col == cBrown ? cBeige : col);
			if (p == vec2(11.0)) return cBlack;
			return col;
		}
	} else {
		int n = (p.x < 8.0 ? 6 : 12) - int(p.y);
		float b = (n ==  0) ? 3284.0 : (n ==  6) ? 5465.0 :
			  (n ==  1) ? 3312.0 : (n ==  7) ? 5465.0 :
			  (n ==  2) ? 6483.0 : (n ==  8) ? 5466.0 :
			  (n ==  3) ? 4413.0 : (n ==  9) ? 5466.0 :
			  (n ==  4) ? 5466.0 : (n == 10) ? 6195.0 :
			  (n ==  5) ? 4372.0 : (n == 11) ? 4371.0 : 0.0;
		int k = int(mod(b / pow(3.0, mod(p.x, 8.0)), 3.0));
		return (k == 0) ? cBeige : (k == 1) ? cBrown : cBlack; 
	}
	return cBrown;
}

// Pipes by LJ, hope they're not too shabby
vec4 pipe(in vec2 p, in float height) {
	p.x -= 64.;
	vec4 c = vec4(.0);
	float xs = step(p.y,height-8.)*2.;
	if (abs(p.x) < 12.-xs && p.y < height) {
		c = vec4(cGreen,1.);
		c.xyz *= clamp(floor((pow(p.x + xs + 12.,.9) / 15.)*4.5)*.3,.2,1.);
		if ((p.y <= height-8. && p.y >= height-8.5) || p.x < -10.+xs || p.y > height-2.)
			c.xyz *= .6;
	}
	return c;
}

vec3 pixel_at(vec2 p) {
	/* x and y should be "integers". */
	if (fract(p.x) > 0.01 && fract(p.x) < 0.99) {
		return vec3(1., 0., 0.);
	}
	if (fract(p.y) > 0.01 && fract(p.y) < 0.99) {
		return vec3(0., 1., 0.);
	}
	
	if (p.y < 32.0) {
		return brick(mod(p, 16.0));
	} else {
		vec4 c = pipe(vec2(mod(p.x,128.),p.y), 80.-step(mod(p.x,256.),128.)*24.*sin(time));
		if (c.a >.01)
			return c.xyz;
	}
	return cAzure;
}

void main(void) {
	vec2 p = gl_FragCoord.xy + vec2(floor(time * 16.0), 0);
	gl_FragColor = vec4(pixel_at(floor(p / 4.0)), 1.0);
}
