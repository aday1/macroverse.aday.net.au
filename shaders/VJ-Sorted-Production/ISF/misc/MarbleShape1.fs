/*{
    "DESCRIPTION": "MarbleShape1",
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
// 

#ifdef GL_ES
precision mediump float;
#endif

float t = 1.0*time;
float pi = atan(1.0)*4.0;

#define COMPLEX
//#define EDGE

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

mat2 CE = complex(1.0,0.0);
mat2 CI = complex(0.0,1.0);

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
	return w;
	//return cdiv(CE,w);
	//return cln(cpow(ccos(cln((w))-CE*(t)),CE*10.0));
	//return (cpow(((cln(w))),CE*2.0));
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

mat2 t3(mat2 z)
{
	mat2 w = z;
   	return w;
}
mat2 t2(mat2 z)
{
   	mat2 w = z;
  	return w;
}
mat2 t1(mat2 z)
{
   	mat2 w = z;
	//w=cln(w*w)-CE*1.0*sin(t);
	//w=((csinh(cabs(cln(cdiv(CE,w*w)))+CI*(t)/10.0)));
	//w=cln(cpow(w*w,CI));
	//w=csin(cdiv(CE,w)-CE*t/10.0);
	//w=csin(w-CE*t/10.0);
	w=pheta(w);
   	return w;
}

float fun2d(float x, float y)
{
	//return (x*y)/(1.0*sqrt(x*x+y*y)-t);
	return (y+1.0)*sin(1.0*x);
	//return (cos(10.1*(x+y))+cos(10.1*(y-x)));
	//float r=sqrt(x*x+y*y);
	//float p=atan(y,x);
	//return sin(40.0*r)-sin(30.0*p)+y;
}
float fun3d(float x, float y,float z)
{
	float fr=10.1;
	return (cos(fr*x)+cos(fr*y)+cos(fr*z))/3.0;
}

float curve(float x, float y)
{
	float r=1.0;
	float q=x*x+y*y;
	if (q>4.0*r*r) return 0.0;
	float a=atan(y,x); float b=asin(1.0-0.5*(q)/(r*r));
	float X=r*cos(a)*cos(b);
	float Z=r*sin(a)*cos(b);
	float Y=r*sin(b);
	float p1=mouse.y*10.0;
	float p2=mouse.x*10.0;
	float Xt=X*cos(p2)-Y*sin(p2);
	float Y1t=X*sin(p2)+Y*cos(p2);
	float Yt=Y1t*cos(p1)-Z*sin(p1);
	float Zt=Y1t*sin(p1)+Z*cos(p1);
	float u=atan(Yt,Xt); float v=asin(Zt/r);
	//float g= (cos(20.0*(u+v))+cos(20.0*(v-u)))/2.0;
	//float f=(sin(8.0*v)-0.5*(sin(5.0*u-t*2.0)+sin(5.0*u+t*4.0)))/3.0;
	//float f=(v*u*2.0)*sin(5.0*(u*u-v*v)-t);
#ifdef COMPLEX
	//mat2 cz=complex(u,v);
	//mat2 w=t1(t1(t1(t1(cz))));
	//float ut=RZ(w);
	//float vt=IZ(w);
	//float f=fun2d(ut,vt);
	
	mat2 cz=complex(Xt,Yt);
	mat2 w=(((t1(cz))));
	float xt=RZ(w);
	float yt=IZ(w);
	float f=fun3d(xt,yt,Zt);
#else
	//float f=fun2d(u,v);
	float f=fun3d(Xt,Yt,Zt);
#endif
	return f;
}
void main(void)
{
   float x = gl_FragCoord.x;
   float y = gl_FragCoord.y;
   float w = resolution.x;
   float h = resolution.y;
	const float m=5.0;
   const float X1 = -m;
   const float X2 = m;
   const float Y1 = -m;
   const float Y2 = m;

   float X = (((X2-X1)/w)*x+X1);
   float Y = ((((Y2-Y1)/h)*y+Y1)*h/w);
   float f = curve(X,Y);

#ifdef EDGE
	float d = 0.1;
	float c = 1.0;
   	if (abs(f)<=d)
   	{
      		c = 0.0;
   	}
	gl_FragColor = vec4(vec3(c),1.0);
#else
	//float c = (f+1.0)/2.0;
   	//gl_FragColor = vec4(vec3(step(0.5,c)),1.0);
	
	float c=f;
	if (c>0.0)
      	{
         	gl_FragColor = vec4(c,0.0,0.0,1.0);
      	}
      	if (c<=0.0)
      	{
         	gl_FragColor = vec4(0.0,abs(c),0.0,1.0);
      	}
#endif
}

