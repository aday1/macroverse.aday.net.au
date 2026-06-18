/*{
    "DESCRIPTION": "DotMatrix-Emerald-FlameEffect-2",
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
            "NAME": "timeScale",
            "TYPE": "float",
            "DEFAULT": 1.0,
            "MIN": 0.1,
            "MAX": 4.0,
            "LABEL": "Time speed"
        },
        {
            "NAME": "mouseX",
            "TYPE": "float",
            "DEFAULT": 0.5,
            "MIN": 0.0,
            "MAX": 1.0,
            "LABEL": "Mouse X"
        },
        {
            "NAME": "mouseY",
            "TYPE": "float",
            "DEFAULT": 0.5,
            "MIN": 0.0,
            "MAX": 1.0,
            "LABEL": "Mouse Y"
        }
    ],
    "TAGS": [
        "geometric",
        "3d"
    ]
}*/




#define time ((useFrameIndex ? (float(FRAMEINDEX) / fps) : TIME) * timeScale)
#define mouse vec2(mouseX, mouseY)
#define resolution RENDERSIZE
#ifdef GL_ES
precision mediump float;
#endif

float rm_scene(vec3 pos)
{
	pos += sin((cross(pos, vec3(1.0, 1.0, 1.0)))*vec3(1.0, 1.0, 1.0)*0.5 + time)*0.9;//*(1.0-exp(-time/10.0));
	pos = mod(pos+2.5, 5.0)-2.5;
	float ldist = max(abs(pos.x), max(abs(pos.y), abs(pos.z)));
	float sdist = min(abs(pos.x), min(abs(pos.y), abs(pos.z)));
	float mdist = dot(abs(pos), vec3(1.0)) - ldist - sdist;

	return -(2.0 - mdist);
}

void main()
{
	//vec2 in_vertex = ( gl_FragCoord.xy / resolution.xy ) + mouse / 4.0;
	vec2 in_vertex = ( (gl_FragCoord.xy - resolution.xy/2.0) / min(resolution.x, resolution.y) );
	
	vec3 vert_ray_step = normalize(vec3(in_vertex.x*1280.0/720.0, in_vertex.y, 1.0));
	vec3 vert_cam_pos = vec3(0.0, 0.0, -0.0);

	// ROTATE
	float rotx = -0.5;
	vert_cam_pos.yz = vert_cam_pos.yz*cos(rotx) + vec2(vert_cam_pos.z, -vert_cam_pos.y)*sin(rotx);
	vert_ray_step.yz = vert_ray_step.yz*cos(rotx) + vec2(vert_ray_step.z, -vert_ray_step.y)*sin(rotx);

	// ROTATE AGAIN
	float rtime = time * 0.35;
	vert_cam_pos.xz = vert_cam_pos.xz*cos(rtime) + vec2(vert_cam_pos.z, -vert_cam_pos.x)*sin(rtime);
	vert_ray_step.xz = vert_ray_step.xz*cos(rtime) + vec2(vert_ray_step.z, -vert_ray_step.x)*sin(rtime);

	vec3 ray_pos = vert_cam_pos;
	const float OFFS = 0.01;
	const float OFFS2 = 0.99;
	const float OFFS3 = 0.05;
	const int STEPS = 100;
	const int SUBDIVS = 2;
	vec3 ray_step = normalize(vert_ray_step);
	const vec3 OFFS_X = vec3(1.0, 0.0, 0.0)*OFFS;
	const vec3 OFFS_Y = vec3(0.0, 1.0, 0.0)*OFFS;
	const vec3 OFFS_Z = vec3(0.0, 0.0, 1.0)*OFFS;

	// Rotate camera

	const vec4 BG = vec4(0.6, 0.8, 1.0, 1.0);
	vec4 out_color = BG;
	float out_acc = 1.0;

	float lstep = 1.0;
	float atime = 0.0;

	for(int i = 0; i < STEPS; i++)
	{
		float res = rm_scene(ray_pos);

		if(res > 0.0)
		{
			// binomial binary bintastic thing i can't remember the name of
			float sub = lstep*0.5;
			float subsub = sub;
			for(int j = 0; j < SUBDIVS; j++)
			{
				subsub *= 0.5;

				if(rm_scene(ray_pos-ray_step*sub) <= 0.0)
					sub -= subsub;
				else
					sub += subsub;
			}
			ray_pos -= ray_step*sub;
			atime -= sub;

			vec3 norm = normalize(vec3(
				rm_scene(ray_pos - OFFS_X) - rm_scene(ray_pos + OFFS_X),
				rm_scene(ray_pos - OFFS_Y) - rm_scene(ray_pos + OFFS_Y),
				rm_scene(ray_pos - OFFS_Z) - rm_scene(ray_pos + OFFS_Z)
			));
			float diff = max(0.0, -dot(norm, ray_step));
			//vec3 refl = normalize(ray_step - 2.0*dot(norm, ray_step)*norm);
			vec3 refl = ray_step - 2.0*dot(norm, ray_step)*norm;
			float spec = max(0.0, -dot(refl, normalize(ray_pos - vert_cam_pos)));
			spec = pow(spec, 128.0);
			out_color = out_color*(1.0-out_acc)
				+ mix(BG,
				(diff*0.9 + 0.1)*vec4(1.0, 0.2, 0.0, 1.0)
				+ spec*vec4(1.0, 1.0, 1.0, 0.0),
				pow(2.0, -0.2*atime))*out_acc;
			ray_step = refl;
			ray_pos += ray_step*0.02;
			atime += 0.02;
			out_acc *= 0.1;
			//if(out_acc < 0.01)
				break;
		}

		lstep = max(OFFS3, -res*OFFS2);
		atime += lstep;
		ray_pos += ray_step*lstep;
	}
	
	gl_FragColor = out_color;
}
