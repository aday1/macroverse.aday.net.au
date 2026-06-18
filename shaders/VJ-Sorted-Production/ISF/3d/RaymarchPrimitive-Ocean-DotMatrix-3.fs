/*{
    "DESCRIPTION": "RaymarchPrimitive-Ocean-DotMatrix-3",
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

// Thematica le 05 juin 2013: Billard circulaire
// credit: http://www.iquilezles.org/www/articles/distfunctions/distfunctions.htm

#ifdef GL_ES
precision mediump float;
#endif

const float deltaT=1.6;
const float rPiste=1.8;
vec3 colcontact=vec3(0.,1.0,0.);
vec3 centreBalle=vec3(0.,0.,0.);
float rBalle=0.2;
vec3 centrePiste=vec3(0.0,0.0,1.0);

float sdPlane( vec3 p )
{
	return p.y+0.2;
}

float sdSphere( vec3 p, float s )
{
    return length(p-centreBalle)-s;
}

float sdCyl(vec3 p, float h,float r){
vec3 pa=abs(p);
float ra=length(pa.xz);
float k=(r<ra)? length(vec2(ra-r,pa.y-h)) : pa.y-h-0.001;
 return (pa.y<h)?(ra-r) : k;

}

float udRoundBox( vec3 p, vec3 b, float r )
{
  return length(max(abs(p)-b,0.05))-r;
}

float udBox( vec3 p, vec3 b )
{
  return length(max(abs(p)-b,0.0));
}

//----------------------------------------------------------------------

float opS( float d1, float d2 )
{
    return max(-d2,d1);
}
vec4 opS( vec4 d1, vec4 d2 )
{
    return vec4(max(-d2.x,d1.x),d2.yzw);
}

float opU( float d1, float d2 )
{
	return (d1<d2) ? d1 : d2;
}

vec2 opU( vec2 d1, vec2 d2 )
{
	return (d1.x<d2.x) ? d1 : d2;
}

vec4 opU( vec4 d1, vec4 d2 )
{
	return (d1.x<d2.x) ? d1 : d2;
}

vec3 opRep( vec3 p, vec3 c )
{
    return mod(p,c)-0.5*c;
}

vec3 opTwist( vec3 p )
{
    float  c = cos(10.0*p.y+10.0);
    float  s = sin(10.0*p.y+10.0);
    mat2   m = mat2(c,-s,s,c);
    return vec3(m*p.xz,p.y);
}

vec3 opTwist( vec3 p, float tc, float ts )
{
    float  c = cos(tc*p.y+ts);
    float  s = sin(tc*p.y+ts);
    mat2   m = mat2(c,-s,s,c);
    return vec3(m*p.xz,p.y);
}
vec3 opRot( vec3 p, float tc, float ts )
{
    float  c = cos(tc*p.y+ts);
    float  s = sin(tc*p.y+ts);
    mat3  mz = mat3(c,-s,0.0,s,c,0.0,0.0,0.0,1.0);//rotation autour de Z
    vec3 pt=mz*p+vec3(0.0,0.2,0.3);
    mat3 mx= mat3(1.0,0.0,0.0,0.0,c,-s,0.0,s,c);//rotation autour de X
    return mx*pt;
}
vec3 rotZ( vec3 p, float te )
{
    float  c = cos(te);
    float  s = sin(te);
    mat3  mz = mat3(c,-s,0.0,s,c,0.0,0.0,0.0,1.0);//rotation autour de Z
    return mz*p;
}
vec3 rotY( vec3 p, float te )
{
    float  c = cos(te);
    float  s = sin(te);
    mat3  my = mat3(c,0.0,-s , 0.0,1.0,0.0, s,0.0,c);//rotation autour de Y
    return my*p;
}
vec3 rotX( vec3 p, float te )
{
    float  c = cos(te);
    float  s = sin(te);
    mat3  my = mat3( 1.0,0.0,0.0, 0.0,c, -s, 0.0,s,c);//rotation autour de X
    return my*p;
}

  vec3 symetrie(vec3 a,vec3 v){
	a=normalize(a);
	return -v+2.0*dot(v,a)*a;
}

vec3 centreprojectile(float timeo){	
	vec3 cp=centrePiste;
	float alpha0=deltaT*floor(timeo/deltaT);
	float reste=mod(timeo,deltaT);
	vec3 ou=rotY(vec3(rPiste,0.,0.),alpha0);
	vec3 ov=rotY(vec3(rPiste,0.,0.),alpha0+deltaT);
	colcontact=(reste<0.11)? vec3(1.,1.,1.) : vec3(0.,1.,0.);	
	return centrePiste+mix(ou,ov,reste/deltaT);
}	

//----------------------------------------------------------------------

vec4 pointToObjects( in vec3 pos ){
	vec4 r3=vec4(sdCyl(pos-centrePiste-vec3(0.,-rBalle,0.),rBalle*1.1,rPiste+rBalle+0.2),1.,0.,0.);
	vec4 r0=vec4(sdCyl(pos-centrePiste,rBalle-0.01,rPiste+rBalle),colcontact);
	vec4 r4=opS(r3,r0);
	vec4 r1= vec4(sdSphere( pos ,rBalle ),1.0,1.0,0.);
	vec4 r=opU(r4,r1);
    	vec4 res = opU( vec4( sdPlane(pos-vec3(0.,-rBalle,0.)), 1.0,0.,1.0 ), r);	
   	return res;
}

vec4 castRay( in vec3 ro, in vec3 rd )
{
    float precis = 0.001;
    float h=1.0/60.0;
    float t = 0.0;
   vec3 m = vec3(0.,0.,0.);
    for( int i=0; i<60; i++ )
    {
        if( abs(h)<precis||t>20.0 ) break;
        t += h;
        vec4 res = pointToObjects( ro+rd*t );
        h = res.x;
        m = res.yzw;
    }

    if( t>20.0 ) m=vec3(0.,0.,0.);
    return vec4( t, m );
}

float softshadow( in vec3 ro, in vec3 rd, in float mint, in float maxt, in float k )
{
	float res = 1.0;
    float dt = 0.02;
    float t = mint;
    for( int i=0; i<20; i++ )
    {
	if( t<maxt )
		{
        float h = pointToObjects( ro + rd*t ).x;
        res = min( res, k*h/t );
	t += 0.02;			
		}
    }
    return clamp( res, 0.0, 1.0 );

}
//--------------------------------------------------------------------------
vec3 calcNormal( in vec3 pos )
{
	vec3 eps = vec3( 0.001, 0.0, 0.0 );
	vec3 nor = vec3(
	pointToObjects(pos+eps.xyy).x - pointToObjects(pos-eps.xyy).x,
	pointToObjects(pos+eps.yxy).x - pointToObjects(pos-eps.yxy).x,
	pointToObjects(pos+eps.yyx).x - pointToObjects(pos-eps.yyx).x );
	return normalize(nor);
}
//---------------------------------------------------------------------------
float calcAO( in vec3 pos, in vec3 nor )// comme une  ombre de la lumiere ambiante
{
    float totao = 0.0;
    float sca = 1.0;// le scale est 1
    for( int aoi=0; aoi<2; aoi++ )
    {
        float hr = .05 + 0.1*float(aoi);
        vec3 aopos =  nor * hr + pos;
        float dd = pointToObjects( aopos ).x;
        totao += -(dd-hr)*sca;
        sca *= 0.75;// le scale est diminué
    }
    return clamp( 1.0 - 10.0*totao, 0.0, 1.0 );
}

vec3 render( in vec3 ro, in vec3 rd )
{ 
    vec3 col = vec3(0.,0.,0.);
    vec4 res = castRay(ro,rd);
    float t = res.x;           
   vec3 m = res.yzw; 

        vec3 pos = ro + t*rd;
        vec3 nor = calcNormal( pos );

		col = m;		
		float ao = calcAO( pos, nor );

		vec3 lig = normalize( vec3(1.0, 0.5, -1.0) );         
		float amb = clamp( 0.5+0.5*nor.y, 0.0, 1.0 );   
		float dif = clamp( 1.5*dot( nor, lig ), 0.0, 1.0 );                              
		float bac = clamp( dot( nor, normalize(vec3(-lig.x,0.0,-lig.z))), 0.0, 1.0 )*clamp( 1.0-pos.y,0.0,1.0);

		float sh = 1.0;
		if( dif>0.02 ) { sh = softshadow( pos, lig, 0.12, 10.0, 7.0 ); dif *= sh; }   //l'ombre pas si douce

		vec3 brdf = vec3(0.0);
		brdf += 0.20*amb*vec3(0.10,0.11,0.13)*ao;
        		brdf += 0.20*bac*vec3(0.15,0.15,0.15)*ao;
        		brdf += 1.20*dif*vec3(1.00,0.90,0.70);

		float pp = clamp( dot( reflect(rd,nor), lig ), 0.0, 1.0 );
		float spe = sh*pow(pp,32.0);
		float fre = ao*pow( clamp(1.0+dot(nor,rd),0.0,1.0), 2.0 );

		col = col*brdf + vec3(1.0)*col*spe + 0.2*fre*(0.1+0.9*col);
		col = col*brdf  ;	

	col *= exp( -0.01*t*t );
	return vec3( clamp(col,0.0,1.0) );
}

void _userMain( void )
{
	
	vec2 p = 2.0*gl_FragCoord.xy/resolution.xy-1.0;
	p.y*=resolution.y/resolution.x;
	vec2 mous =vec2(2.0* mouse.x-1.0,2.0* mouse.y-1.0); 
	float tm = 15.0 + time;
	 centreBalle=centreprojectile(time*0.6);
	// camera	
	float angleCamX=-0.5+0.2*cos(time);
	float angleCamY=cos(time*0.3);
	float focale=2.5;
		
	vec3 ro = rotX(vec3( 0.0, 0.0 , -4.0),angleCamX)+vec3(centreBalle.x,0.,centreBalle.z);
	vec3 rd=normalize(rotX(vec3(p.x,p.y,focale),angleCamX));

	vec3 col = render( ro, rd);
	col = sqrt( col );
	gl_FragColor=vec4( col.x, col.y*0.8,2.0*col.z,1.0 );

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