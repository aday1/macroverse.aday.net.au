/*{
    "DESCRIPTION": "grid4",
    "CREDIT": "GLSL Sandbox / various",
    "ISFVSN": "2.0",
    "CATEGORIES": [
        "grid"
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
        "grid"
    ]
}*/



#define time ((useFrameIndex ? (float(FRAMEINDEX) / fps) : TIME) * speed)
#define mouse vec2(mouseX, mouseY)
#define resolution (RENDERSIZE / zoom)
#ifdef GL_ES
precision mediump float;
#endif

// Visualization of the voronoi field in 3D. red = center of cell, blue = edge between cells. Loosely based on the work of others (hit "parent" to see more). - Kabuto

vec3 random3f( vec3 seed ) {
	vec3 r = fract(seed*mat3(0.6299523843917996,0.18076110584661365,0.7744768380653113,0.07479058369062841,0.6860866188071668,0.7200816590338945,0.38891187869012356,0.9161028724629432,0.5484214460011572));
	return fract(r+r.yzx*r.zxy*4190.2654);
}

vec3 voronoi( in vec3 x ){
    vec3 p = floor( x );
    vec3 f = fract( x );

    float res = 8.0;
    float res2 = 8.0;
    float res3 = 8.0;
	
    for( int k=-1; k<=1; k++ )
    for( int j=-1; j<=1; j++ )
    for( int i=-1; i<=1; i++ )
    {
        vec3 b = vec3( i, j, k );
        vec3 r = b - f + random3f( p + b );
        float d = dot( r, r );

    if (d < res) {res3 = res2; res2 = res; res = d; } else if (d < res2) {res3 = res2; res2 = d;} else if (d < res3) res3 = d;
     }
	return vec3(sqrt(res),sqrt(res2),sqrt(res3));
}

void _userMain( void ) {

	vec3 dir = normalize(vec3((gl_FragCoord.xy-resolution*.5)/resolution.x,1.));
	vec3 pos = vec3(mouse-.5,time);
	
	vec3 color = vec3(0.);
	for (int i = 0; i < 30; i++) {
		vec3 d3= voronoi(pos);
		float d = min(d3.x,(d3.z-d3.x)*.5);
		pos += dir*d;
		color += 1./(vec3(d3.x,90/*d3.y-d3.x+.05*/,d3.z-d3.x)+.0001);
	}
	
	color = 1.-1./(1.+color*vec3(.02,.006, .01));
	color = (color-.2)*1.25;
	color *= color;
	float alpha = max(color.r, max(color.g, color.b));
	
	//color *= 20.;

	gl_FragColor = vec4( color, alpha );

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