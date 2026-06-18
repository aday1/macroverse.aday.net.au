/*{
    "DESCRIPTION": "DotMatrix-ElectricArc-14",
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
        "color",
        "3d"
    ]
}*/




#define time ((useFrameIndex ? (float(FRAMEINDEX) / fps) : TIME) * speed)
#define mouse vec2(mouseX, mouseY)
#define resolution (RENDERSIZE / zoom)
// http://glslsandbox.com/e#29651.0

#ifdef GL_ES
precision mediump float;
#endif

float hash( float n )
{
    return fract(sin(n*12.435)*43758.5453123);
}

bool notEmpty(ivec3 p)
{
	if (mod(float(p.z), 2.0) < 0.5)
		return length(vec2(p.xy)) > 1.1+4.0*hash(float(p.z));
	else
		return length(vec2(p.xy)) > 3.1;
}

// return location of first non-empty block
// marching accomplished with fb39ca4's non-branching dda: https://www.shadertoy.com/view/4dX3zl
vec3 intersect(vec3 ro, vec3 rd, out ivec3 ip, out ivec3 n)
{
	ivec3 mapPos = ivec3(floor(ro + 0.));
	vec3 deltaDist = abs(vec3(length(rd)) / rd);
	ivec3 rayStep = ivec3(sign(rd));
	vec3 sideDist = (sign(rd) * (vec3(mapPos) - ro) + (sign(rd) * 0.5) + 0.5) * deltaDist; 
	
	bvec3 mask;
	for (int i = 0; i < 25; i++) {
		if (notEmpty(mapPos)) continue;
		
		bvec3 b1 = lessThan(sideDist.xyz, sideDist.yzx);
		bvec3 b2 = lessThanEqual(sideDist.xyz, sideDist.zxy);
		mask.x = b1.x && b2.x;
		mask.y = b1.y && b2.y;
		mask.z = b1.z && b2.z;
		
		//All components of mask are false except for the corresponding largest component
		//of sideDist, which is the axis along which the ray should be incremented.			
		sideDist += vec3(mask) * deltaDist;
		mapPos += ivec3(mask) * rayStep;
	}
	ip = mapPos;
	n = ivec3(mask)*ivec3(sign(-rd));
	
	// intersect the cube (copied from iq :] )
	vec3 mini = (vec3(mapPos)-ro + 0.5 - 0.5*sign(rd))/rd;
	float t = max ( mini.x, max ( mini.y, mini.z ) );
	
	return ro+rd*t;
}

void mainImage( out vec4 fragColor, in vec2 fragCoord )
{
	vec2 uv = (fragCoord.xy-resolution.xy/2.0) / resolution.yy * 8.0;
	uv.y = -uv.y;

	// perspective projection
	vec3 ro = vec3(0.5,0.5,time*6.0);
	vec3 rd = normalize(vec3(uv+vec2(0.01,0.01),1.0));
	
	ivec3 ip;
	ivec3 n;
	vec3 pos = intersect(ro,rd, ip,n);
	vec3 col = vec3(1.0);
	col.r = sin(float(ip.z-999)/25.0);
	col.g = sin(float(ip.z-99)/24.0);
	col.b = sin(float(ip.z)/23.0);
	col *= 0.2+0.8*hash(float(ip.z+10*ip.x+100*ip.y+99));
	
	vec3 cubeP = mod(pos-0.001,1.0);
	vec3 edgeD = 0.5-abs(cubeP-0.5);
	edgeD += abs(vec3(n));
	float closest = min(edgeD.x,min(edgeD.y,edgeD.z));
	float glow = smoothstep(0.1,0.0,closest);
	col += 0.2*glow;
	col += float(n.z)*0.5;
	col *= 8.0/pow(length(pos.z-ro.z),1.6);

	fragColor = vec4(col,1.0);
}

void _userMain()
{
	vec4 color;
	mainImage(gl_FragColor, gl_FragCoord.xy);
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