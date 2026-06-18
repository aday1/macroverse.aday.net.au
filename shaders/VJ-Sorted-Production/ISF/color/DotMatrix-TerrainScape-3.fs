/*{
    "DESCRIPTION": "DotMatrix-TerrainScape-3",
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
#ifdef GL_ES
precision mediump float;
#endif

// amiga ball...

float pi = atan(1.)*4.;

vec3 sphereTex(vec2 p,vec3 n,float radius)
{
	float t = time;
	vec2 sc = vec2(sin(t), cos(t));
	mat3 rotate_x = mat3(  1.0,  0.0,  0.0,
			       0.0, sc.y,-sc.x,
			       0.0, sc.x, sc.y);
	t = time * 1.1;
	sc = vec2(sin(t), cos(t));
	mat3 rotate_y = mat3( sc.y,  0.0, sc.x,
			       0.0,  1.0,  0.0,
			     -sc.x,  0.0, sc.y);
	t = time * 0.9;
	sc = vec2(sin(t), cos(t));
	mat3 rotate_z = mat3( sc.y,-sc.x,  0.0,
			      sc.x, sc.y,  0.0,
			       0.0,  0.0,  1.0);
	n *= rotate_x * rotate_y * rotate_z;
	
	vec2 uv = vec2(atan(n.z,n.x), atan(n.y,length(n.xz)));
		
	float checker = sin(uv.x*6.0)*sin(uv.y*6.0);
	checker = max(checker,0.);
	checker = pow(checker, 0.001);
	checker *= smoothstep(radius,radius-0.01,length(p));
	checker = max(checker,0.3);
		
	return checker > 0.3 ? vec3(1.0) : checker * vec3(2.0, 0.2, 0.2);
}

void _userMain( void ) {

	vec2 res = vec2(resolution.x/resolution.y,1.0);
	vec2 cen = res / 2.0;
	vec2 p = ( gl_FragCoord.xy / resolution.y ) - cen;
	vec2 m = mouse*res-cen;
	
	vec3 col;
	
	float radius = 0.4;
	float height = sqrt(radius*radius -(p.x*p.x) - (p.y*p.y));
	vec3 normal = normalize(vec3(p.x,p.y,height));
	
	vec3 lightDir = normalize(vec3(m,1.));
	
	float shad = dot(normal,vec3(mouse*res-cen,1.));
	float spec = dot(lightDir,normalize(reflect(-lightDir,normal)));
	spec = max(0.,spec);
	spec = pow(spec,10.)*0.7;	
	
	col = sphereTex(p,normal,radius)*shad+spec;				
	
	gl_FragColor = vec4( vec3(col), 1.0 );
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