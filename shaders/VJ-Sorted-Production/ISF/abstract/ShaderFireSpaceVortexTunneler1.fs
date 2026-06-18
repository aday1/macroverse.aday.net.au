/*{
    "DESCRIPTION": "ShaderFireSpaceVortexTunneler1",
    "CREDIT": "GLSL Sandbox / various",
    "ISFVSN": "2.0",
    "CATEGORIES": [
        "abstract"
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
        "abstract"
    ]
}*/



#define time ((useFrameIndex ? (float(FRAMEINDEX) / fps) : TIME))
#define mouse vec2(0.0)
#define resolution RENDERSIZE
#ifdef GL_ES
precision mediump float;
#endif

// Edited by: ABraker
// Buckle up, becuase this will be a LONG journey through hell!

const int MAX_ITER = 30; // Try 30 for extra flames

vec2 rotate(in vec2 v, in float a) {
	return vec2(cos(a)*v.x + sin(a)*v.y, -sin(a)*v.x + cos(a)*v.y);
}

float torus(in vec3 p, in vec2 t)
{
	vec2 q = abs(vec2(max(abs(p.x), abs(p.z))-t.x, p.y));
	return max(q.x, q.y)-t.y;
}

// These are all equally interesting, but I could only pick one :(
float trap(in vec3 p)
{
	float x = abs(max(abs(p.z)-0.1, abs(p.x)-0.1))-0.01;
	float y = length(max(abs(p.xy) - 0.05, 0.0));
	float z = length(p)-0.5;
	float w = length(max(abs(p) - 0.35, 0.0));
	float t = abs(length(p.xz)-0.2)-0.01;
	#define var1 abs(max(abs(p.z)-0.1, abs(p.x)-0.1))-0.01;
	#define var2 length(max(abs(p.xy) - 0.05, 0.0));
	#define var3 length(p)-0.5;
	#define var4 length(max(abs(p) - 0.35, 0.0));
	#define var5 abs(length(p.xz)-0.2)-0.01;
	#define var6 abs(min(torus(vec3(p.x, mod(p.y,0.4)-0.2, p.z), vec2(0.1, 0.05)), max(abs(p.z)-0.05, abs(p.x)-0.05)))-0.005;
	#define var7 abs(min(torus(p, vec2(0.3, 0.05)), max(abs(p.z)-0.05, abs(p.x)-0.05)))-0.005;
	#define var8 min(length(p.xz), min(length(p.yz), length(p.xy))) - 0.05;
	#define var9 (sqrt(atan((length(p)-0.5))) )/20.0;
	#define varA abs((tan((abs(min(tan(torus(vec3(p.x, mod(p.y,0.4)+0.2, p.z), vec2(0.1, 0.05))), max(sin(abs(p.x)+0.05), atan(abs(p.z)-0.5))))-0.05)))/1.0);
	#define varB  (t+w-z)*(y); w+z;
	
	return var5+varA;
}

float map(in vec3 p)
{
	float cutout = dot(abs(p.yz),vec2(0.5))-0.035;
	float road = max(abs(p.y-0.0426), abs(p.y)-0.035);
	
	vec3 z = abs(1.+mod(p,-2.0));
	z.yz = rotate(z.yz, time*0.05);

	float d = 999.0;
	float s = 1.0;
	for (float i = 0.0; i < 3.0; i++) {
		z.xz = rotate(z.xz, radians(i*20.0+time));
		z.zy = rotate(z.yz, radians((i+1.5)*40.0+time*1.1234));
		z = abs(1.0-mod(z+i/3.0,2.0));
		
		z = z*2. - 0.423641753621;
		s *= 0.27345;
		d = min(d, (1.2-2.*sin(03.2868457*time+2.4)+1.)*trap(z) * s);
	}
	return min(max(d, -cutout), .98975*road);
}

vec3 hsv(in float h, in float s, in float v) {
	return mix(vec3(1.0), clamp((abs(fract(h + vec3(3, 2, 1) / 3.0) * 6.0 - 3.0) - 1.0), 0.0 , 1.0), s) * v;
}

vec3 intersect(in vec3 rayOrigin, in vec3 rayDir)
{
	float total_dist = 0.0;
	vec3 p = rayOrigin;
	float d = 1.0;
	float iter = 0.0;
	float mind = 3.14159+sin(time*0.51)*0.52; // Move road from side to side slowly
	
	for (int i = 0; i < MAX_ITER; i++)
	{		
		if (d < 0.001) continue;
		//p.z+=sin(time)/500.0;
		d = map(p);
		// This rotation causes the occasional distortion - like you would see from heat waves
		p += d*vec3(rayDir.x, rotate(rayDir.yz+(sin(time))/10.0, sin(mind)+(cos(time))/10.0));
		mind = min(mind, d);
		total_dist += d;
		iter++;
	}

	vec3 color = vec3(0.0);
	if (d < 0.001) {
		float x = (iter/float(MAX_ITER));
		float y = (d-0.01)/0.01/(float(MAX_ITER));
		float z = (0.01-d)/0.01/float(MAX_ITER);
		if (max(abs(p.y-0.025), abs(p.z)-0.035)<0.002) { // Road
			float w = smoothstep(mod(p.x*50.0, 4.0), 2.0, 2.01);
			w = fract(w+0.0001);
			float a = fract(smoothstep(abs(p.z), 0.0025, 0.0026));
			color = vec3((0.4-x-y)*mix(vec3(0.8, 0.8, 0), vec3(0.1), 1.0-(1.0-w)*(1.0-a)));
		} else {
			float q = 1.0-x-y*2.+z;
			color = hsv(sin(time*0.43)*q*0.72+0.835, cos(-time*0.623)*1.0-q*0.522, q+0.1)/5.1;
		}
	} else
		color = sin(sin(time)*hsv(sin(time-d)*d, 1.0, 1.0)*mind)*85.0; // Background
	return color;
}

void main()
{
	vec3 upDirection = vec3(0, -1, 0);
	vec3 cameraDir = vec3(1,0,0);
	vec3 cameraOrigin = vec3(time*0.81, 0, 0);
	
	vec3 u = normalize(cross(upDirection, cameraOrigin));
	vec3 v = normalize(cross(cameraDir, u));
	vec2 screenPos = sin(time)*(-3.0 + 6.0 * gl_FragCoord.xy / resolution.xy);
	screenPos.x *= resolution.x / resolution.y;
	vec3 rayDir = normalize(u * screenPos.x + v * screenPos.y + cameraDir*(1.0-length(screenPos)*0.5));
	
	gl_FragColor = vec4(intersect(cameraOrigin, rayDir), 1.0);
} 

