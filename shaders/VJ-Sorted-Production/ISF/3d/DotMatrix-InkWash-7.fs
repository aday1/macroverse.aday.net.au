/*{
    "DESCRIPTION": "DotMatrix-InkWash-7",
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
        }
    ],
    "TAGS": [
        "geometric",
        "3d"
    ]
}*/




#define time ((useFrameIndex ? (float(FRAMEINDEX) / fps) : TIME))
#define mouse vec2(0.0)
#define resolution RENDERSIZE
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

void main( void ) {
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
