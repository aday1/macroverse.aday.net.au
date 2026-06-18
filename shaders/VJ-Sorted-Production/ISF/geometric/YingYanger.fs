/*{
    "DESCRIPTION": "YingYanger",
    "CREDIT": "GLSL Sandbox / various",
    "ISFVSN": "2.0",
    "CATEGORIES": [
        "geometric"
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
        "geometric"
    ]
}*/



#define time ((useFrameIndex ? (float(FRAMEINDEX) / fps) : TIME) * timeScale)
#define mouse vec2(mouseX, mouseY)
#define resolution RENDERSIZE
#ifdef GL_ES
precision mediump float;
#endif

// dashxdr 20151110

varying vec2 surfacePosition;

vec3 color = vec3(0.0);
vec3 dir, eye, lookat;
vec2 spinu, spinv;
vec3 yy_ndir, yy_udir, yy_vdir;
vec3 last_norm;
vec3 best_contact, best_norm;
float best_dist = 9999.0;
bool hit = false;

float rim = .05 * 1.0;
void fixup(float fout, vec3 outdir)
{
	if(fout<0.0)
	{
		return;
	}
	fout = min(fout, 1.0);
	float fup = sqrt(1.0 - fout*fout);
	last_norm = fup*yy_ndir + fout*normalize(outdir);
}

float fout(float r, float rmax)
{
	return (r - (rmax-rim)) / rim;
}

bool yinyang(vec3 last_contact, float h)
{
	last_norm = yy_ndir;
	float indent = rim - sqrt(max(0.0, rim*rim - h*h));
	vec3 center1 = 0.5 * yy_udir; // center1 is the hole, big end of the yin-yang
	vec3 center2 = -center1; // center2 is the spot
	float r = length(last_contact);
	if(r>1.0-indent) return false;
	float r_fout = fout(r, 1.0);
	vec3 v1 = last_contact - center1;
	float d1 = length(v1);
	float d1_fout = fout(d1, .5);
	vec3 v2 = last_contact - center2;
	float d2 = length(v2);
	float d2_fout = fout(d2, .5);
	float smallr = 0.15;
	if(d1<smallr) return false;
	if(d1<smallr + rim)
	{
		if(d1<smallr + indent) return false;
		d1_fout = fout(d1, smallr+rim);
		fixup(1.0 - d1_fout, -v1);
		return true;
	}
	if(d1<0.5-indent && dot(v1, yy_vdir)<0.0)
	{
		fixup(d1_fout, v1);
		return true;
	}
	if(d2<smallr-indent)
	{
		fixup(fout(d2, smallr), v2);
		return true;
	}
	if(d2<0.5+indent) return false;
	if(dot(last_contact, yy_vdir)>0.0)
	{
		if(r_fout >= 0.0)
			fixup(r_fout, last_contact);
		else
			fixup(2.0 - d2_fout, -v2);
		return true;
	}
	return false;
}

vec4 contact1h, contact2h;
vec3 tracecolor;
vec3 temp_ndir;
bool better;

bool checklayer(float f)
{
	vec4 contacth = mix(contact1h, contact2h, f);
	float h = contacth.w;
	vec3 plane_contact = contacth.xyz - temp_ndir*h;
	yy_ndir = (h<0.0) ? -temp_ndir : temp_ndir;
	bool inside = yinyang(plane_contact, h);
	if(inside)
	{
		float d = length(eye - contacth.xyz);
		better = d<best_dist;
		if(better)
		{
			best_dist = d;
			color = tracecolor;
			best_norm = last_norm;
			best_contact = contacth.xyz;
			hit = true;
		}
	}
	return inside;
}

