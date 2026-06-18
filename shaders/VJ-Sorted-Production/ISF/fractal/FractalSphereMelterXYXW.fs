/*{
    "DESCRIPTION": "FractalSphereMelterXYXW",
    "CREDIT": "GLSL Sandbox / various",
    "ISFVSN": "2.0",
    "CATEGORIES": [
        "fractal"
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
        "fractal"
    ]
}*/
uniform float amplitude;
#define E 2.71828182846
uniform float timeScale;




#define time ((useFrameIndex ? (float(FRAMEINDEX) / fps) : TIME) * timeScale)
#define mouse vec2(mouseX, mouseY)
#define resolution RENDERSIZE
#ifdef GL_ES
precision mediump float;
#endif

uniform vec4 inputColour;

#define OCTAVES mouse.x*5
#define STRETCH 100.0
#define SQUISH 5.0

float map(in vec3 p){
	vec3 q = mod(p + 2.0, 4.0) - 2.0;
	float d1 = length(q) - 1.0;
	d1 += 0.1*sin(10.0*p.x)*sin(10.0*p.y+1.0*time*mouse.x)*sin(10.0*p.z); 
	float d2 = p.y + 1.0;
	float k = 1.0*mouse.y;
	float h = clamp(0.5 + 0.5*(d1-d2)/k,0.0,1.0);
	return mix(d1,d2,h)-k*h*(1.0-h);
}
vec3 calcNormal(in vec3 p){
	vec2 e = vec2(0.0001, inputColour.x*5);
	return normalize(vec3( 
					  map(p + e.xyy) - map(p - e.xyy),
					  map(p + e.yxy) - map(p - e.yxy),
					  map(p + e.yyx) - map(p - e.yyx)));
}
float rand(vec2 n) { 
	return fract(sin(dot(n, vec2(13, 5))) * 43758.5453);
}

float noise(vec2 n) {
	const vec2 d = vec2(0.0, 1.0);
	vec2 b = floor(n), f = smoothstep(vec2(0.0), vec2(1.0), fract(n));
	return mix(mix(rand(b), rand(b + d.yx), f.x), mix(rand(b + d.xy), rand(b + d.yy), f.x), f.y);
}

float fbm(vec2 n) {
	float total = inputColour.y, amplitude = 1.0;
	for (int i = 0; i < OCTAVES; i++) {
		total += noise(n) * amplitude;
		n += n;
		amplitude *= inputColour.y*2;
	}
	return total;
}

vec3 tex(vec2 pos) {
	const vec3 c1 = vec3(.1,0,0);
	const vec3 c2 = vec3(.7,0,0);
	const vec3 c3 = vec3(.2,0,0);
	const vec3 c4 = vec3(1,.9,0);
	const vec3 c5 = vec3(.1);
	const vec3 c6 = vec3(.9);
	vec2 p = pos;
	float q = fbm(p - time * -0.1);
	vec2 r = vec2(fbm(p + q + time - p.x - p.y), fbm(p + q + time));
	vec3 c = mix(c1, c2, fbm(p + r)) + mix(c3, c4, r.x) - mix(c5, c6, r.y);
	return c;
}

void main( void ) {

	vec2 uv = gl_FragCoord.xy / resolution.xy;
	vec2 p = -1.0 + 2.0*uv;
	p.x *= resolution.x / resolution.y;
	vec3 r0 = vec3(0.0,0.0,2.0);
	vec3 rd = normalize(vec3(p,-1.0));
	vec3 col = vec3(0.0);
	float tmax = 20.0;
	float h = 1.0;
	float t = 0.0;
	for (int i=0; i<50; i++){
		if (h < 0.0001 || t > tmax){ 
			break;
		}
		h = map(r0 + t*rd);
		t += h;
	}
	
	vec3 lig = vec3(0.5773);
	
	if (t < tmax){
		vec3 pos = r0 + t*rd;
		vec3 norm = calcNormal(pos);
		col = vec3(uv,0.5+0.5*sin(time));
		//col = col*clamp(dot(norm,lig),0.0,1.0);
		col *= clamp(tex(2.*norm.xy),0.0,1.0);
		col += vec3(0.2,0.3,0.4)*clamp(norm.y,0.0,1.0);
	}
		
	gl_FragColor = vec4(col,1.0);
}
