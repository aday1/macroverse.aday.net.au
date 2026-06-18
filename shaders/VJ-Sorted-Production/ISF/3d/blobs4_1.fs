/*{
    "DESCRIPTION": "blobs4",
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
        "3d"
    ]
}*/



#define time ((useFrameIndex ? (float(FRAMEINDEX) / fps) : TIME) * speed)
#define mouse vec2(mouseX, mouseY)
#define resolution (RENDERSIZE / zoom)
// Fixed shadows and ambient occlusion bugs, and sped some shit up.
// Still needs some work
// Voltage / Defame (I just fixed some bugs, someone else did they main work on this)
// rotwang: @mod* tinted shadow
// modded by @dennishjorth. A few opts, just with the blue color, metafied blobby toruses are pretty neat.
// xpansive: removed some useless code (in the marching loop!)

#ifdef GL_ES
precision highp float;
#endif

//Simple raymarching sandbox with camera

//Raymarching Distance Fields
//About http://www.iquilezles.org/www/articles/raymarchingdf/raymarchingdf.htm
//Also known as Sphere Tracing
//Original seen here: http://twitter.com/#!/paulofalcao/statuses/134807547860353024

//Scene Start

//Torus
float torus(in vec3 p2, in vec2 t, float offset, float modder){
	vec3 p = vec3(
		(sin(offset+time*modder*0.14+0.5+sin(p2.y*p2.y*0.3+p2.z*p2.z*0.3+2.0+time*modder*0.14))*0.4+1.0)*p2.x,
		(sin(offset+time*modder*0.15+1.0+sin(p2.x*p2.x*0.3+p2.z*p2.z*0.3+1.5+time*modder*0.15))*0.4+1.0)*p2.y,
		(sin(offset+time*modder*0.13+1.5+sin(p2.x*p2.x*0.3+p2.y*p2.y*0.3+0.5+time*modder*0.12))*0.4+1.0)*p2.z
		);	
	vec2 q = vec2(length(vec2(p.x,p.z))-t.x, p.y);
	return length(q) - t.y;
}

//Objects union
vec2 inObj(in vec3 p){
    const float modder = 0.1;
    float cos1x = cos(time*modder*5.0);
    float sin1x = sin(time*modder*5.0);
    float cos2x = cos(time*modder*4.0);
    float sin2x = sin(time*modder*4.0);
    float cos3x = cos(time*modder*5.5);
    float sin3x = sin(time*modder*5.5);
    float cos4x = cos(time*modder*4.5);
    float sin4x = sin(time*modder*4.5);

    vec3 p3 = vec3(p.x*cos1x+p.z*sin1x,
	    p.y,
	    p.x*sin1x-p.z*cos1x);

    vec3 p4 = vec3(p.x*cos3x+p.z*sin3x,
	    p.y,
	    p.x*sin3x-p.z*cos3x);

   vec3 p5 = vec3(p4.x,
	    p4.y*cos4x+p4.z*sin4x,
	    p4.y*sin4x-p4.z*cos4x);
	
   vec3 p6 = vec3(p3.x,
	    p3.y*cos2x+p3.z*sin2x,
	    p3.y*sin2x-p3.z*cos2x);

    float b1 = torus(p5+vec3(cos(time*modder*0.37)*3.33,sin(time*modder*0.69)*0.33,cos(time*modder*0.79)*0.33),vec2(3.0,1.0),0.5,modder);
    float b2 = torus(p3+vec3(sin(time*modder*0.57)*3.33,cos(time*modder*0.74)*0.33,cos(time*modder*0.64)*0.33),vec2(3.0,1.0),1.0,modder);
    float b6 = torus(p6+vec3(sin(time*modder*0.47)*3.33,cos(time*modder*0.94)*0.33,cos(time*modder*0.84)*0.33),vec2(3.0,1.0),1.5,modder);
    const float e = 0.1;
    const float r = 2.0;
    float b = 1.0/(b1+1.0+e)+1.0/(b2+1.0+e)+1.0/(b6+1.0+e);
    vec2 dist = vec2(1.0/b-0.7,1);
    return dist;
}

//Scene End

void _userMain(void){
  //Camera animation
  vec3 U=vec3(0,1,0);//Camera Up Vector
  vec3 viewDir=vec3(0,0,0); //Change camere view vector here
  vec3 E=vec3(-sin(time*0.2)*8.0,4,cos(time*0.2)*8.0); //Camera location; Change camera path position here
	
  //Camera setup
  vec3 C=normalize(viewDir-E);
  vec3 A=cross(C, U);
  vec3 B=cross(A, C);
  vec3 M=(E+C);
  
  vec2 vPos=2.0*gl_FragCoord.xy/resolution.xy - 1.0; // (2*Sx-1) where Sx = x in screen space (between 0 and 1)
  vec3 scrCoord=M + vPos.x*A*resolution.x/resolution.y + vPos.y*B; //normalize resolution in either x or y direction (ie resolution.x/resolution.y)
  vec3 scp=normalize(scrCoord-E);

  //Raymarching
  const vec3 e=vec3(0.001,0,0);
  const float MAX_DEPTH=20.0; //Max depth

  vec2 s=vec2(0.1,0.0);
  vec3 c,p,n;
	
  float f=1.0;
  for(int i=0;i<192;++i){
    if (abs(s.x)<.01||f>MAX_DEPTH) break;
    f+=s.x;
    p=E+scp*f;
    s=inObj(p);
  }	
  n=normalize(
      vec3(s.x-inObj(p-e.xyy).x,
           s.x-inObj(p-e.yxy).x,
           s.x-inObj(p-e.yyx).x));

  if (f<MAX_DEPTH){
    c=vec3(1.0,0.1,0.4);
	  c.x = c.x*sin(f+cos(f+time*1.2)+time*1.1)*0.4;
	  c.y = 0.1;
	  c.z += cos(f+sin(f+time*2.1)+time*3.3)*0.2;
	  c.x += 0.2;
	  c.z += 0.4;
    float b=max(dot(n,normalize(E-p)),0.1);
    gl_FragColor=vec4((b*c+pow(b,100.0))*(1.0-f*.001),1.0);//simple phong LightPosition=CameraPosition
  }
  else gl_FragColor=vec4(0,0,0.0,0); //background color
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