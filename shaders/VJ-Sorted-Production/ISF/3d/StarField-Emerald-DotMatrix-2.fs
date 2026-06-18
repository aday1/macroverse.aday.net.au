/*{
    "DESCRIPTION": "StarField-Emerald-DotMatrix-2",
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
            "NAME": "speed",
            "TYPE": "float",
            "DEFAULT": 1.0,
            "MIN": 0.0,
            "MAX": 5.0,
            "LABEL": "Speed"
        },
        {
            "NAME": "mouseX",
            "TYPE": "float",
            "DEFAULT": 0.0,
            "MIN": -1.0,
            "MAX": 1.0,
            "LABEL": "Mouse X"
        },
        {
            "NAME": "mouseY",
            "TYPE": "float",
            "DEFAULT": 0.0,
            "MIN": -1.0,
            "MAX": 1.0,
            "LABEL": "Mouse Y"
        },
        {
            "NAME": "zoom",
            "TYPE": "float",
            "DEFAULT": 1.0,
            "MIN": 0.1,
            "MAX": 4.0,
            "LABEL": "Zoom"
        },
        {
            "NAME": "colorR",
            "TYPE": "float",
            "DEFAULT": 1.0,
            "MIN": 0.0,
            "MAX": 2.0,
            "LABEL": "Color Red"
        },
        {
            "NAME": "colorG",
            "TYPE": "float",
            "DEFAULT": 1.0,
            "MIN": 0.0,
            "MAX": 2.0,
            "LABEL": "Color Green"
        },
        {
            "NAME": "colorB",
            "TYPE": "float",
            "DEFAULT": 1.0,
            "MIN": 0.0,
            "MAX": 2.0,
            "LABEL": "Color Blue"
        },
        {
            "NAME": "brightness",
            "TYPE": "float",
            "DEFAULT": 0.0,
            "MIN": -1.0,
            "MAX": 1.0,
            "LABEL": "Brightness"
        },
        {
            "NAME": "saturation",
            "TYPE": "float",
            "DEFAULT": 1.0,
            "MIN": 0.0,
            "MAX": 3.0,
            "LABEL": "Saturation"
        },
        {
            "NAME": "contrast",
            "TYPE": "float",
            "DEFAULT": 1.0,
            "MIN": 0.0,
            "MAX": 3.0,
            "LABEL": "Contrast"
        },
        {
            "NAME": "hueShift",
            "TYPE": "float",
            "DEFAULT": 0.0,
            "MIN": 0.0,
            "MAX": 1.0,
            "LABEL": "Hue Shift"
        },
        {
            "NAME": "invert",
            "TYPE": "bool",
            "DEFAULT": 0,
            "LABEL": "Invert Colors"
        }
    ],
    "TAGS": [
        "tunnel",
        "geometric",
        "3d"
    ]
}*/




#define time ((useFrameIndex ? (float(FRAMEINDEX) / fps) : TIME) * speed)
#define mouse vec2(mouseX, mouseY)
#define resolution (RENDERSIZE / zoom)
// Torus - TorusJourney (modified)
//
// original by @paulofalcao
// @rotwang: @mod* background replaced, less light for distant rings, torus shape

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
vec3 sim(vec3 p,float s){
   vec3 ret=p;
   ret=p+s/2.0;
   ret=fract(ret/s)*s-s/2.0;
   return ret;
}

vec2 rot(vec2 p,float r){
   vec2 ret;
   ret.x=p.x*cos(r)-p.y*sin(r);
   ret.y=p.x*sin(r)+p.y*cos(r);
   return ret;
}

vec2 rotsim(vec2 p,float s){
   vec2 ret=p;
   ret=rot(p,-PI/(s*2.0));
   ret=rot(p,floor(atan(ret.x,ret.y)/PI*s)*(PI/s));
   return ret;
}

//Util End

//Scene Start
vec2 obj0(in vec3 p){
  p.y=p.y-0.5;
  p.xz=rotsim(p.xz,200.0);
  p.z=p.z-32.0;
  p.yz=rotsim(p.yz,2.0); 
  p.z=p.z-1.0;
  p.xy=rotsim(p.xy,1.0);
  float c1=length(max(abs(p)-vec3(0.2,0.2,0.2),0.0));
  float c3=length(max(abs(p)-vec3(0.15,3.5,0.02),0.0));
  return vec2(min(c1,c3),0);
}
//Floor Color (checkerboard)
vec3 obj0_c(in vec3 p){
  return vec3(1.0,0.5,0.2);
}

