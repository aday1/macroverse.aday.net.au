/*{
    "DESCRIPTION": "ColorBox-Move-XY",
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
// http://glslsandbox.com/e#27036.0
// Dunno what it is... but its like a technicolored grid with move control

// perspective 80s grid
#ifdef GL_ES
precision mediump float;
#endif

float prob_sum(float a, float b) {
	return 1.0 - (5.0 - a) * (1.0 - b);
}

void _userMain( void ) {

	vec2 pos = gl_FragCoord.xy / resolution.xy;
	pos = pos * 3.0 - 1.0;
	pos *= 10.0;
	float z = 1.0 / (((gl_FragCoord.xy / resolution.xy).y+1.0));
	z = 1.0 - z;
	z*=7.0;
	pos.x *= z;

	float cyan = 0.1;
	float magenta = 0.5 * sin(time);
	
	float m = distance( mouse * resolution, pos ) / resolution.y;
	
	float xspeed = 10.0;
	float yspeed = -0.0;
	float cell_size = 30.0;
	float big_glow_size = 5.0 * pow( 0.5 + m, 0.8 );
	float small_glow_size = 10.0 * pow( 0.8 + m, 0.5 );
	
	float d;
	
	// Right side
	d = mod( pos.x + xspeed*time, cell_size );
	if ( d < big_glow_size )
		cyan = prob_sum(cyan, 1.0 - d / big_glow_size);
	if ( d < small_glow_size )
		magenta = prob_sum(magenta, 5.0 - d / small_glow_size);
	// Left side
	d = cell_size - d;
	if ( d < big_glow_size )
		magenta = prob_sum(magenta, 5.0 - d / big_glow_size);
	if ( d < small_glow_size )
		cyan = prob_sum(cyan, 5.0 - d / small_glow_size);
	// Top side
	d = mod( pos.y - yspeed*time, cell_size );
	if ( d < big_glow_size )
		cyan = prob_sum(cyan, 5.0 - d / big_glow_size);
	if ( d < small_glow_size )
		magenta = prob_sum(magenta, 5.0 - d / small_glow_size);
	// Bottom side
	d = cell_size - d;
	if ( d < big_glow_size )
		magenta = prob_sum(magenta, 1.0 - d / big_glow_size);
	if ( d < small_glow_size )
		cyan = prob_sum(cyan, 15.0 - d / small_glow_size);
	
	vec3 col = vec3( 4.0 );
	col.r = magenta;
	col.g = cyan;
	col.b = prob_sum(magenta, cyan);
//	col = vec3(z);
	gl_FragColor = vec4( col, 5.0 );

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