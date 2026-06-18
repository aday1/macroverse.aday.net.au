/*{
    "DESCRIPTION": "aday",
    "CREDIT": "GLSL Sandbox / various",
    "ISFVSN": "2.0",
    "CATEGORIES": [
        "geometric"
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
        "geometric"
    ]
}*/



#define time ((useFrameIndex ? (float(FRAMEINDEX) / fps) : TIME) * timeScale)
#define mouse vec2(mouseX, mouseY)
#define resolution RENDERSIZE
//Borrowed code from GLSLSandbox.com -- A combination of a few shaders
// Your inputColour wont work in GLSLSandbox, but give ShaderLoader.DLL a go!!!
// And ShaderMaker.dll too
// @Aday_net_au was here April 2016 -- Thank you GLSLSandbox for the inspiration,
//I smashed a couple of shaders together, sorry i lost the credit :|
//Look around, you'll find them :)
// Using inputColour for OSC or FFT stuff
//Go check it out, https://www.youtube.com/watch?v=9lt2u4UlfyM

#ifdef GL_ES
precision mediump float;
#endif

uniform vec4 inputColour;
 
#define STEPS 72
#define PRECISION 0.01
#define DEPTH 15.0

//For the Bars
vec2 position;
vec3 bars;
float barsize = mouse.y;
float barsangle = 1.0;

vec3 color;

//Starfield
float rand (in vec2 uv) { return fract(sin(dot(uv,vec2(12.4124,48.4124)))*48512.41241); }
const vec2 O = vec2(1.,1.);
float noise (in vec2 uv) {
        vec2 b = floor(uv);
        return mix(mix(rand(b),rand(b+O.yx),.5),mix(rand(b+O),rand(b+O.yy),.5),.5);
}

#define DIR_RIGHT -1.
#define DIR_LEFT 1.
#define DIRECTION DIR_RIGHT

#define LAYERS 3
#define SPEED 10.
#define SIZE 5.

//returns 0/1 based on the state of the given bit in the given number
float getBit(float num,float bit){
    num = floor(num);
    bit = floor(bit);
    return float(mod(floor(num/pow(2.,bit)),2.) == .0);
}

//The Rasterbars
vec3 mixcol(float value, float r, float g, float b)
{
        return vec3(value * r, value * g, value * b);
}

void bar(float pos, float r, float g, float b)
{
         if ((position.y <= pos + barsize) && (position.y >= pos - barsize))
                bars = mixcol(3.0 - abs(pos - position.y) / barsize, r, g, b);
}

/// NUCODE

vec3 eye = vec3(0,0.1,-1)*3.0;
vec3 light = vec3(0,1,-1);

const float lineWidth = 0.02;
const float border = 0.05;
const float scale = 0.07;

float bounding, ground, letters;
const float groundPosition = -0.5;
const vec3 boundingSize = vec3(20,32,1.8)*scale;

float t = time;
float scene(vec3);

// Utilities
float udBox(vec3 p, vec3 s) { return length(max(abs(p)-s,0.0)); }

/*
    mat4 rotX = mat4(      vec4(1,0,0,0),
                           vec4(0,c,-s,0),
                           vec4(0,s,c,0),
                           vec4(0,0,0,1) );
    
    mat4 rotY = mat4(      vec4(c,0,-s,0),
                           vec4(0,1,0,0),
                           vec4(s,0,c,0),
                           vec4(0,0,0,1) );
    
    mat4 rotZ = mat4(      vec4(c,s,0,0),
                           vec4(-s,c,0,0),
                           vec4(0,0,1,0),
                           vec4(0,0,0,1) );
    
    mat4 pos = mat4(       vec4(1,0,0,s),
                           vec4(0,1,0,0),
                           vec4(0,0,1,c),
                           vec4(0,0,0,1) );
*/
mat3 rotX(float a) {float s=sin(a); float c=cos(a); return mat3(1,0,0,0,c,-s,0,s,c);}
mat3 rotY(float a) {float s=sin(a); float c=cos(a); return mat3(c,0,-s,0,1,0,s,0,c);}

float line(vec2 p, vec2 s, vec2 e) {s*=scale;e*=scale;float l=length(s-e);vec2 d=vec2(e-s)/l;p-=vec2(s.x,-s.y);p=vec2(p.x*d.x+p.y*-d.y,p.x*d.y+p.y*d.x);return length(max(abs(p-vec2(l/2.0,0))-vec2(l/2.0,lineWidth/2.0),0.0))-border;}

float A(vec2 p){float d=5.0;d=min(d,line(p,vec2(1,8),vec2(1,1.5)));d=min(d,line(p,vec2(1,1.5),vec2(5,1.5)));d=min(d,line(p,vec2(5,1.5),vec2(5,5)));d=min(d,line(p,vec2(5,5),vec2(1,5)));d=min(d,line(p,vec2(1,5),vec2(5,5)));d=min(d,line(p,vec2(5,5),vec2(5,8)));return d;}

