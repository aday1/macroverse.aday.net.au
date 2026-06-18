/*{
    "DESCRIPTION": "wormswormsworms",
    "CREDIT": "GLSL Sandbox / various",
    "ISFVSN": "2.0",
    "CATEGORIES": [
        "misc"
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
        "misc"
    ]
}*/



#define time ((useFrameIndex ? (float(FRAMEINDEX) / fps) : TIME) * timeScale)
#define mouse vec2(mouseX, mouseY)
#define resolution RENDERSIZE
// worms worms worms

#ifdef GL_ES
precision mediump float;
#endif

float t = 1.0*time;
float pi = atan(1.0)*4.0;
float w = resolution.x;
float h = resolution.y;
float mx = mouse.x*w;
float my = h-mouse.y*h;

#define SHOW_CURVE

float ZR(mat2 z)
{
   return z[0][0];
}
float ZI(mat2 z)
{
   return z[0][1];
}
float ZP(mat2 z)
{
   float x = ZR(z);
   float y = ZI(z);
   return atan(y,x);
}
float ZL(mat2 z)
{
   float x = ZR(z);
   float y = ZI(z);
   return sqrt(x*x+y*y);
}

mat2 complex(float rz, float iz)
{
   return mat2(rz,iz,-iz,rz);
}

mat2 cdiv(mat2 z1,mat2 z2)
{
   float x1 = ZR(z2);
   float y1 = ZI(z2);
   float x2 = x1/(x1*x1+y1*y1);
   float y2 = -(y1)/(x1*x1+y1*y1); 
   mat2 invz2 = complex(x2,y2);
   return z1*invz2;
}

// stupid driver, says
// global variable initializers must be constant expressions
mat2 CE = mat2(1.0,0.0, 0.0,1.0);
mat2 CI = mat2(0.0,1.0,-1.0,0.0);

float sinh(float x)
{
   return 0.5*exp(0.0-x)-0.5*exp(x);
}
float cosh(float x)
{
   return 0.5*exp(0.0-x)+0.5*exp(x);
}

mat2 csin(mat2 z)
{
   float x = ZR(z);
   float y = ZI(z);
   return complex(sin(x)*cosh(y),-cos(x)*sinh(y));
}
mat2 ccos(mat2 z)
{
   float x = ZR(z);
   float y = ZI(z);
   return complex(cos(x)*cosh(y),sin(x)*sinh(y));
}
mat2 cexp(mat2 z)
{
   float x = ZR(z);
   float y = ZI(z);
   return exp(x)*(cos(y)*CE+sin(y)*CI);
}
mat2 csqrt(mat2 z)
{
   float x = ZR(z);
   float y = ZI(z);
   float p = y/(sqrt(2.0*(sqrt(x*x+y*y)-x)));
   float q = sqrt((sqrt(x*x+y*y)-x)/2.0);
   return complex(p,q);
}
mat2 cln(mat2 z)
{
   float x = ZR(z);
   float y = ZI(z);
   return complex(0.5*log(x*x+y*y),atan(y,x));
}
mat2 cpow(mat2 z, mat2 w)
{
   float x = ZR(z);
   float y = ZI(z);
   float a = ZR(w);
   float b = ZI(w);
   float rs = x*x+y*y;
   float phi = atan(y,x);
   float xt = a*0.5*log(rs)-b*phi;
   float yt = b*0.5*log(rs)+a*phi;
   return cexp(complex(xt,yt));
}

mat2 fun1(mat2 z)
{
   mat2 w = CE;
   for (float n=1.0;n<=19.0;n+=1.0)
   {
      w *= CE+cdiv(CE*n*n*n*n*n,cpow(z,CE*n))*cexp(CI*t);
   }
   //return w;
   //return cpow((cdiv(CE,w)),CI);
   return cpow(w,CI*cexp(CI*t/10.0));
}

mat2 t2(mat2 z)
{
   mat2 w = z;
   w = CE*0.5-cdiv(CE,w);
   w = cdiv(w,complex(4.0,0.0));
   return w;
}

mat2 t1(mat2 z)
{
   mat2 w = z;
   w = cdiv(CE,w)*100.0;
	//w=w*w;
   return w;
}

mat2 t3(mat2 z)
{
   //return csqrt(CE-z*z*z);
   //return cexp(z*z)+cexp(z*z*CI*CI);
   //return csin(cexp(csin(1.0*z))+cexp(csin(1.0*z)*CI*CI));
   //return (cln(cdiv(csin(z*4.0),CE*z-cexp(z))));
   //return cln(csin((z)));
   //return cpow(CI,csin(cdiv(CE,z)));
   //float phi = pi*0.5;
   //return cpow((z),CE*1.0*cos(t/8.0)+CI*1.0*sin(t/8.0));
   //return cpow((z*z+CE)/ZL(CE-z*z),CE*2.0*cos(phi)+CI*1.0*sin(phi));
	
   return fun1(z);
}

float curve(float x, float y)
{
   //float dx = MX;
   //float dy = MY;
   //return x*x+y*y-10.0;
   //return (pow(x,2.0)+pow(y,2.0))-16.0;
   //return (pow(x,2.0)+pow(y,2.0))-16.0+10.0*sin(4.0*x-t);
   //return (pow(x,2.0)+pow(y,2.0))-16.0+10.0*sin(8.0*x-t)+10.0*sin(8.0*y);
   //return (pow(x,2.0)+pow(y,2.0))-9.0+exp(1.0*sin(10.0*(x)-t*4.0)+1.0*sin(10.0*y));
   return sin(2.0*(y-x))-cos(2.0*(x+y));
   //return ((pow(x,2.0)+pow(y,2.0))-9.0)*((pow(x-4.0,2.0)+pow(y,2.0))-9.0);
   //return sin(1.0*y)-sin((1.0*x))+0.0;
   //return y-tan(t/10.0)*x;
}

void main(void)
{
   float x = gl_FragCoord.x;
   float y = gl_FragCoord.y;

   float X1 = -10.0;
   float X2 = 10.0;
   float Y1 = -10.0;
   float Y2 = 10.0;
 
   float X = (((X2-X1)/w)*x+X1);
   float Y = ((((Y2-Y1)/h)*y+Y1)*h/w);
   float MX = (((X2-X1)/w)*mx+X1);
   float MY = -((((Y2-Y1)/h)*my+Y1)*h/w);

   mat2 z = complex(X,Y);
   //mat2 zt = ((t1(t1(t1(t1(z))))));
   //mat2 zt = t1(t2(t2(z)));
   mat2 zt = ((t1(z)));
   float Xt = ZR(zt);
   float Yt = ZI(zt);
   float f = curve(X-MX,Y-MY);
   vec3 c = vec3(0.0,0.0,0.0);
   float ft = curve(Xt-MX,Yt-MY);
   float d = 0.1;
   if (abs(ft)<=d)
   {
      c = vec3(0.0,1.0,0.0);
   }
#ifdef SHOW_CURVE
   d = 0.1;
   if (abs(f)<=d)
   {
      c = vec3(1.0,0.0,0.0);
   }
#endif
   gl_FragColor = vec4(c,1.0);
}
