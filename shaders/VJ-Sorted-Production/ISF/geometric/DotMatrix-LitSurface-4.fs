/*{
    "DESCRIPTION": "DotMatrix-LitSurface-4",
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
        }
    ],
    "TAGS": [
        "geometric"
    ]
}*/




#define time ((useFrameIndex ? (float(FRAMEINDEX) / fps) : TIME))
#define mouse vec2(0.0)
#define resolution RENDERSIZE
#ifdef GL_ES
precision mediump float;
#endif

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
	vec2 e = vec2(0.0001, 0.0);
	return normalize(vec3( 
					  map(p + e.xyy) - map(p - e.xyy),
					  map(p + e.yxy) - map(p - e.yxy),
					  map(p + e.yyx) - map(p - e.yyx)));
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
		col = col*clamp(dot(norm,lig),0.0,1.0);
		col += vec3(0.2,0.3,0.4)*clamp(norm.y,0.0,1.0);
	}
		
	gl_FragColor = vec4(col,1.0);
}
