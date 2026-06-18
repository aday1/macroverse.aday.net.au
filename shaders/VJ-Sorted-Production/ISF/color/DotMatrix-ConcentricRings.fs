/*{
    "DESCRIPTION": "DotMatrix-ConcentricRings",
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
        "geometric"
    ]
}*/




#define time ((useFrameIndex ? (float(FRAMEINDEX) / fps) : TIME) * speed)
#define mouse vec2(mouseX, mouseY)
#define resolution (RENDERSIZE / zoom)
#ifdef GL_ES
precision mediump float;
#endif

#define PI 3.14159265358979323846264

//mattdesl - devmatt.wordpress.com - textured sphere by Matt DesLauriers

void _userMain( void ) {	
	vec2 position = ( gl_FragCoord.xy / resolution.xy ) - 0.5;
	position.x *= resolution.x / resolution.y;
	
	vec2 norm = 2.0 * gl_FragCoord.xy / resolution.xy - 1.0;
	norm.x *= resolution.x / resolution.y;
	
	float r = length(norm);
	float phi = atan(norm.y, norm.x);
	
	//spherize
	r = 2.0 * asin(r) / PI;
	
	//bulge a bit
	//r = pow(r, 1.5);
	
	//zoom in a bit
	//r /= 1.25;
	
	vec2 coord = vec2(r * cos(phi), r * sin(phi));
	coord = coord/2.0 + 0.5;

	//color += sin(coord.y) + cos(coord.y * 30.0);
	//color *= cos(coord.y*80.0);

	//color -= sin(coord.x*sin(coord.y*5.0)*9.0)*20.0;

//	color.rgb += 0.25*sin(coord.y*sin(coord.x*0.0)*9.0)*2.5;
//	color.rgb += 0.45*sin(coord.x*30.0*sin(coord.y*1.0));
	
	float rings = 1.0;
	
	coord.x += time*0.05;
	
	float block = 100.0;
	rings += sin(coord.y*block)*10.0;
	rings *= sin(coord.x*block)*0.25;
	rings *= sin(coord.x*block);
	rings *= sin(coord.y*block);
	
	//assymetry
	//rings -= sin(coord.y*block)*.5;
	//rings += sin(position.x*0.025)*100.0;
	
	rings += 0.05*sin(coord.x*25.0)*25.0 * sin(time*1.5);
	rings += 0.05*sin(coord.y*25.0)*25.0;
	
	//* sin(time*1.0)*2.0-1.;
	
	rings = clamp(rings, 0., 1.0);
	
	float len = length(position);
	
	vec3 color = mix(vec3(0.2,0.1,0.4), vec3(0.4,0.6,0.7), rings);

	color *= smoothstep(0.5, 0.495, len);

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