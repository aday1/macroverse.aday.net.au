/*{
    "DESCRIPTION": "fractal1",
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
        "fractal"
    ]
}*/



#define time ((useFrameIndex ? (float(FRAMEINDEX) / fps) : TIME) * speed)
#define mouse vec2(mouseX, mouseY)
#define resolution (RENDERSIZE / zoom)
// ref:
// http://blog.hvidtfeldts.net/index.php/2011/11/distance-estimated-3d-fractals-vi-the-mandelbox/

#ifdef GL_ES
precision mediump float;
#endif

void sphereFold(inout vec3 z, inout float dz)
{
    const float fixedRadius2 = 1.0;
    const float minRadius2 = 0.5;
    float r2 = dot(z,z);
    if (r2<minRadius2) { 
        // linear inner scaling
        float temp = (fixedRadius2/minRadius2);
        z *= temp;
        dz*= temp;
    } else if (r2<fixedRadius2) { 
        // this is the actual sphere inversion
        float temp =(fixedRadius2/r2);
        z *= temp;
        dz*= temp;
    }
}
void boxFold(inout vec3 z, inout float dz)
{
    const float foldingLimit = 2.0;
    z = clamp(z, -foldingLimit, foldingLimit) * 2.0 - z;
}
float map(vec3 z)
{
	z.y = z.y + time*0.1;
	z = mod(z, 8.5) -4.25;

    float Scale = -2.7;
    vec3 offset = z;
    float dr = 1.0;
    for(int n = 0; n<16; n++) {
        boxFold(z,dr);
        sphereFold(z,dr);
        z = Scale*z + offset;
        dr = dr*abs(Scale)+1.0;
    }
    float r = length(z);
    return r/abs(dr);
}

void _userMain( void ) {
	
	vec2 pos = (gl_FragCoord.xy*2.0 - resolution.xy) / resolution.y;
	vec3 camPos = vec3(6.9, 0.0, 6.7);
	vec3 camDir = normalize(vec3(0.0, -0.2, -1.0));

    float sr = sin(time*0.05);
    float cr = cos(time*0.05);
    mat3 rotz = mat3(
        cr, sr, 0,
        sr,-cr, 0,
         0,  0, 1 );
    mat3 roty = mat3(
      cr, 0, sr,
       0, 1,  0,
     -sr, 0, cr );
	camDir = normalize(roty * vec3(0.0, -0.2, -1.0));
	vec3 camUp  = normalize(vec3(0.0, 1.0, 0.0));
	vec3 camSide = cross(camDir, camUp);
	float focus = 1.8;
	
	vec3 rayDir = normalize(camSide*pos.x + camUp*pos.y + camDir*focus);
	vec3 ray = camPos;
	float m = 0.0;
	float d = 0.0, total_d = 0.0;
	const int MAX_MARCH = 64;
	const float MAX_DISTANCE = 1000.0;
	for(int i=0; i<MAX_MARCH; ++i) {
		d = map(ray);
		total_d += d;
		ray += rayDir * d;
		m += 1.0;
		if(d<0.001) { break; }
		if(total_d>MAX_DISTANCE) { break; }
	}

	float r = mod(time*2.0, 20.0);
	float glow = max((mod((ray.x+ray.y+ray.z)-time*2.0, 15.0)-13.0)/2.0, 0.0);
	vec3 gp = abs(mod(ray, vec3(1.2)));
	if(gp.x<1.175 && gp.z<1.175) {
		glow = 0.0;
	}	
	
	float c = (total_d)*0.05;
	vec4 result = vec4( vec3(c, c, c*2.0) + vec3(0.01, 0.01, 0.02)*m*0.1, 1.0 );
	result.xyz += vec3(glow*1.0, glow*1.0, glow*2.5);
	gl_FragColor = result;
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