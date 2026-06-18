/*{
    "DESCRIPTION": "DotMatrix-Emerald-Rotating-2",
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
        "color",
        "geometric",
        "3d"
    ]
}*/




#define time ((useFrameIndex ? (float(FRAMEINDEX) / fps) : TIME) * speed)
#define mouse vec2(mouseX, mouseY)
#define resolution (RENDERSIZE / zoom)
#define PROCESSING_COLOR_SHADER

// point de départ: http://www.iquilezles.org/www/articles/distfunctions/distfunctions.htm
//  distance  à l'octaedre:  thematica

#ifdef GL_ES
precision mediump float;
#endif

float pointToBoxVide( vec3 p, vec3 b, float r )
	{	vec3 pa=abs(p);
		float dy= length( vec3( b.x-pa.x  ,  b.z-pa.z,  (-b.y+pa.y)*step(b.y,pa.y) ))-r;
		float dx= length( vec3( b.y-pa.y  ,  b.z-pa.z,  (-b.x+pa.x)*step(b.x,pa.x) ))-r;
		float dz= length( vec3 (b.x-pa.x  ,  b.y-pa.y,  (-b.z+pa.z)*step(b.z,pa.z) ))-r;
		return min(dx,min(dy,dz));
	}
float pointToOctaedre(vec3 p, float h, float c){
		vec3 pa=abs(p);
		pa.z=max(abs(p.x),abs(p.z));
		pa.x=min(abs(p.x),abs(p.z));
		vec2 v=normalize(vec2(h,c));
		float dis= abs(pa.z*v.x+ v.y*pa.y-v.x*h);
		return dis;

}

float pointToO( vec3 ppp)
{	
	float res = pointToOctaedre( ppp, 1.6+0.5*sin(time) , 2.0+cos(time));
	return res;	
}	

vec3 rotZ( vec3 p, float tc)
{			
		float  c = cos(tc);
		float  s = sin(tc);
		mat3   mz = mat3(c,-s,0.0,s,c,0.0,0.0,0.0,1.0);
		mat3 mx=mat3(1.0,0.0,0.0,0.0,c,-s,0.0,s,c);
		return  mz*mx*p ;
 }

vec3 orthogonal(vec3 p) 
{
	float eps = 0.001;
	vec3 u=vec3(eps,0.0,0.0);
	vec3 v=vec3(0.0,eps,0.0);
	vec3 w=vec3(0.0,0.0,eps);
	vec3  n = vec3(pointToO(p+u) - pointToO(p-u),pointToO(p+v) - pointToO(p-v),pointToO(p+w) - pointToO(p-w)) ;			
	return normalize(n);
}

vec4 render( vec3 org, vec3 dir)
{
	
	float lambda = 0.0;
	vec3 pos= org ;
	float lamb=1.0;
for(int  compte=0; compte<150; compte++)
	{
	lamb= pointToO(pos);
	lambda+=lamb;
	pos=org+lambda*dir;
	if(lamb<1.0e-4) break;
	}
	if (abs(lamb)>1.0e-3) return  vec4(dir,1.0);
	else
	{
	float reflet =abs(dot(dir,orthogonal(pos)));
	return vec4(0.5+0.5*reflet,0.2+0.8* reflet, reflet*0.7+0.3,1.0);
	}	
}

void _userMain( void )
{
	vec2 p = -1.0+ 2.0*gl_FragCoord.xy/resolution.xy;
	
	//déplacement de l'oeil 
	vec3 oeil = vec3( 0.0, 2.5 ,10.0);
	oeil=rotZ(oeil,  time);
	// le repere camera
	vec3 cw = normalize(-oeil );
	vec3 cp = vec3( 0.0, 1.0, 0.0 );
	vec3 cu = normalize( cross(cw,cp) );
	vec3 cv = normalize( cross(cu,cw) );
	vec3 dirRayon = normalize( p.x*cu + p.y*cv+ 4.5*cw );		
	gl_FragColor= render( oeil, dirRayon );
	
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