/*{
    "DESCRIPTION": "rightiswhite",
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
        },
        {
            "NAME": "inputColour",
            "TYPE": "vec4",
            "LABEL": "Input Colour"
        }
    ],
    "TAGS": [
        "grid"
    ]
}*/
#define E 2.71828182846
uniform float timeScale;




#define time ((useFrameIndex ? (float(FRAMEINDEX) / fps) : TIME) * timeScale)
#define mouse vec2(mouseX, mouseY)
#define resolution RENDERSIZE
// right is white
// left is black
// red in the center
// rest is blue
// vector field how to solve you?

#ifdef GL_ES
precision mediump float;
#endif

uniform vec4 inputColour;

varying vec2 surfacePosition;

float t = 1.0*time;
float pi=atan(1.0)*4.0;

#define CELL_SIZE 16.0

float Ex(float x, float y)
{
	//return (sin(y)+sin(x));
	//return x*(x+y)*(x*y-1.0);
	return (y+1.0)*(x*x*y-1.0)*x;
	//return sin(x+y-t/4.0);
}
float Ey(float x, float y)
{
	//return (sin(y)-sin(x));
	//return y*(y-x)*(x*y+1.0);
	return y*y*(1.0-2.0*x*x);
	//return sin(y-x+t/4.0);
}

void main(void)
{
   float x = gl_FragCoord.x;
   float y = gl_FragCoord.y;
	//float x=surfacePosition.x;
	//float y=surfacePosition.y;
   float w = resolution.x*inputColour.x;
   float h = resolution.y*inputColour.y;

   float s = CELL_SIZE;
   float a = ceil(x/s);
   float b = ceil(y/s);
   float xd = s*(a-mouse.x);
   float yd = s*(b-mouse.y);

   float m=10.0;
   float X1 = -m;
   float X2 = m;
   float Y1 = -m;
   float Y2 = m;

   float X = (((X2-X1)/w)*x+X1);
   float Y = ((((Y2-Y1)/h)*y+Y1)*h/w);
   float Xd = (((X2-X1)/w)*xd+X1);
   float Yd = ((((Y2-Y1)/h)*yd+Y1)*h/w);
   
   float ex = Ex(Xd,Yd);
   float ey = Ey(Xd,Yd);
   float ex2 = Ex(X,Y);
   float ey2 = Ey(X,Y);
   float p = ey/ex;
   float f = (Y-Yd)-p*(X-Xd);
   float c = atan(ey2,ex2)/pi;
   if (abs(f)<=0.01) c=1.0;

#if 0
   gl_FragColor = vec4(0.0,0.0,0.0,1.0);
#else
   if (c>=0.0 && c<=0.5)
   {
      gl_FragColor = vec4(2.0*c,0.0,0.0,1.0);
   }
   if (c>0.5 && c<=inputColour.z)
   {
      gl_FragColor = vec4(1.0,c*2.0-1.0,c*2.0-1.0,1.0);
   }
   if (c<0.0 && c>=-inputColour.w)
   {
      gl_FragColor = vec4(0.0,0.0,abs(c)*2.0,1.0);
   }
   if (c<-0.5 && c>-1.0)
   {
      gl_FragColor = vec4(abs(c)*2.0-1.0,abs(c)*2.0-1.0,1.0,1.0);
   }
#endif   
}