void yinyangtrace(vec3 tc, bool flip)
{
	tracecolor = tc;
	float m1, m2;
	vec3 temp_udir, temp_vdir;
	if(!flip) // xy plane, red
	{
		m1 = (eye.z - rim) / -dir.z;
		m2 = (eye.z + rim) / -dir.z;
		temp_ndir = vec3(0.0, 0.0, 1.0);
		temp_udir = vec3(1.0, 0.0, 0.0);
		temp_vdir = vec3(0.0, 1.0, 0.0);
	} else // xz plane, blue
	{
		m1 = (eye.y - rim) / -dir.y;
		m2 = (eye.y + rim) / -dir.y;
		temp_ndir = vec3(0.0, 1.0, 0.0);
		temp_udir = vec3(-1.0, 0.0, 0.0);
		temp_vdir = vec3(0.0, 0.0, -1.0);
	}
	yy_udir = spinu.x * temp_udir + spinu.y * temp_vdir;
	yy_vdir = spinv.x * temp_udir + spinv.y * temp_vdir;

	contact1h = vec4(eye+m1*dir, rim);
	contact2h = vec4(eye+m2*dir, -rim);
#define STEPS1 8
	float finside = -1.0;
	float foutside = -1.0;

	for(int i=0;i<=STEPS1;++i)
	{
//if(i==STEPS1/2 && fract(time) < .5) break;
		float f = float(i)/float(STEPS1);
		bool inside = checklayer(f);
		if(inside)
		{
			if(i==0) return; // not going to get any better...
			if(better || finside<0.0)
				finside=f;
		} else
		{
			if(foutside<0.0)
				foutside=f;
		}
	}
	if(finside<0.0 || foutside<0.0) return;
#define STEPS2 8
	for(int i=0;i<STEPS2;++i)
	{
		float f = (finside + foutside) * .5;
		bool inside = checklayer(f);
		if(inside) finside = f;
		else foutside=f;
	}
}

void main()
{
	vec2 pos = surfacePosition * 2.0;

	float tt = time*sin(time);
tt *= .25;
//tt = 3.5;
//tt = 4.8;

	vec2 rot;
	float downa;

	float zoom = 2.0;

	eye = zoom*vec3(0.5, 1.0, 1.0);
	vec2 m = (mouse*2.0 - 1.0);
	m = vec2(0.1, 0.1);
	m *= 3.1415927*2.0;
	float lon = m.x;
	float lat = m.y;
	eye = vec3(sin(lon), 0.0, cos(lon));
	eye = eye*cos(lat) + sin(lat)*vec3(0.0, 1.0, 0.0);
	eye *= zoom;
	lookat = vec3(0.0);

	spinu.x = cos(tt);
	spinu.y = -sin(tt);
	spinv.x = -spinu.y;
	spinv.y = spinu.x;

	vec3 upDirection = vec3(0.0, 1.0, 0.0);
	vec3 cameraDir = normalize(lookat - eye);
	vec3 cameraRight = normalize(cross(cameraDir, upDirection));
	vec3 cameraUp = cross(cameraRight,cameraDir);
	dir = normalize(cameraRight * pos.x + cameraUp * pos.y + 1.5 * cameraDir);

	yinyangtrace(vec3(0.0, 1.0, 0.0), false); // red
	yinyangtrace(vec3(0.0, 0.0, 1.0), true); // blue

	vec3 lightpos = .2*vec3(-1.0, 1.0, 2.0);
	if(hit)
	{
		vec3 toeye = normalize(eye - best_contact);
		vec3 tolight = normalize(lightpos - best_contact);
		vec3 mid = normalize(toeye + tolight);
		float spectral = pow(max(0.0, dot(mid, best_norm)), 32.0);
		color *= min(1.0, .5 * (dot(tolight, best_norm) + 1.0) + .1);
		color += vec3(spectral);
	}

	if(true) // show light
	{
		vec3 ltoe = lightpos - eye;
		float along = dot(ltoe, dir);
		vec3 closestpoint = eye + along * dir;
		float size = .025*1.0;
		float r = length(closestpoint-lightpos);
		if(r < size)
		{
			float back = sqrt(size*size-r*r);
			closestpoint -= back*dir;
			vec3 rad = closestpoint - lightpos;
				color = vec3(dot(-dir, normalize(rad)));
		}
	}

	gl_FragColor =  vec4(color, 1.0);
}

