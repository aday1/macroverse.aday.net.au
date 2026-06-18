/*{
    "DESCRIPTION": "HorizonLight-DotMatrix-4",
    "CREDIT": "GLSL Sandbox / various",
    "ISFVSN": "2.0",
    "CATEGORIES": [
        "particles"
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
        "particles"
    ]
}*/




#define time ((useFrameIndex ? (float(FRAMEINDEX) / fps) : TIME) * speed)
#define mouse vec2(mouseX, mouseY)
#define resolution (RENDERSIZE / zoom)
#ifdef GL_ES
precision mediump float;
#endif

//Stupid mode7 bullshit
//Written by ninjapretzel

float rand(vec2 c) {
	return fract(abs(sin(sin(5.321+c.x) * (c.y+8.123)) * 4.654));
}

float noise(vec2 c) {
	float corners = rand(c+vec2(1.,1.)) + rand(c+vec2(-1.,1.)) + rand(c+vec2(1.,-1.)) + rand(c+vec2(-1.,-1.));
	float sides = rand(c+vec2(1.,0.)) + rand(c+vec2(0.,1.)) + rand(c+vec2(-1.,0.)) + rand(c+vec2(0.,-1.));
	float center = rand(c);
	return corners / 16. + sides / 3. + center / 4.;
}

#define STEP 1.
float inoise(vec2 c) {
	float ix = floor(c.x);
	float fx = fract(c.x);
	
	float iy = floor(c.y);
	float fy = fract(c.y);
	
	float v1 = noise(vec2(ix	, iy	));
	float v2 = noise(vec2(ix + STEP	, iy	));
	float v3 = noise(vec2(ix	, iy + STEP));
	float v4 = noise(vec2(ix + STEP	, iy + STEP));
	
	float i1 = mix(v1, v2, fx);
	float i2 = mix(v3, v4, fx);
	
	return mix(i1, i2, fy);
	
}

float pnoise(vec2 c) {
	float total = 0.;
	float t = 0.;
	
	float freq = 1.;
	float amp = 1.;
	
	for (float i = 0.; i < 10.; i += 1.) {
		t += amp;
		total += inoise(c * freq) * amp;
		freq *= 2.;
		amp *= .852;
	}
	
	return ((total / t) - .4) * 5.;
}

vec3 texture(vec2 position) {
	float height = pnoise(position * .02);
	float moisture = pnoise(position * .022);
	
	float hab = moisture - height;
	if (height < .40) {//water
		
		float x = pnoise(position * 42.4 + 15. * sin(vec2(time * .5)));
		return vec3(x * .4, x * .6, 1. * x);
	}

	if (moisture > .5 && hab > 0.) {//grass
		float x = pnoise(position * 23.4);

		vec3 colora = vec3(.0, .2, .0);
		vec3 colorb = vec3(.1, .7, .2);
		
		//vec3(x * .2, x, x * .1);
		return mix(colora, colorb, x);
	}
	
	if (height > .55 ) {//snow & lava
		if (hab > -.15) {
			float x = pnoise(position * 32.4);
			return vec3(1.-x*.2, 1.-x*.2, 1.-x*.2);
		} else if (hab < -.3) {
			float x = pnoise(position * 42.4 + 5. * sin(vec2(time)));
			return vec3(.8 + x*x, .4 + x*x, 0);
		}
		
	}
	//dirt
	float x = pnoise(position * 32.4);
	return vec3(x, x*.4, x*.15);
}

vec3 skyTex(vec2 position) {
	float clouds = pnoise(position * .15);
	
	float x = pnoise(position * .4);
	float y = pnoise(position * .1);
	if (clouds > .55) {
		return vec3(.6 + x*x, .7 + y*x, .7 + y*y);
	}
	return vec3(.2, .3+x/6., .3 + y/4.);
}

void _userMain( void ) {
	vec2 center =  resolution.xy * .5;
	vec2 position = ( gl_FragCoord.xy ) * 1. - center;
	vec2 ncoord = gl_FragCoord.xy / resolution.xy;
	
	float horizon = .7;
	float horizonpx = center.y - horizon * resolution.y;
	
	float fov = .6;
	float fovpx = fov * resolution.x;
	
	vec2 off = vec2(time * .01 , time * -2.);
	
	float px = position.x;
	float py = fovpx;
	float pz = position.y + horizonpx;
	vec2 tc = vec2(px/pz, py/pz);
	vec4 color = vec4(0, 0, 0, 1);
	
	if (ncoord.y > horizon) {
		color.rgb = skyTex(tc - off * .3);
	} else {
		color.rgb =  texture(tc + off);
	}
	
	gl_FragColor = color;
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