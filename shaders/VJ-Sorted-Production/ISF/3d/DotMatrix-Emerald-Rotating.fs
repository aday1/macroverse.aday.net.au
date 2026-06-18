/*{
    "DESCRIPTION": "DotMatrix-Emerald-Rotating",
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
            "NAME": "val_n1_6",
            "TYPE": "float",
            "DEFAULT": 0.5,
            "MIN": 0,
            "MAX": 1
        }
    ],
    "TAGS": [
        "3d"
    ]
}*/




#define time ((useFrameIndex ? (float(FRAMEINDEX) / fps) : TIME) * timeScale)
#define mouse vec2(mouseX, mouseY)
#define resolution RENDERSIZE
/*{
    "DESCRIPTION": "UNSORTEDSHADER224",
    "CREDIT": "GLSL Sandbox / various",
    "ISFVSN": "2.0",
    "CATEGORIES": ["misc"],
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
    ]
}*/

#define time ((useFrameIndex ? (float(FRAMEINDEX) / fps) : TIME) * timeScale)
#define mouse vec2(mouseX, mouseY)
#define resolution RENDERSIZE
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
	float res = pointToOctaedre( ppp, val_n1_6+0.5*sin(time) , 2.0+cos(time));
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

void main( void )
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

