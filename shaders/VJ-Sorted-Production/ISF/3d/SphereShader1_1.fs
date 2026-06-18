/*{
    "DESCRIPTION": "SphereShader1",
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
        "3d"
    ]
}*/



#define time ((useFrameIndex ? (float(FRAMEINDEX) / fps) : TIME) * speed)
#define mouse vec2(mouseX, mouseY)
#define resolution (RENDERSIZE / zoom)
// http://glslsandbox.com/e#17665.7
// Ball mask
#ifdef GL_ES
precision mediump float;
#endif

float pi = atan(1.)*4.;

float backStripes = 32.0;
vec3 backColor1 = vec3(0.345,0.812,0.929);
vec3 backColor2 = vec3(0.039,0.580,0.717);

float sphereStripes = 16.0;
float sphereMinSize = 0.8;
float sphereMaxSize = 1.0;
float spherePulseSpeed = 1.0;
float sphereShadowSize = 0.06;
float sphereSpinSpeed = 2.0;
vec3 sphereRotationAxis = vec3(0.5,1,0.5);
vec3 sphereColor1 = vec3(0.25);
vec3 sphereColor2 = vec3(1.00);

//Axis-Angle rotation using Rodrigues' rotation formula
vec3 rotate(vec3 axis, float ang, vec3 vec)
{
	axis = normalize(axis);
	return vec * cos(ang) + cross(axis, vec) * sin(ang) + axis * dot(axis, vec) * (1.0 - cos(ang));
}

void _userMain( void ) 
{
	vec2 res = vec2(resolution.x / resolution.y, 1.0);
	vec2 cen = res / 2.0;
	vec2 p = ( gl_FragCoord.xy / resolution.y ) - cen;
	p *= 4.0;
	
	vec3 col;
	
	//Sphere size & blending mask
	float midSize = (sphereMaxSize + sphereMinSize) / 2.0;
	float deltaSize = (sphereMaxSize - sphereMinSize);
	float size = sin(time * spherePulseSpeed) * deltaSize + midSize;	
	float mask = smoothstep(size, size - 0.01, length(p));
	
	//Background stripes
	float split = step(p.y, 0.0) * 2.0 - 1.0;
	float back = sin((p.x + p.y * split) * pi * 0.125 * backStripes);
	back = smoothstep(0.0, 0.01, back);
	col += mix(backColor1, backColor2, back);
	
	//Shadow
	col *= smoothstep(size + sphereShadowSize, size + sphereShadowSize + 0.01, length(p)) * 0.5 + 0.5;
	col *= 1.0 - mask;
	
	//Sphere height at point p
	float height = sqrt(abs(p.x * p.x + p.y * p.y - size * size)) * mask;
	
	//Pixel's position in 3D based on screen position and sphere height
	vec3 pos = vec3(p, height);
	
	//Rotate the 3d position around the rotation axis
	pos = rotate(sphereRotationAxis, time * sphereSpinSpeed, pos);
	
	//Stripes on the sphere
	float pitch = atan(length(pos.xz), pos.y);
	float bands = sin(pitch * sphereStripes - pi);
	bands = smoothstep(0.0, 0.1, bands);
	col += mix(sphereColor1, sphereColor2, bands) * mask;
	
	gl_FragColor = vec4( col, 1.0 );

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