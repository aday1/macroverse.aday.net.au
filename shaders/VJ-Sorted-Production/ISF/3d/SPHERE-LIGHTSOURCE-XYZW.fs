/*{
    "DESCRIPTION": "SPHERE-LIGHTSOURCE-XYZW",
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
        },
        {
            "NAME": "inputColour",
            "TYPE": "vec4",
            "LABEL": "Input Colour"
        }
    ],
    "TAGS": [
        "3d",
        "texture-input"
    ]
}*/
#define E 2.71828182846

uniform vec4 color;
uniform float timeScale;




#define time ((useFrameIndex ? (float(FRAMEINDEX) / fps) : TIME) * timeScale)
#define mouse vec2(mouseX, mouseY)
#define resolution RENDERSIZE
// 

#ifdef GL_ES
precision mediump float;
#endif

uniform sampler2D backBuffer; 
uniform vec4 inputColour;

vec3 lightpos = vec3(0.0,0.7,0); 
float light_brightness = mouse.y; 

vec2 sph(in vec3 p, vec3 pos, float r, float o)
{
	p -= pos; 
	p.x +=mouse.x; 
	p.x = mod(p.x+0.5, 1.0) - 0.5; 
	return vec2(length(p) - r, o); 
}
vec2 rbox(in vec3 p, vec3 pos, vec3 size, float o)
{
	return vec2(length(max(abs(p-pos)-size,0.0)) - 0.0, o); 
}
vec2 pln(in vec3 p, float o)
{
	vec3 n = normalize(vec3(0,1,0)); 
	return vec2(dot(p, n) - dot(vec3(-inputColour.x), n), o);
}

vec2 min2(in vec2 o1, in vec2 o2)
{
	if (o1.x < o2.x)
		return o1; 
	else 
		return o2; 
}
vec2 scene(in vec3 p)
{
	vec2 d = vec2(inputColour.y, 0); 
	d = min2(d, pln(p, 1.0)); 
	d = min2(d, rbox(p, vec3(0,0.8,0), vec3(1.0,0.001,inputColour.w), 2.0)); 
	d = min2(d, sph(p, vec3(0.0,-0.0,0), 0.5, 3.0)); 
	return d; 
}
vec3 get_normal(in vec3 p)
{
	vec3 eps = vec3(0.0001, 0, 0);
	float nx = scene(p+eps.xyy).x - scene(p-eps.xyy).x; 
	float ny = scene(p+eps.yxy).x - scene(p-eps.yxy).x; 
	float nz = scene(p+eps.yyx).x - scene(p-eps.yyx).x; 
	return normalize(vec3(nx,ny,nz));
}
vec3 get_material(out vec3 matr, out vec3 amb, out vec3 refl_color, in vec3 p, float o)
{
	if (o > inputColour.z && o < 1.5) { // SWITCH FOR A SICCCCCCK COLOR SHIFT!
		matr = vec3(1,1,1)*0.5; 
		amb = vec3(0,0,0); 
		refl_color =vec3(0,0,0); 
	}
	else if (o > 1.5 && o < 2.5) {
		matr = vec3(1,1,1); 
		amb = vec3(1,1,1)*clamp(1.0-length(p.xz*vec2(0.5, 1.0)), 0.0, 1.0)*light_brightness; 
		refl_color =vec3(0,0,0); 
	}
	else if (o > 2.5 && o < 3.5) {
		matr = vec3(1.0,1.0,1.0)*1.0; 
		amb = vec3(1,1,1)*0.0; 
		refl_color = vec3(1,1,1); 
	}
	return vec3(0,0,0); 
}

float ao(vec3 p, vec3 n, float d) 
{
	float o,s=sign(d);
	o = s*0.5+0.5; 
	for (float i = 0.0; i < 10.0; i+=1.0) {
		o-=(i*d-scene(p+n*i*d*s).x)/exp2(i);
	}
	return o;
}
float softshadow(in vec3 ro, in vec3 rd)
{
	vec3 pos = ro; 
	vec2 d; 
	float t = 0.0;  
	for (int i = 0; i < 8; i++) {
		d = scene(pos); 
		pos += rd*d.x; 
		t += d.x; 		
	}
	return 0.2+0.8*clamp(t, 0.0, 1.0); 
}

vec3 rm2(in vec3 ro, in vec3 rd) 
{
	vec3 color = vec3(0); 
	vec3 pos = ro; 
	vec2 d; 
	float t; 
	for (int i = 0; i < 32; i++) {
		d = scene(pos); 
		pos += rd*d.x;
		t += d.x; 
		if ( t < 10.0 && abs(d.x) < 0.001 ) {
			vec3 lightpos = vec3(0,0.7,0); 
			vec3 n = get_normal(pos);
			vec3 l = normalize(lightpos - pos); 
			float diff = clamp(dot(n,l), 0.0, 1.0); 
			float falloff = 1.0 / clamp(dot(lightpos - pos, lightpos-pos), 1.0, 100.0);
			float shade = softshadow(pos+0.01*n, l); 
			vec3 matr;
			vec3 amb; 
			vec3 refl_color; 
			get_material(matr, amb, refl_color, pos, d.y); 
			color += amb;
			color += shade*diff*light_brightness*matr*smoothstep(0.0, 0.5, falloff); 	
		}
	}
	color /= 32.0; 
	return color; 
}

void main( void ) {

	vec2 p = 2.0 * ( gl_FragCoord.xy / resolution.xy ) - 1.0;
	p.x *= resolution.x/resolution.y; 
	
	vec3 color = vec3(0); 
	
	vec3 ro = vec3(0,0.0,2.0); 
	vec3 rd = normalize(vec3(p.x,p.y,-1.0)); 
	
	vec3 pos = ro; 
	vec2 d; 
	float t; 
	for (int i = 0; i < 64; i++) {
		d = scene(pos); 
		pos += rd*d.x;
		t += d.x; 
	}
	if (t < 100.0 && abs(d.x) < 0.01 ) {
		vec3 n = get_normal(pos);
		vec3 l = normalize(lightpos - pos); 
		vec3 r = reflect(rd, n); 
		float diff = clamp(dot(n,l), 0.0, 1.0); 
		float diff2 = 0.2*clamp(dot(n,normalize(vec3(1,1,1))), 0.0, 1.0); 
		float diff3 = 0.1*clamp(dot(n,normalize(vec3(-1,-1,-1))), 0.0, 1.0); 
		float falloff = 1.0 / clamp(dot(lightpos - pos, lightpos-pos), 1.0, 100.0);
		float fres = clamp(1.0-dot(-rd,n), 0.0, 1.0);
		float shade = softshadow(pos+0.01*n, l); 
		float amb_o = ao(pos+0.001*n, 0.1*n,0.5); 
		vec3 refl = rm2(pos + n*0.001, r)*fres; 
		vec3 matr = vec3(0);
		vec3 amb = vec3(0);
		vec3 refl_color = vec3(0);
		get_material(matr, amb, refl_color, pos, d.y); 
		color = amb+amb_o;
		color += shade*light_brightness*(diff+diff2+diff3)*matr*smoothstep(0.0, 0.5, falloff); 
		color += refl*refl_color; 
	
		vec3 bb_col = texture2D(backBuffer, gl_FragCoord.xy/resolution.xy).xyz; 
		color = color*2.0 - bb_col*0.5; 
	}
	
	gl_FragColor = vec4(color, 1.0); 
}


