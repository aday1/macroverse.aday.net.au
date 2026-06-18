/*{
    "DESCRIPTION": "DotMatrix-Zooming-102",
    "CREDIT": "GLSL Sandbox / various",
    "ISFVSN": "2.0",
    "CATEGORIES": [
        "geometric"
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
        "geometric"
    ]
}*/




#define time ((useFrameIndex ? (float(FRAMEINDEX) / fps) : TIME) * speed)
#define mouse vec2(mouseX, mouseY)
#define resolution (RENDERSIZE / zoom)
#ifdef GL_ES
precision mediump float;

#endif

#pragma optionNV(fastmath off)
#pragma optionNV(fastprecision off)

vec2 ds_set(float a)
{
 vec2 z;
 z.x = a;
 z.y = 0.0;
 return z;
}

vec2 ds_add (vec2 dsa, vec2 dsb)
{
vec2 dsc;
float t1, t2, e;
 
 t1 = dsa.x + dsb.x;
 e = t1 - dsa.x;
 t2 = ((dsb.x - e) + (dsa.x - (t1 - e))) + dsa.y + dsb.y;
 
 dsc.x = t1 + t2;
 dsc.y = t2 - (dsc.x - t1);
 return dsc;
}

vec2 ds_mul (vec2 dsa, vec2 dsb)
{
vec2 dsc;
float c11, c21, c2, e, t1, t2;
float a1, a2, b1, b2, cona, conb, split = 8193.;
 
 cona = dsa.x * split;
 conb = dsb.x * split;
 a1 = cona - (cona - dsa.x);
 b1 = conb - (conb - dsb.x);
 a2 = dsa.x - a1;
 b2 = dsb.x - b1;
 
 c11 = dsa.x * dsb.x;
 c21 = a2 * b2 + (a2 * b1 + (a1 * b2 + (a1 * b1 - c11)));
 
 c2 = dsa.x * dsb.y + dsa.y * dsb.x;
 
 t1 = c11 + c2;
 e = t1 - c11;
 t2 = dsa.y * dsb.y + ((c2 - e) + (c11 - (t1 - e))) + c21;
 
 dsc.x = t1 + t2;
 dsc.y = t2 - (dsc.x - t1);

 return dsc;
}

vec2 ds_sub (vec2 dsa, vec2 dsb)
{
vec2 dsc;
float e, t1, t2;

 t1 = dsa.x - dsb.x;
 e = t1 - dsa.x;
 t2 = ((-dsb.x - e) + (dsa.x - (t1 - e))) + dsa.y - dsb.y;

 dsc.x = t1 + t2;
 dsc.y = t2 - (dsc.x - t1);
 return dsc;
}

struct Complex {
	vec2 r;
	vec2 i;
};
	
Complex fromVec2(vec2 vec) {
	return Complex(ds_set(vec.x),ds_set(vec.y));
}

Complex add(Complex c1,Complex c2) {
	return Complex(ds_add(c1.r,c2.r),ds_add(c1.i,c2.i));
}

Complex square(Complex c) {
	vec2 r = ds_sub(ds_mul(c.r,c.r),ds_mul(c.i,c.i));
	
	vec2 i = ds_mul(ds_mul(c.r,c.i),ds_set(2.0));
	
	return Complex(r,i);
}

float ds_compare(vec2 dsa, vec2 dsb)
{
 if (dsa.x < dsb.x) return -1.;
 else if (dsa.x == dsb.x) 
	{
	if (dsa.y < dsb.y) return -1.;
	else if (dsa.y == dsb.y) return 0.;
	else return 1.;
	}
 else return 1.;
}

vec2 four;

bool lengthGreaterThen2(Complex c) {
	vec2 r = ds_add(ds_mul(c.r,c.r),ds_mul(c.i,c.i));
	
	return ds_compare(r,four) == 1.0;	
}

void _userMain( void ) {

	four = ds_set(4.0);
	
	vec2 position = ( gl_FragCoord.xy / resolution.xy );
	
	Complex z = fromVec2(position);

	z.r = ds_mul(z.r,ds_set(3.0));
	z.i = ds_mul(z.i,ds_set(2.0));

	z.r = ds_sub(z.r,ds_set(2.0));
	z.i = ds_sub(z.i,ds_set(1.0));

	vec2 scale = ds_set(0.1);
	for (int i =0 ;i < 5; ++i) {
		scale = ds_mul(scale,ds_set(0.1));
	}

	z.r = ds_mul(z.r,scale);
	z.i = ds_mul(z.i,scale);
	
	z.r = ds_sub(z.r,ds_set(0.73));
	z.i = ds_sub(z.i,ds_set(0.225));
	
	//z += mouse / scale;
	
	Complex c = z;

	vec3 color = vec3(0);
	
	float it = 0.0;
	
	for (int i = 0; i < 256; ++i) {
		z = add(square(z),c);
		
		if (lengthGreaterThen2(z)) {
			break;
		}
		it += 1.0; 
	}
	
	if (it < 256.0) {
		color = vec3(sin(it / 10.0 + time),cos(it / 8.0),sin(it / 3.0));
	}

	gl_FragColor = vec4( color, 1.0 );

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