float D(vec2 p){float d=1.0;d=min(d,line(p,vec2(1,8),vec2(4,8)));d=min(d,line(p,vec2(4,8),vec2(4.5,7.5)));d=min(d,line(p,vec2(4.5,7.5),vec2(5,6.25)));d=min(d,line(p,vec2(5,6.25),vec2(5,3.75)));d=min(d,line(p,vec2(5,3.75),vec2(4.5,2)));d=min(d,line(p,vec2(4.5,2),vec2(4,1.5)));d=min(d,line(p,vec2(4,1.5),vec2(1,1.5)));d=min(d,line(p,vec2(1,1.5),vec2(1,8)));return d;}

float Y(vec2 p){float d=1.0;d=min(d,line(p,vec2(1,1.5),vec2(3,5)));d=min(d,line(p,vec2(3,5),vec2(3,8)));d=min(d,line(p,vec2(3,8),vec2(3,5)));d=min(d,line(p,vec2(3,5),vec2(5,1.5)));return d;}

// Marching
vec3 getNormal(vec3 p){vec2 e=vec2(PRECISION,0);return(normalize(vec3(scene(p+e.xyy)-scene(p-e.xyy),scene(p+e.yxy)-scene(p-e.yxy),scene(p+e.yyx)-scene(p-e.yyx))));}
vec3 march(vec3 ro,vec3 rd){float t=1.0,d;for(int i=2;i<STEPS;i++){d=scene(ro+rd*t);if(d<PRECISION||t>DEPTH){break;}t+=d;}return(ro+rd*t);}
vec3 lookAt(vec3 o,vec3 t){vec2 uv=(2.0*gl_FragCoord.xy-resolution.xy)/resolution.xx;vec3 d=normalize(t-o),u=vec3(mouse.x,1,mouse.y),r=cross(u,d);return(normalize(r*uv.x+cross(d,r)*uv.y+d));}

vec3 processColor(vec3 p)
{
	float d = 1e10;
	
	vec3 n = getNormal(p);
	vec3 l = normalize(light-p);
	vec3 col;
	
	float dist = length(light-p);
	float diff = max(dot(n, normalize(light-p)),0.0);
	float spec = pow(diff, 2.0);
	
	if (ground<d) { col = vec3(diff+spec*1.0)*vec3(mouse.x,mouse.y,mouse.x); d = ground; }
	if (letters<d) { col = vec3(p.y*0.5+1.0,0.5,.11)+diff+spec; }
		
	col *= min(1.5, 1.5/dist);
	
	return col;
}

float scene(vec3 p)
{	
	p.x += 0.1;
	
	ground   = p.y-groundPosition;
	bounding = udBox(p,boundingSize);

	float d = 1e11;

  // IF I HAD LETTERS, THEY WOULD GO HERE

	letters = max(bounding, letters);
	
	d = min(d, ground);
	d = min(d, letters);
	
	return d +.01*sin(time*3.);
}

void main()
{	

// NUCODE

{
 {

        //Some Stars
        vec2 uv = ( gl_FragCoord.xy / resolution.xy )*SIZE;
        
        float stars = 0.;
        float fl, s;
        for (int layer = 2; layer < LAYERS; layer++)
        
                fl = float(layer);
                s = (10.-fl*10.);
                stars += step(.1,pow(noise(mod(vec2(uv.x*s + time*SPEED*DIRECTION - fl*10.,uv.y*s),resolution.x)),18.)) * (fl/float(LAYERS));

// NUCODE

	eye *= rotY(mouse.x - mouse.y + 0.2*sin(t));
	eye *= rotX(mouse.y - mouse.x);
	light.x = sin(t);

	vec3 p = march(eye,lookAt(eye,vec3(0)));

	   //The bars
       
            position = ( gl_FragCoord.xy / resolution.xy );
            position = position * vec2(1.5) - vec2(0.7);    
         
            bars = vec3(0., 0., 0.);
            float t = mod(time * 0.1, 10.) + time;

            bar(-0.5+abs(sin(t*2.)),                  1.0, 0.0, 0.0);
            bar(-0.5+abs(sin(t*2.+barsangle/6.)),     1.0, 1.0, 0.0);
            bar(-0.5+abs(sin(t*2.+barsangle/6.*3.)),  0.0, 0.0, 1.0);

        vec3 c = vec3(0);
  
        //Mixing it all together
        color = vec3(processColor(p))+(c)+(bars)+(stars);
     
     	// vec3 col = processColor(p);
     
       gl_FragColor = vec4(color,1.0);
     //    gl_FragColor = vec4(color, 1.0, col);

        }
} } 
