/*{
    "DESCRIPTION": "3dCockTorus-01",
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
        "3d"
    ]
}*/



#define time ((useFrameIndex ? (float(FRAMEINDEX) / fps) : TIME) * timeScale)
#define mouse vec2(mouseX, mouseY)
#define resolution RENDERSIZE
//Simple raymarching sandbox with camera (v1.1)
//
//by @paulofalcao
//
//Raymarching Distance Fields
//About http://www.iquilezles.org/www/articles/raymarchingdf/raymarchingdf.htm
//Also known as Sphere Tracing

#ifdef GL_ES
precision highp float;
#endif

//Util Start
float PI=3.14159265;

vec2 ObjUnion(in vec2 obj0,in vec2 obj1){
  if (obj0.x<obj1.x)
  	return obj0;
  else
  	return obj1;
}

//Util End

//Scene Start

//Floor
vec2 obj0(in vec3 p){
  return vec2(p.y+3.0,0);
}
//Floor Color (checkerboard)
vec3 obj0_c(in vec3 p){
 if (fract(p.x*.5)>.5)
   if (fract(p.z*.5)>.5)
     return vec3(0,0,0);
   else
     return vec3(1,1,1);
 else
   if (fract(p.z*.5)>.5)
     return vec3(1,1,1);
   else
     	return vec3(0,0,0);
}

//IQs RoundBox (try other objects http://www.iquilezles.org/www/articles/distfunctions/distfunctions.htm)
vec2 obj1(in vec3 p){
  return vec2(length(max(abs(p)-vec3(1.0,1.0,1.0),0.0))-0.25,1);
}

//RoundBox with simple solid color
vec3 obj1_c(in vec3 p){
	return vec3(1.0,0.5,0.2);
}

vec2 torus(vec3 p,float rT,float rR) {
	return vec2(length(vec2(length(p.xz) - rT,p.y)) - rR, 1.);
}

vec2 capsule(vec3 p, float r, float c) {
	return vec2(mix(length(p.xz)-r, length(vec3(p.x,abs(p.y)-c,p.z))-r, step(c,abs(p.y))), 1.);
}

//Objects union
vec2 inObj(in vec3 p){
	//vec2 r = ObjUnion(, obj1(p));
	
	vec2 r = ObjUnion(obj0(p), torus(p+vec3(0., -0.5, 0.), mix(1.1,.5,(pow(sin(time*3.),4.)+1.)/2.), 0.4));
	vec3 q = p+vec3(0., -.25-3.*(sin(time*3.)+1.)/2., 0.); // INPUT COLOR FOR TUNABLE COCK
	
	r = ObjUnion(r, capsule(q, 0.7, 1.5));
	r = ObjUnion(r, capsule(q+vec3(0.,2.,0.), 0.8, .1));
	r = ObjUnion(r, vec2(length(q+vec3(-.5, -1.5, 0.)) - 0.9, 1.));
	r = ObjUnion(r, vec2(length(q+vec3(.5, -1.5, 0.)) - 0.9, 1.));

  return r;
}

//Scene End

void main(void){
  vec2 vPos=-1.0+2.0*gl_FragCoord.xy/resolution.xy;

  //Camera animation
  vec3 vuv=vec3(0,1,0);//Change camere up vector here
  vec3 vrp=vec3(0,0,0); //Change camere view here
  float mx=mouse.x*PI*2.0;
  float my=mouse.y*PI/2.01;  
  vec3 prp=vec3(cos(my)*cos(mx),sin(my),cos(my)*sin(mx))*6.0; //Trackball style camera pos - Change camera path position here

  //Camera setup
  vec3 vpn=normalize(vrp-prp);
  vec3 u=normalize(cross(vuv,vpn));
  vec3 v=cross(vpn,u);
  vec3 vcv=(prp+vpn);
  vec3 scrCoord=vcv+vPos.x*u*resolution.x/resolution.y+vPos.y*v;
  vec3 scp=normalize(scrCoord-prp);

  //Raymarching
  const vec3 e=vec3(0.1,0,0);
  const float maxd=60.0; //Max depth

  vec2 s=vec2(0.1,0.0);
  vec3 c,p,n;

  float f=1.0;
  for(int i=0;i<64;i++){
    if (abs(s.x)<.01||f>maxd) break;
    f+=s.x;
    p=prp+scp*f;
    s=inObj(p);
  }
  
  if (f<maxd){
    if (s.y==0.0)
      c=obj0_c(p);
    else
      c=obj1_c(p);
 
    //tetrahedron normal
    const float n_er=0.01;
    float v1=inObj(vec3(p.x+n_er,p.y-n_er,p.z-n_er)).x;
    float v2=inObj(vec3(p.x-n_er,p.y-n_er,p.z+n_er)).x;
    float v3=inObj(vec3(p.x-n_er,p.y+n_er,p.z-n_er)).x;
    float v4=inObj(vec3(p.x+n_er,p.y+n_er,p.z+n_er)).x;
    n=normalize(vec3(v4+v1-v3-v2,v3+v4-v1-v2,v2+v4-v3-v1));
    
    float b=dot(n,normalize(prp-p));
    gl_FragColor=vec4((b*c+pow(b,8.0))*(1.0-f*.01),1.0);//simple phong LightPosition=CameraPosition
  }
  else gl_FragColor=vec4(0,0,0,1); //background color
}

