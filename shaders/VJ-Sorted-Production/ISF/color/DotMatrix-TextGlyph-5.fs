/*{
    "DESCRIPTION": "DotMatrix-TextGlyph-5",
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
        "color",
        "texture-input"
    ]
}*/




#define time ((useFrameIndex ? (float(FRAMEINDEX) / fps) : TIME) * speed)
#define mouse vec2(mouseX, mouseY)
#define resolution (RENDERSIZE / zoom)
#ifdef GL_ES
precision mediump float;
#endif

uniform sampler2D backbuffer;

float random(vec2 v) 
{
return fract(sin(dot(v ,vec2(12.9898,78.233))) * (10000.0+time*0.05));
}

void _userMain( void ) 
{
	const int numColors = 3;
	vec3 colors[numColors];
	colors[0] = vec3( 78, 205, 205) / 255.0;
	colors[1] = vec3( 199, 244, 100) / 255.0;
	colors[2] = vec3( 85, 98, 112)/ 255.0;
	
	vec2 screenPos = gl_FragCoord.xy;
	vec2 screenPosNorm = gl_FragCoord.xy / resolution.xy;
	vec2 position = screenPosNorm + mouse / 4.0;
	vec4 prevColor = texture2D( backbuffer, screenPosNorm );

	// calc block
	vec2 screenBlock0 = floor(screenPos*0.16 + vec2(time,0) + mouse*3.0);
	vec2 screenBlock1 = floor(screenPos*0.08 + vec2(time*1.5,0) + mouse*5.0);
	vec2 screenBlock2 = floor(screenPos*0.02 + vec2(time*2.0,0)+mouse*10.0);
	float rand0 = random(screenBlock0);
	float rand1 = random(screenBlock1);
	float rand2 = random(screenBlock2);
	
	float rand = rand1;
	if ( rand2 < 0.05 ) { rand = rand2; }
	
	// block color
	vec3 color = mix( colors[0], colors[1], pow(rand,5.0) );
	if ( rand < 0.05 ) { color=colors[2]; }

	float vignette = 1.6-length(screenPosNorm*2.0-1.0);
	vec4 finalColor = vec4(color*vignette, 1.0);
	
	//gl_FragColor = finalColor;
	gl_FragColor = mix(prevColor, finalColor, sin( sin(screenPos.x)+cos(screenPos.y) +time*10.0)*0.5+0.5);
	
	if( rand==rand2) { gl_FragColor = finalColor;}

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