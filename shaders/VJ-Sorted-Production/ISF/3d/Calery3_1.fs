/*{
    "DESCRIPTION": "Calery3",
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
        }
    ],
    "TAGS": [
        "3d"
    ]
}*/



#define time ((useFrameIndex ? (float(FRAMEINDEX) / fps) : TIME))
#define mouse vec2(0.0)
#define resolution RENDERSIZE
#ifdef GL_ES
precision highp float;
#endif

/* AlgebraicSurfaces   modified by I.G.P.  modified by 焦堂生
Ray marching for algebraic surface 
with orthographics  projection
Idea from RealSurf (http://realsurf.informatik.uni-halle.de/)
*/

const vec4 backColor = vec4(0.14, 0.14, 0.1, 1.0);
 
// select current function here:
//#define F F1
//#define F F2
//#define F F3
//#define F F4
//#define F F5
 
#define F F6
//#define F F6
 
#define R 8.
#define oo 1000.
#define N 5
#define IT 15
 
float alpha = 2.3+.35*sin(time);
 
struct listN{
	float[N] a;
};
 
// surface	
float F1(vec3 v) 
{
  float x = v.x;
  float y = v.y;
  float z = v.z;
  return  x*x*x*x +y*y*y*y +z*z*z*z
	  -8.*x*y*z -1.;	
}
 
// Cayley Cubic surface
float F2(vec3 v) 
{
  float x = v.x;
  float y = v.y;
  float z = v.z;
  return x*x +y*y +z*z +x*y*z-4.;
}	
 
float F3(vec3 v) 
{
  float x = v.x;
  float y = v.y;
  float z = v.z;
  float s = x+y+z+1.;
  return 4.*(x*x*x+y*y*y+z*z*z+1.)-alpha*s*s*s;
}
 
// surfer.Helix
// https://imaginary.org/gallery/herwig-hauser-classic
float F4(vec3 v) 
{
  float x2 = v.x*v.x;
  float y2 = v.y*v.y;
  float z2 = v.z*v.z;
  return 6.*x2 -2.*x2*x2 -y2*z2;
}
 
// surfer.Zeck
// https://imaginary.org/gallery/herwig-hauser-classic
float F5(vec3 v) 
{
  float x2 = v.x*v.x;
  float y2 = v.y*v.y;
  float z2 = 0.2*v.z*v.z;
  return x2 +y2 -z2*v.z*(3.-v.z);
}
//don't work,too.
//this  finding-foot  method  works  to  Quartic Implicit Algebraic Surfaces
// surfer.Distel
// https://imaginary.org/gallery/herwig-hauser-classic
float F6(vec3 v) 
{
  float x2 = v.x*v.x;
  float y2 = v.y*v.y;
  float z2 = v.z*v.z;
  return x2 +y2 +z2 +1500. -(x2+y2) *(x2+z2) *(y2+z2) -1.0;
}
 
// don't work!!!
// Wolf Barths Sextik Surface (1994) with 65 double points
// http://www.holtzbrinck.de/artikel/951590// Bath's Sextic
// https://imaginary.org/program/formula-morph
// http://mathworld.wolfram.com/BarthSextic.html
float F7(vec3 v) 
{
  float f = 3.236068;   // 1+sqrt(5);
  float g = 1.618034;   // golden ratio = (1+sqrt(5))/2
  float k = g*g;        // k = g^2
  float x2 = v.x * v.x;
  float y2 = v.y * v.y;
  float z2 = v.z * v.z;
  float s = x2 +y2 +z2 -1.;	
  return 4.0 *(k*x2-y2) *(k*y2-z2) *(k*z2-x2) -(1.0+f) *s *s;
}
 
//------------------------------------------
vec3 dF(vec3 v) 
{
  float x = v.x;
  float y = v.y;
  float z = v.z;
  float diff =0.0001;
  float dfx = (F(vec3(x+diff,y,z))-F(vec3(x-diff,y,z)))/(2.*diff);
  float dfy = (F(vec3(x,y+diff,z))-F(vec3(x,y-diff,z)))/(2.*diff);	
  float dfz = (F(vec3(x,y,z+diff))-F(vec3(x,y,z-diff)))/(2.*diff);	
  return vec3(dfx,dfy,dfz ); 
}
//------------------------------------------
 
float SR(vec3 v) {
  return dot(v,v)-R*R;  
}
 
vec3 ray(vec2 pos, float t) {
  float th = time*.2-4.*mouse.x;
  float phi = time*.1321+3.*mouse.y;
	
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
    *(vec3(pos,10.)+vec3(0.,0.,-0.5)*t));
}

