/*{
    "DESCRIPTION": "DotMatrix-LitSurface-3",
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

//
// Thematica : Torsade
// Credit: http://glsl.heroku.com/e#8774.0 

#ifdef GL_ES
precision mediump float;
#endif

const float pi=3.14159;
const float pr=1.3;
const float gr=1.5;

vec3 rotX(vec3 v, float angle)
{
    float s = sin(angle);
    float c = cos(angle);
    mat3 mx=mat3(1.0,0.0,0.0,0.0,c,-s,0.0,s,c);
   return mx*v;
}

vec3 rotY(vec3 v, float angle)
{
    float s = sin(angle);
    float c = cos(angle);
    mat3 my=mat3(c,0.0,-s,0.0,1.0,0.0,s,0.0,c);
   return my*v;
}

vec3 rotZ(vec3 v, float angle)
{
    float s = sin(angle);
    float c = cos(angle);
    mat3 mz=mat3(c,-s,0.0,  s,c,0.0,  0.0,0.0,1.0);
   return mz*v;
}

vec3 rotate(vec3 v,vec2 a){
    float s = sin(a.x);
    float c = cos(a.x);
    mat3  mx=mat3(1.0,0.0,0.0,0.0,c,-s,0.0,s,c);
    s= sin(a.y);
    c = cos(a.y);
    mat3 mz=mat3(c,-s,0.0,  s,c,0.0,  0.0,0.0,1.0);
    return v*mx*mz;}

float pointToTore( vec3 p){	
	return length(vec2(length(p.xz)-gr,p.y))-pr;
}

float pointToSphere( vec3 p, float r )
{
return length(p)-r;
}

vec2 map( in vec3 pos )
{	
	
	float r3=pointToTore(pos);	
    	return vec2(r3,1.0);
    	
}

vec3 calcNormal( in vec3 pos)
{
	vec3 eps = vec3( 0.001, 0.0, 0.0 );
	vec3 nor = vec3(
	    map(pos+eps.xyy).x - map(pos-eps.xyy).x,
	    map(pos+eps.yxy).x - map(pos-eps.yxy).x,
	    map(pos+eps.yyx).x - map(pos-eps.yyx).x );
	return normalize(nor);
}

vec2 castRay( in vec3 ro, in vec3 rd )
{

    float precis = 0.001;
    float lamb=precis*2.0;
    float lambda = 0.0;
    float m = -1.0;
    for( int i=0; i<60; i++ )
    {
        if( abs(lamb)<precis || lambda>20.0 ) break;
       lambda +=lamb;
	    vec2 res = map( ro+rd*lambda);
       lamb = res.x;
        m = res.y;
    }

    if( lambda>20.0 ) m=-1.0;
    return vec2( lambda, m );
}

vec3 render( in vec3 ro, in vec3 rd )
{ 
   vec2 res = castRay(ro,rd);
   float t = res.x;
   float m = res.y;
   vec3 col=vec3(0.0);
   if( m>-0.5 )
   {
      vec3 pos = ro + t*rd;
    
      float alpha=atan(pos.z,pos.x);
      float beta=atan(pos.y,(sin(alpha)+cos(alpha))*pos.y-gr);
     col=vec3(step(mod(alpha+beta,pi/6.0),pi/12.0));
     vec3 no= calcNormal(pos);
     col=dot(col,no)*no;
   }
   else
   {col=vec3(0.5); }   
   return vec3(sqrt(clamp(col,0.0,1.0) ));
}

void _userMain( void ) {

   vec2 q = gl_FragCoord.xy/resolution.xy;
   vec2 p = -1.0+2.0*q;
   p.x *= resolution.x/resolution.y;
	
   // camera
   vec2 angles=vec2((0.5-mouse.y)*pi,(mouse.x-0.5)*pi);
   vec3 ro =rotate(vec3(0.0,0.0,-3.5),angles);

  float focale=1.2;
   vec3 rd = normalize(rotate(vec3(p,focale),angles ));
   vec3 col = render( ro, rd);
   gl_FragColor=vec4( col, 1.0 );

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