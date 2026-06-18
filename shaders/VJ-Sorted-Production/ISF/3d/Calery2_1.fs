/*{
    "DESCRIPTION": "Calery2",
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
#ifdef GL_ES
precision highp float;
#endif

/* AlgebraicSurface_Caley
Raymarching for algebraic surfaces with  orthographics projection
by 焦堂生    jiaotangsheng@126.com
Idea from RealSurf (http://realsurf.informatik.uni-halle.de/)
*/

//#define F1 F
//#define dF1 dF

const float R = 4.;
const float oo = 1000.;

const int IT = 10;
float alpha = 2.2+.25*sin(time);
//float alpha = 2.;

float F(vec3 v) 
{
  float x = v.x;
  float y = v.y;
  float z = v.z;
  float s = x+y+z+1.;
  return x*x+y*y+z*z+alpha*x*y*z-1.;
}

vec3 dF(vec3 v) {
  float x = v.x;
  float y = v.y;
  float z = v.z;
  float s = x+y+z+1.;
  return vec3(
	    2.*x+alpha*y*z,
	    2.*y+alpha*x*z,
	    2.*z+alpha*x*y
	  );
}

float SR(vec3 v) {
  return dot(v,v)-R*R;  
}

vec3 ray(vec2 pos, float t) {
  float th = time*.5-4.*mouse.x;
  float phi = time*.7+3.*mouse.y;
  return 
    mat3(
      vec3(cos(th),0.,sin(th)),
      vec3(0.,1.,0.),
      vec3(-sin(th),0.,cos(th))
    )*
    (
    mat3(
      vec3(1.,0,0.),
      vec3(0,cos(phi),sin(phi)),
      vec3(0,-sin(phi),cos(phi))
    )  
    *(vec3(pos,6.)+vec3(0.,0.,-1.)*t));
}

float eval(vec4 poly, float t) {
  return (((poly[3])*t+poly[2])*t+poly[1])*t+poly[0]; //horner scheme
}

vec4 d(vec4 p) {
  vec4 r = vec4(0.);
  for(int i=0; i<3; i++) {
    r[i] = p[i+1]*float(i+1);  
  }
  return r;
}

float bisect(vec4 p, float l, float u, float def) {
  if(l==u) return def;
  float lv = eval(p, l);
  float uv = eval(p, u);
  if(lv*uv>=0.) return def;
  
  float m, mv;
  for(int i=0; i<IT; i++) {
    m = (l+u)/2.;
    mv = eval(p, m);
    if(lv*mv>0.) {
      l = m;
      //lv = mv;
    } else {
      u = m;
      //bv = cv; //nobody cares
    }
  }
  return m;
}

float firstroot(vec4 poly, float l, float u) { //finds first root of poly in interval [l, u]
  vec4 p[4];//derivatives
  p[3] = poly; //deg 3
  for(int i=2; i>=1; i--) {
    p[i] = d(p[i+1]);  
  }
  vec4 roots = vec4(u); //always consider u as root
  vec4 oroots = vec4(u);
  for(int i=1; i<4; i++) { //i: degree
    roots[0] = bisect(p[i], l, oroots[0], l);
    for(int j=1; j<4; j++) { if(j<i)
      roots[j] = bisect(p[i], oroots[j-1], oroots[j],roots[j-1]);
    }
    oroots = roots;
  }
  for(int i=0; i<4; i++) {
    if(roots[i]!=l && roots[i]!=u) return roots[i];
    //if(abs(eval(poly,roots[i]))<.01) return roots[i];
  }
  return oo;
}

mat4 A = mat4( //polynomial interpolation for basepoints 0, 5, 10, 15 (better use chebyshev nodes)
vec4(  1.000000000000000, -0.366666666666667,  0.040000000000000, -0.001333333333333),
vec4( -0.000000000000000,  0.600000000000000, -0.100000000000000,  0.004000000000000),
vec4(  0.000000000000000, -0.300000000000000,  0.080000000000000, -0.004000000000000),
vec4( -0.000000000000000,  0.066666666666667, -0.020000000000000,  0.001333333333333)
);
//octave: inv(fliplr(vander([0,5,10,15])))'

void _userMain( void ) {

  vec2 pos = (gl_FragCoord.xy * 2.0 - resolution) / min(resolution.x, resolution.y)*6.;
  //pos.y *= resolution.y/resolution.x;
  
  vec4 vals;
  vec4 rvals;
  for(int i=0; i<4; i++) {
    vec3 p = ray(pos, 5.*float(i));
    vals[i] = F(p);
    rvals[i] = SR(p);
  }
  
  vec4 poly = A*vals;//interpolate
  vec4 rpoly = A*rvals; 
  
  //rpoly is quadratic
  float D = (rpoly[1]*rpoly[1])-4.*rpoly[2]*rpoly[0]; 
  
  float froot = oo;
  if(D>=0.) 
    froot = firstroot(poly, max(0.,(-rpoly[1]-sqrt(D))/(2.*rpoly[2])), max(0.,(-rpoly[1]+sqrt(D))/(2.*rpoly[2])));

  gl_FragColor = vec4(.1);
  
  if(froot != oo) {
    vec3 n = normalize(dF(ray(pos,froot)));

    vec3 l[5]; //position of light
    vec3 c[5]; //color of light
    l[0] = vec3(-1.,1.,0.);
    l[1] = vec3(0.,-1.,1.);
    l[2] = vec3(1.,0.,-1.);
    l[3] = -ray(vec2(.0,.0),-10.);
    l[4] = ray(vec2(.0,.0),-10.);
    
    c[0] = vec3(1.,.6,.3);
    c[1] = vec3(.3,1.,.6);
    c[2] = vec3(.6,.3,1.);
    c[3] = vec3(.9,.3,.0);
    c[4] = vec3(0.,.8,.8);

    gl_FragColor = vec4(0.,0.,0.,1.);
    
    for(int i=0; i<5; i++) {
      float illumination = max(0.,dot(normalize(l[i]),n));
      gl_FragColor.rgb += illumination*illumination*c[i];
    }
  } 
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