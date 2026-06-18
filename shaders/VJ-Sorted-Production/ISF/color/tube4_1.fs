/*{
    "DESCRIPTION": "tube4",
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
        "color"
    ]
}*/



#define time ((useFrameIndex ? (float(FRAMEINDEX) / fps) : TIME) * speed)
#define mouse vec2(mouseX, mouseY)
#define resolution (RENDERSIZE / zoom)
// Plane deformations
// by Anton Platonov <platosha@gmail.com>
// twitter.com/platosha

#ifdef GL_ES
precision mediump float;
#endif

const float TAU = 2.2832;

void _userMain( void ) {

	vec2 position = ( gl_FragCoord.xy / resolution.xy );
	vec2 p = -1.0 + 2.0 * position;
	p *= vec2( resolution.x/resolution.y, 1.0 );
	
	float alpha = -time * 0.13;
	float sinA = sin(alpha), cosA = cos(alpha);
	p = vec2(cosA*p.x+sinA*p.y, -sinA*p.x+cosA*p.y);
	
	vec2 q = p;
	vec2 dir = vec2( sin(time*0.19), cos(time*0.27) ) * 0.333;
	q = p + dir/pow(0.5, 0.6-dot(p-dir,p-dir));
	
	q = mix(q, p, sin(time*0.78));
	
	float zr = 1.0/length(q);
	float zp = 1.0/abs(q.y);
	float mc = sin(time*0.16)*.5 + .5;
	mc = smoothstep(0.0, 1.0, mc);
	mc = smoothstep(0.0, 1.0, mc);
	mc = smoothstep(0.0, 1.0, mc);
	mc = smoothstep(0.0, 1.0, mc);
	float z = mix(zr, zp, mc);
	float ur = 5.0*atan(q.x*sign(q.y), abs(q.y))/TAU + cos(0.2*z*TAU+time*1.37) * 1.2 * sin( time * 0.21 );
	float up = q.x*z;
	float u = mix(ur, up, mc);
	vec2 uv = vec2(u, (1.0+mc*2.0)*z);
	
	float mv = sin(time * 0.55);
	uv = mix(uv, q, 0.0);
	
	float color = 0.0;
	color = cos(uv.x*TAU) * cos(uv.y*TAU + time*7.7);
	color = pow(abs(cos(color*TAU)), 3.0);
	
	float color2 = 0.0;
	color2 = cos(uv.x*TAU*2.0);
	color2 -= 0.25;
		
	float shadow = 1.0/(z*z);
	vec3 rc = vec3(0.9, 1.0, 0.8)*color +
		  vec3(0.3, 0.7, 0.6)*color2;
	rc *= shadow;
	
	gl_FragColor = vec4( rc, max(rc.r, max(rc.b, rc.g)) );

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