/*{
    "DESCRIPTION": "DotMatrix-InkWash-4",
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
        "tunnel",
        "geometric",
        "3d"
    ]
}*/




#define time ((useFrameIndex ? (float(FRAMEINDEX) / fps) : TIME) * speed)
#define mouse vec2(mouseX, mouseY)
#define resolution (RENDERSIZE / zoom)
/**
* Original code by Paulo Falcao
* adapted for beginners by Jaksa Vuckovic
* Amiga rules!
*/

#ifdef GL_ES
precision mediump float;
#endif

// sphere1
vec3 origin = vec3(10.0 * cos(time * .5), 6.*abs(cos(time*2.)), -10.0*sin(time*.5));
const float radius = 10.0;

// sphere2
vec3 origin2 = vec3(-15.0 * cos(time * .5), 18.*abs(cos(time*3.))-4.0, 15.0*sin(time*.5));
const float radius2 = 6.0;

// given a point returns the distance from the floor object
float obj_floor(in vec3 p)
{
  return p.y+10.0;
}

// give a point returns the distance from the sphere
float obj_sphere(in vec3 p) {
  return length(p - origin) - radius;
}

// give a point returns the distance from the sphere2
float obj_sphere2(in vec3 p) {
  return length(p - origin2) - radius2;
}

// given a point returns the distance to the nearest object
float distance_to_obj(in vec3 p)
{
  return min(min(obj_sphere(p), obj_floor(p)), obj_sphere2(p));
}

// procedural definition of Floor Color (checkerboard)
// given a point in the 3d space returns the texture color of the floor at that point
// this implementation ignores the y axis
vec3 floor_color(in vec3 p)
{
    if ((fract(p.z * 0.2) > 0.5) ^^ (fract(p.x * 0.2) > 0.5))
      return vec3(1,0,0);
    else
      return vec3(1,1,1);
}

void _userMain( void ) {
	vec2 position = ( gl_FragCoord.xy / resolution.xy ); // position in range (0,0)..(1,1)
	vec2 screen_pos = (2.0 * position - 1.0); // position in range (-1,-1)..(1,1)
	vec3 camera_up = vec3(0,1,0);
  	vec3 scr_world_pos=vec3(cos(mouse.x*6.0)*50.0, mouse.y * 49.0, sin(mouse.x*6.0)*50.0); // position of the center of the screen in the 3d world
	vec3 camera_pos = vec3(cos(mouse.x*6.0)*51.0, mouse.y * 50.0, sin(mouse.x*6.0)*51.0);
	
	vec3 camera_dir = normalize(scr_world_pos - camera_pos);
	vec3 u = normalize(cross(camera_up, camera_dir));
	vec3 v = cross(camera_dir,u) * (resolution.y/resolution.x);
	vec3 scr_world_coord = scr_world_pos + screen_pos.x*u + screen_pos.y*v; // the point on the screen in the 3d world
	vec3 rayDirection=normalize(scr_world_coord-camera_pos);
	
	// Raymarching.
  	const vec3 e = vec3(0.02,0,0);
	const float max_dist = 100.0; // Max depth
	float d = 0.02; // initial step
	vec3 p;
	
	float distance_from_camera = 1.0;
	for (int i=0; i<64; i++) {
	    if ((abs(d) < .001) || (distance_from_camera > max_dist)) break;
	    
	    distance_from_camera += d;
	    p = camera_pos + rayDirection*distance_from_camera;
	    d = distance_to_obj(p);
	}
	
	if (distance_from_camera < max_dist) {	        
	      	vec3 c=floor_color(p);
	    
		vec3 normal = normalize(vec3(d-distance_to_obj(p-e.xyy),
		  		        d-distance_to_obj(p-e.yxy),
				        d-distance_to_obj(p-e.yyx)));
		float lightAngle = dot(normal,normalize(camera_pos-p));
			
		//simple phong lighting, LightPosition = CameraPosition
		gl_FragColor = vec4((lightAngle*c + pow(lightAngle,16.0)) * (1.0-distance_from_camera*.01), 1.0);
		
	} else {
		// we didn't hit anything, so paint the background
		gl_FragColor = vec4( 0,0,0, 1.0 );
	}

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