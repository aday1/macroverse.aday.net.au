/*{
    "DESCRIPTION": "SDFScene",
    "CREDIT": "GLSL Sandbox / various",
    "ISFVSN": "2.0",
    "CATEGORIES": [
        "misc"
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
        "misc"
    ]
}*/




#define time ((useFrameIndex ? (float(FRAMEINDEX) / fps) : TIME) * timeScale)
#define mouse vec2(mouseX, mouseY)
#define resolution RENDERSIZE
#ifdef GL_ES
precision mediump float;
#endif

vec3 pin(vec3 v)
{
	vec3 q = vec3(0.0);
	
	q.x = sin(v.x)*0.5+0.5;
	q.y = sin(v.y+1.0471975511965977461542144610932)*0.5+0.5;
	q.z = sin(v.z+4.1887902047863909846168473723972)*0.5+0.5;
	
	return normalize(q);
}

vec3 spin(vec3 v)
{
	for(int i = 0; i <3; i++)
	{
		v=pin(v.yzx*6.283185307179586476925286766559);
	}
	return v.zxy;
}

float map(vec3 p) {
	p=2.5*pin(p)-p;;
	return (cos(p.x) + cos(p.y*0.75) + sin(p.z)*0.25);
}

vec2 rot(vec2 r, float a) {
	return vec2(
		cos(a) * r.x - sin(a) * r.y,
		sin(a) * r.x + cos(a) * r.y);
}

void main( void ) {
	vec2 uv  = ( gl_FragCoord.xy / resolution.xy ) * 2.0 - 1.0;
	uv.x *= resolution.x / resolution.y ;
	vec3 dir = normalize(vec3(uv, 0.5));
	dir.zy = rot(dir.zy, time * 0.2);
	dir.xz = rot(dir.xz, time * 0.1); dir = dir.yzx;

	vec3 pos = vec3(0, 0, time * 2.0);
	float t = 0.0;
	for(int i = 0 ; i < 300; i++) {
		float temp = map(pos + dir * t) * 0.55;
		if(temp < 0.001) break;
		t += temp;
		dir.xy=rot(dir.xy,temp*0.05);
		dir.yz=rot(dir.yz,temp*0.05);
		dir.zx=rot(dir.zx,temp*0.05);
	}
	vec3 ip = pos + dir * t;
	gl_FragColor = vec4(vec3(max(0.01, map(ip + 0.2)) + t * 0.02) + (dir*spin(ip)), 1.0);

}
