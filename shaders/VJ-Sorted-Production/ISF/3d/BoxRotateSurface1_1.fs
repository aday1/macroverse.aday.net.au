/*{
    "DESCRIPTION": "BoxRotateSurface1",
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
#ifdef GL_ES
precision mediump float;
#endif

#extension GL_OES_standard_derivatives : enable

const vec3 cube_size = vec3 (1.0, 1.0, 1.0);

vec3 cube_normal(vec3 point) {
	float x = step(max(abs(point.y), abs(point.z)), point.x) - step(max (abs(point.y), abs(point.z)), -point.x);
	float y = step(max(abs(point.z), abs(point.x)), point.y) - step(max (abs(point.z), abs(point.x)), -point.y);
	float z = step(max(abs(point.x), abs(point.y)), point.z) - step(max (abs(point.x), abs(point.y)), -point.z);
	return normalize(vec3(x,y,z));
}

float cube_isect(vec3 ray_orig, vec3 ray_dir) {
	vec3 orig = sign(ray_dir) * ray_orig;
	vec3 dir = sign(ray_dir) * ray_dir;
	
	vec3 min_dists = (-0.5 * cube_size - orig) / dir;
	vec3 max_dists = (0.5 * cube_size - orig) / dir;
	
	float min_dist = max (min_dists.x, max (min_dists.y, min_dists.z));
	float max_dist = min (max_dists.x, min (max_dists.y, max_dists.z));
	
	if (min_dist > max_dist) {
		return -1.0;
	} 
	return min_dist;
}

void _userMain( void ) {

	mat3 twist = mat3(0.6, 0.48, 0.64,
			  -0.8, 0.36, 0.48,
			  0.0, -0.8, 0.6);
	float ct = cos(time);
	float st = sin(time);
	twist = mat3(ct, st, 0.0, -st, ct, 0.0, 0.0, 0.0, 1.0) * twist;
	vec2 position = ( gl_FragCoord.xy / resolution.xy ) - vec2 (0.5, 0.5);
	vec3 dir = twist * normalize(vec3(position, -1.0));
	vec3 orig = twist * vec3(0.0, 0.0, 5.0);
	vec3 light_dir = twist * normalize(vec3 (mouse - 0.5, 1.0));
	
	vec3 color = vec3 (0.0, 0.0, 0.0);
	
	float along_ray = cube_isect(orig, dir);
	
	if (along_ray >= 0.0) {
		vec3 hit_point = orig + dir * along_ray;
		vec3 surface_normal = cube_normal(hit_point);
		vec3 reflection = reflect(dir, surface_normal);
		float dotness = dot(reflection, light_dir);
		color = smoothstep(-0.8, 1.0, dotness) * vec3 (1.0, 0.0, 0.0);
		//color = vec3(mouse.x - 0.0, 0.0, mouse.y - 0.0);
		// color = surface_normal;
		// color = vec3 (0.5 + light_dir.x, 1.0, 0.5 + light_dir.y); // light_dir; // 5.0 * reflection;
	}

	gl_FragColor = vec4(color, 1.0 );

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