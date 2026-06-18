/*{
    "DESCRIPTION": "InwardZooms",
    "CREDIT": "GLSL Sandbox / various",
    "ISFVSN": "2.0",
    "CATEGORIES": [
        "tunnel"
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
        "tunnel"
    ]
}*/



#define time ((useFrameIndex ? (float(FRAMEINDEX) / fps) : TIME) * timeScale)
#define mouse vec2(mouseX, mouseY)
#define resolution RENDERSIZE
// 

#ifdef GL_ES
precision mediump float;
#endif

varying vec2 surfacePosition;

float t = 1.0*time;
float pi = atan(1.0)*4.0;
float w = resolution.x;
float h = resolution.y;
float mx = mouse.x*w;
float my = h-mouse.y*h;

//#define SHOW_CURVE

#define MAX_ITER 1

mat2 complex(float zr, float zi)
{
   return mat2(zr,-zi,zi,zr);
}

mat2 complexp(float zl, float zp)
{
   return complex(zl*cos(zp),zl*sin(zp));
}

float RZ(mat2 z)
{
   return z[0][0];
}

float IZ(mat2 z)
{
   return z[0][1];
}

float LZ(mat2 z)
{
   float x = RZ(z);
   float y = IZ(z);
   return sqrt(x*x-y*y);
}

float PZ(mat2 z)
{
   float x = RZ(z);
   float y = IZ(z);
   return atan(y,x);
}

mat2 cdiv(mat2 z1, mat2 z2)
{
   float x2 = RZ(z2);
   float y2 = IZ(z2);
   float l2sq = x2*x2+y2*y2;
   mat2 inv_z2 = complex(x2/(l2sq),-y2/(l2sq));
   return z1*inv_z2;
}

mat2 CZ(mat2 z)
{
   return complex(RZ(z),-IZ(z));
}

// small compile error fix
mat2 CE = mat2(1.0,-0.0,0.0,1.0); //complex(1.0,0.0);
mat2 CI = mat2(0.0,-1.0,1.0,0.0); //complex(0.0,1.0);

float sinh(float x)
{
   return 0.5*exp(-x)-0.5*exp(x);
}
float cosh(float x)
{
   return 0.5*exp(-x)+0.5*exp(x);
}
mat2 ccos(mat2 z)
{
   float x = RZ(z);
   float y = IZ(z);
   return cos(x)*cosh(y)*CE+sin(x)*sinh(y)*CI;
}
mat2 csin(mat2 z)
{
   float x = RZ(z);
   float y = IZ(z);
   return sin(x)*cosh(y)*CE-cos(x)*sinh(y)*CI;
}
mat2 ctan(mat2 z)
{
	return cdiv(csin(z),ccos(z));
}
mat2 cexp(mat2 z)
{
   float x = RZ(z);
   float y = IZ(z);
   return exp(x)*(cos(y)*CE+sin(y)*CI);
}
mat2 cln(mat2 z)
{
   float x = RZ(z);
   float y = IZ(z);
   return complex(0.5*log(x*x+y*y),atan(y,x));
}
mat2 cpow(mat2 z, mat2 w)
{
   return cexp(w*cln(z));
}
mat2 csqrt(mat2 z)
{
   return cpow(z,CE*0.5);
}
mat2 csinh(mat2 z)
{
   return cexp(-z)*0.5-cexp(z)*0.5;
}
mat2 ccosh(mat2 z)
{
   return cexp(-z)*0.5+cexp(z)*0.5;
}
mat2 ctanh(mat2 z)
{
	return cdiv(csinh(z),ccosh(z));
}
mat2 catan(mat2 z)
{
   return (cln(CE-z*CI)-cln(CE+z*CI))*CI*0.5;
}
mat2 cacos(mat2 z)
{
	return cln(z+cpow(z*z-CE,CE*0.5))*(-CI);
}
mat2 cabs(mat2 z)
{
   return complex(abs(RZ(z)),abs(IZ(z)));
}

mat2 pheta(mat2 z)
{
   mat2 w = CI;
   for (float n=1.0;n<=19.0;n+=1.0)
   {
	   w *= CE-cpow(z,CE*n);
	   //w *= CE-cpow((z),CE*(n))+z*(z-CE);
   }
	//return w;
	//return cdiv(CE,w);
	//return cln(cpow(ccos(cln((w))-CE*(t)),CE*10.0));
	return (cpow(((cln(w))),CE*2.0));
}
mat2 zeda(mat2 z)
{
   mat2 w = CE;
   for (float n=1.0;n<=19.0;n+=1.0)
   {
	   w+=cpow(CE/n,(z));
   }
	return (w);
}
mat2 ghamma(mat2 z)
{
   mat2 w = CE;
   for (float n=1.0;n<=19.0;n+=1.0)
   {
	   w*=(CE+z/n)*cexp(-z/n);
   }
	w*=z*cexp(z*0.5772157);
	return cdiv(CE,w);
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
   mat2 w = z*1.0;
	//w=cdiv(w,cpow(CE-w*(w),CE*0.5));
	
	float x = RZ(z);
   	float y = IZ(z);
	w=complex(x/sqrt(100.0-x*x-y*y),y/sqrt(100.0-x*x-y*y));
   return w;
}

mat2 t3(mat2 z)
{
   //return (z);
	//return cdiv(CE,(z*z+CE));
	return 10.0*cln((z)-CE*0.5)-10.0*cln((z)+CE*0.5);
}

float curve(float x, float y)
{
   //float dx = MX;
   //float dy = MY;
   //return (pow(x,2.0)+pow(y,2.0))-10.0;
   //return (pow(x,2.0)+pow(y,2.0))-16.0;
   //return (pow(x,2.0)+pow(y,2.0))-16.0+10.0*sin(4.0*x-t);
   //return (pow(x,2.0)+pow(y,2.0))-16.0+10.0*sin(8.0*x-t)+10.0*sin(8.0*y);
   //return (pow(x,2.0)+pow(y,2.0))-9.0+exp(1.0*sin(10.0*(x)-t*4.0)+1.0*sin(10.0*y));
   return cos(2.0*(y-x)+t)-cos(2.0*(x+y)-t);
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
 
   //float X = (((X2-X1)/w)*x+X1);
   //float Y = ((((Y2-Y1)/h)*y+Y1)*h/w);
   //float MX = (((X2-X1)/w)*mx+X1);
   //float MY = -((((Y2-Y1)/h)*my+Y1)*h/w);
	vec2 sp = surfacePosition * 10.0;
	float X=sp.x;
	float Y=sp.y;
   mat2 z = complex(X,Y);
   //mat2 zt = ((t1(t1(t1(t1(z))))));
   //mat2 zt = t1(t2(t2(z)));
   mat2 zt = (t3(t1(z)));
   float Xt = RZ(zt);
   float Yt = IZ(zt);
   float f = curve(X,Y);
   vec3 c = vec3(0.0,0.0,0.0);
   float ft = curve(Xt,Yt);
   float d = 0.15;
   if (abs(ft)<=d)
   {
      c = vec3(0.0,1.0,0.0);
   }
#ifdef SHOW_CURVE
   d = 0.03;
   if (abs(f)<=d)
   {
      c = vec3(1.0,0.0,0.0);
   }
#endif
   gl_FragColor = vec4(c,1.0);
}
