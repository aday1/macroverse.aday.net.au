/*{
    "DESCRIPTION": "GridMatrix1",
    "CREDIT": "GLSL Sandbox / various",
    "ISFVSN": "2.0",
    "CATEGORIES": [
        "grid"
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
        "grid"
    ]
}*/



#define time ((useFrameIndex ? (float(FRAMEINDEX) / fps) : TIME) * timeScale)
#define mouse vec2(mouseX, mouseY)
#define resolution RENDERSIZE
#ifdef GL_ES
precision mediump float;
#endif

#extension GL_OES_standard_derivatives : enable

const int ite_max = 100;
const float dist_coeff = 1.0;
const float dist_min = 0.01;
const float dist_max = 1000.0;
const float inf = 999999999.0;

float sphere(vec3 p) {
	return length(p) - 0.5;
}

float box(vec3 p, vec3 b) {
	vec3 d = abs(p) - b;
	return min(max(d.x, max(d.y, d.z)), 0.0) + length(max(d, 0.0));
}

float box2(vec2 p, vec2 b) {
	vec2 d = abs(p) - b;
	return min(max(d.x, d.y),.0) + length(max(d, 0.0));
}

float crossline(vec3 p, float s) {
  float da = box2(p.xy,vec2(s));
  float db = box2(p.yz,vec2(s));
  float dc = box2(p.zx,vec2(s));
  return min(da,min(db,dc));
}

float cbox(vec3 p){
	float b = box(p, vec3(1.0));
	float c = crossline(p, .15);
	
   float s = 1.;
   for( int m=0; m<3; m++ )
   {
      vec3 a = mod( p*s, 2.0 )-1.0;
      s *= 3.0;
      vec3 r = 1.0 - 3.0*abs(a);

      float c = crossline(r,1.0)/s;
	   b = max(b,-c);
   }

	float m = max(-c,b);
	
	return m;
}

float map(vec3 p) {
	float t = dist_max;
	float w = 0.0;
	
	//w = box(p, vec3(0.2));
	//w = crossline(p);
	w = cbox(p);
	t = min(t,w);
	return t;
}

vec3 intersect(vec3 pos, vec3 dir){
	float t = 0.0;
	
	for(int i=0; i < ite_max; i++) {
		float ttemp = map(t * dir + pos);
		if(ttemp < dist_min) break;
		
		t += ttemp * dist_coeff;
	}
	
	return vec3(t*.2,.2,.3);
}

void main( void ) {
	vec2 uv = ( gl_FragCoord.xy / resolution.xy ) * 2.0 - 1.0;
	
	float aspect = resolution.x / resolution.y;

	float theta = mouse.x*6.38;
	float phi = mouse.y*6.28;
	vec3 pos = vec3(sin(theta)*cos(phi), sin(phi)*sin(theta), cos(theta)+sin(time)*3.0);
	vec3 dir = normalize(-pos+vec3( uv * vec2(aspect, 1.0), 1.0));
		
	vec3 color = intersect(pos, dir);
	
	gl_FragColor = vec4(color, 1.0);

}
