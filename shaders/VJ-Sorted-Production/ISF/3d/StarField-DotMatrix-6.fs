/*{
    "DESCRIPTION": "StarField-DotMatrix-6",
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
        "geometric",
        "3d"
    ]
}*/




#define time ((useFrameIndex ? (float(FRAMEINDEX) / fps) : TIME) * speed)
#define mouse vec2(mouseX, mouseY)
#define resolution (RENDERSIZE / zoom)
// Testing the spikeball in paulo falcao's framework!
// las/mercury

// Shitty soccer ball mod, anyone care to fix the colors?

#ifdef GL_ES
precision highp float;
#endif

//Simple raymarching sandbox with camera

//Raymarching Distance Fields
//About http://www.iquilezles.org/www/articles/raymarchingdf/raymarchingdf.htm
//Also known as Sphere Tracing
//Original seen here: http://twitter.com/#!/paulofalcao/statuses/134807547860353024

//Util Start
vec2 ObjUnion(in vec2 obj_floor,in vec2 obj_roundBox){
  if (obj_floor.x<obj_roundBox.x)
  	return obj_floor;
  else
  	return obj_roundBox;
}
//Util End

//Sphere
float sphere(in vec3 p, float radius){
	float length = sqrt(p.x*p.x + p.y*p.y + p.z*p.z);
	return length-radius;
}

//Floor
vec2 obj_floor(in vec3 p){
  return vec2(p.y+3.0,0);
}

vec2 fblob(vec3 p) {
	// This is what I more or less recommend to use (TOP SECRET CODE FROM MERCURY - USE WITH CAUTION)
	float l = length(p);
	p = abs(normalize(p));
	p = mix(mix(p.zxy, p.yzx, step(p.z, p.y)), p, step(p.y, p.x) * step(p.z, p.x));
	float b = max(
		max(dot(p,vec3(.577)),
			dot(p.xz,vec2(.526,.851))), // <--- seems to be necessary (try specular reflections without it)
		max(dot(p.xz,vec2(.934,.357)),
			dot(p.xy,vec2(.851,.526))));
	// Three lines full of magic to play with:
	b = acos(b-0.01) / (3.1415*.9);
	b = smoothstep(.7, 0.0, b);
	return vec2(l - 2.0 - b * b * .8, 1);
	// las/mercury - END OF TRANSMISSION - <3
}

//Floor Color (checkerboard)
vec3 obj_floor_c(in vec3 p){
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
vec2 obj_roundBox(in vec3 p){
  return vec2(length(max(abs(p)-vec3(1,1,1),0.0))-0.25,1);
}

vec2 obj_sphere(in vec3 p){
  return vec2(length(p)-2.0);
}

//RoundBox with simple solid color
vec3 obj_roundBox_c(in vec3 p){
	p = abs(normalize(p));
	p = mix(mix(p.zxy, p.yzx, step(p.z, p.y)), p, step(p.y, p.x) * step(p.z, p.x));
	float b1 = dot(p,vec3(.577));
	float b2 = dot(p.xz,vec2(.526,.851));
	float b3 = dot(p.xz,vec2(.934,.357));
	float b4 = dot(p.xy,vec2(.851,.526));

	bool b12 = (b1 < b2);
	bool b34 = (b4 < b3);
	bool b14 = max(b1, b2) < max(b3, b4);
	if (b34 && b14) {
		return vec3(1,0,0);
	}
	else if (!(b12 || b14)) {
		return vec3(0,1,0);
	}
	else {
		return vec3(0,0,1);
	}
	//return vec3((b1 < b2) ? 1.0 : 0.0, (b4 < b3) ? 1.0 : 0.0, (max(b1, b2) < max(b3, b4)) ? 1.0 : 0.0);
}
//Objects union
vec2 inObj(in vec3 p){
  return ObjUnion(obj_floor(p),fblob(p));
}

//Scene End

void _userMain(void){
  //Camera animation
  vec3 U=vec3(0,1,0);//Camera Up Vector
  vec3 viewDir=vec3(0,0,0); //Change camere view vector here
  float spin = time * 0.1 + mouse.x * 8.0;
  vec3 E=vec3(-sin(spin)*4.0, 8.0 * mouse.y, cos(spin)*4.0); //Camera location; Change camera path position here
	
  //Camera setup
  vec3 C=normalize(viewDir-E);
  vec3 A=cross(C, U);
  vec3 B=cross(A, C);
  vec3 M=(E+C);
  
  vec2 vPos=2.0*gl_FragCoord.xy/resolution.xy - 1.0; // (2*Sx-1) where Sx = x in screen space (between 0 and 1)
  vec3 scrCoord=M + vPos.x*A*resolution.x/resolution.y + vPos.y*B; //normalize resolution in either x or y direction (ie resolution.x/resolution.y)
  vec3 scp=normalize(scrCoord-E);

  //Raymarching
  const vec3 e=vec3(0.1,0,0);
  const float MAX_DEPTH=60.0; //Max depth

  vec2 s=vec2(0.5,0.0);
  vec3 c,p,n;

  float f=1.0;
  for(int i=0;i<256;i++){
    if (abs(s.x)<.01||f>MAX_DEPTH) break;
    f+=s.x;
    p=E+scp*f;
    s=inObj(p);
  }
  
  if (f<MAX_DEPTH){
    if (s.y==0.0)
      c=obj_floor_c(p);
    else
      c=obj_roundBox_c(p);
    n=normalize(
      vec3(s.x-inObj(p-e.xyy).x,
           s.x-inObj(p-e.yxy).x,
           s.x-inObj(p-e.yyx).x));
    float b=dot(n,normalize(E-p));
    gl_FragColor=vec4((b*c+pow(b,8.0))*(1.0-f*.01),1.0);//simple phong LightPosition=CameraPosition
  }
  else gl_FragColor=vec4(0,0,0,1); //background color
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