vec2 obj1(vec3 p){
  p.y=p.y-0.5;
  p.xz=rotsim(p.xz,6.0); // Number of rings
  p.z=p.z-16.0;
  p.yz=rotsim(p.yz,48.0); // Resolution of rings
  p.z=p.z-6.0;
  p.xy=rotsim(p.xy,2.0);
  float c3=length(max(abs(p)-vec3(0.1,2.0,0.1),0.0));
  return vec2(c3,1);
}

//RoundBox with simple solid color
vec3 obj1_c(in vec3 p){
  return vec3(0.2,0.5,1.0);
}

//Objects union
vec2 inObj(in vec3 p){
  return ObjUnion(obj0(p),obj1(p));
  //return obj1(p);
}

vec2 hitTest(in vec3 p){
  return obj0(p);

}

//Scene End

void _userMain(void){
  vec2 vPos=-1.0+2.0*gl_FragCoord.xy/resolution.xy;
 
  //Camera animation
  vec3 vuv=vec3(0,1,0);//Change camere up vector here
  vec3 vrp=vec3(sin(time*0.1)*32.0,0.0,cos(time*0.1)*32.0);
  vec3 prp=vec3(sin(time*0.1-0.1)*32.0,0.0,cos(time*0.1-0.1)*32.0);
  float vpd=3.5;  
 
  //Camera setup
  vec3 vpn=normalize(vrp-prp);
  vec3 u=normalize(cross(vuv,vpn));
  vec3 v=cross(vpn,u);
  vec3 scrCoord=prp+vpn*vpd+vPos.x*u*resolution.x/resolution.y+vPos.y*v;
  vec3 scp=normalize(scrCoord-prp);

  //Raymarching
  const vec3 e=vec3(0.1,0,0);
  const float maxd=60.0; //Max depth

  vec2 s=vec2(0.1,0.0);
  vec3 c,p;
vec3 n = vec3(0.0,1.0,0.0);	
  float f=1.0;
	
  for(int i=0;i<256;i++){
    if (abs(s.x)<.001||f>maxd)
	    break;
    f+=s.x;
    p=prp+scp*f;
    s=hitTest(p);
  }
 
vec3 rgb = vec3(0.0, 0.05, 0.1);
	
  if (f<maxd){
  
     c=obj1_c(p);
 
    //tetrahedron normal
    const float n_er=0.01;
    float v1=inObj(vec3(p.x+n_er,p.y-n_er,p.z-n_er)).x;
    float v2=inObj(vec3(p.x-n_er,p.y-n_er,p.z+n_er)).x;
    float v3=inObj(vec3(p.x-n_er,p.y+n_er,p.z-n_er)).x;
    float v4=inObj(vec3(p.x+n_er,p.y+n_er,p.z+n_er)).x;
    n=normalize(vec3(v4+v1-v3-v2,v3+v4-v1-v2,v2+v4-v3-v1));
    
    float b=dot(n,normalize(prp-p));
    rgb=vec3((b*c+pow(b,4.0))*(1.0-f*.095));//simple phong LightPosition=CameraPosition
  }

	gl_FragColor = vec4(rgb,1.0);
}

void main() {
    _userMain();
    vec3 c = gl_FragColor.rgb;
    float a = gl_FragColor.a;
    float luma = dot(c, vec3(0.299, 0.587, 0.114));
    c = mix(vec3(luma), c, saturation);
    c = (c - 0.5) * contrast + 0.5;
    c *= vec3(colorR, colorG, colorB);
    c += brightness;
    if (hueShift > 0.001) {
        float cosH = cos(hueShift * 6.28318);
        float sinH = sin(hueShift * 6.28318);
        c = vec3(
            c.r * (0.299 + 0.701*cosH + 0.168*sinH) + c.g * (0.587 - 0.587*cosH + 0.330*sinH) + c.b * (0.114 - 0.114*cosH - 0.497*sinH),
            c.r * (0.299 - 0.299*cosH - 0.328*sinH) + c.g * (0.587 + 0.413*cosH + 0.035*sinH) + c.b * (0.114 - 0.114*cosH + 0.292*sinH),
            c.r * (0.299 - 0.300*cosH + 1.250*sinH) + c.g * (0.587 - 0.588*cosH - 1.050*sinH) + c.b * (0.114 + 0.886*cosH - 0.203*sinH)
        );
    }
    if (invert) c = 1.0 - c;
    gl_FragColor = vec4(clamp(c, 0.0, 1.0), a);
}