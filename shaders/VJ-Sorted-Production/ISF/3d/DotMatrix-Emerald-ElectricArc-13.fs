/*{
    "DESCRIPTION": "DotMatrix-Emerald-ElectricArc-13",
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
        "geometric",
        "3d"
    ]
}*/




#define time ((useFrameIndex ? (float(FRAMEINDEX) / fps) : TIME))
#define mouse vec2(0.0)
#define resolution RENDERSIZE

#define BITS 5
//#define AND
#define XOR

float bitwise(vec2 v)
{
	float word	= pow(2., float(BITS));
	v		= floor(v*word)/word;
	float c		= 0.;
	for(int i=0; i < BITS; i++) 
	{
		vec2 n	= floor(v * word);

		#ifdef XOR
		c 	+= mod(n.x + n.y, 2.);
		#endif
		
		#ifdef AND
		c 	+= mod(n.x * n.y, 2.);
		#endif 
		
		c 	*= .5;
		v 	*= .5;
 	}	
	return c;
}

vec2 iSphere( in vec3 ro, in vec3 rd, in vec4 sph )//from iq
{
	vec3 oc = ro - sph.xyz;
	float b = dot( oc, rd );
	float c = dot( oc, oc ) - sph.w * sph.w;
	float h = b*b - c;
	if( h<0.0 ) return vec2(-1.0);
	h = sqrt(h);
	return vec2(-b-h, -b+h );
}

mat2 rmat(float r)
{
    float c = cos(r);
    float s = sin(r);
    return mat2(c, s, -s, c);
}

#define TAU (8.*atan(1.))
float map(in vec3 position) 
{
	position.xz 		*= rmat((mouse.x-.5)*TAU);
	position.xy		*= rmat((mouse.y-.5)*TAU);
	position		*= 1.05;
	position 		= abs(position)/dot(position,position);
	float r			= bitwise(vec2(position.x,bitwise(position.yz)));
	return r;
}

vec3 derivative(vec3 position, float delta)
{
	vec2 offset 	= vec2(delta, 0.);
	vec3 normal 	= vec3(0.);
	normal.x 	= map(position+offset.xyy)-map(position-offset.xyy);
	normal.y 	= map(position+offset.yxy)-map(position-offset.yxy);
	normal.z 	= map(position+offset.yyx)-map(position-offset.yyx);
	return normalize(normal);
}

vec3 hsv(float h,float s,float v)
{
	return mix(vec3(1.),clamp((abs(fract(h+vec3(3.,2.,1.)/3.)*6.-3.)-1.),0.,1.),s)*v;
}

vec3 view(vec2 pixel, vec3 origin)
{ 
    	vec3 w = normalize( origin );
    	vec3 u = normalize( cross(w,vec3(0.0,1.0,0.0) ) );
    	vec3 v = normalize( cross(u,w));

	float fov = 1.7;
	
	return normalize(pixel.x*u + pixel.y*v + fov * w);
}

void main()
{
	// screen
	vec2 aspect 		= resolution/min(resolution.x, resolution.y);
	vec2 pixel 		= gl_FragCoord.xy/resolution.xy;
	vec2 uv	 		= (pixel - .5) * aspect * 2.;
	vec2 mpos = (mouse - .5) * aspect * 2.;

    	// view origin
    	vec3 origin 		= vec3(0., 0., 2.);

	// view direction
    	vec3 direction		= view(uv, origin);
	
	//ray position
	vec3 position		= origin;

   	//bounding sphere
	vec2 bound 		= iSphere( origin, direction, vec4(0.,0.,0.,1.) );

	// raymarch
	float range 		= 0.;
	float prior_range	= 0.;
	float total_range	= bound.x;

	vec3 color 		= vec3(0.);
	float scattering		= 0.;
	vec3 light		= normalize(position-vec3(-16., 32., 49.));
			
	float decay		= .975;	
	for(float i = 1.; i < 128.; i++)
	{
		total_range	= mix(total_range, range, .015);   
			
		position 	= origin + direction * total_range;
		range 		= abs(map(position));
		
		float delta	= abs(prior_range-range);
		prior_range	= range;
		
		float response	= abs(log(.25+range*range)*.0225);
		color		+= hsv(pow(range, .5), range*.5, 1.) * response;
		color 		*= decay;
		
		vec3 normal 	= vec3(0.);
		normal 		= (position+bound.x*direction+direction*total_range) * .5;
	        normal 		= reflect(direction, normal);
	
		vec3 ndh		= normalize(light-normal);
		scattering	+= max(pow(1.125*dot(ndh, light), 32.*delta)*.025, 0.);
		prior_range 	= range;
		
		color 		+= pow(delta, 16.)*.5;
		
		if (total_range >= bound.y) break;
	}

	vec3 normal 	= vec3(0.);
	normal 		= (origin-bound.x*direction) * .5;
	normal 		= reflect(direction, normal);
	
	light		= normalize(position-vec3(-8., 15., 49.));
	
	vec3 bg_color	= vec3(.2, .4, .4) - position.y*4.;

	float fresnel	= clamp(pow(cos(1.+.25*dot(normal,direction))-total_range*.015-range*.25,4.), 0., 1.);
	float incident 	= dot(normal, light);
	float specular	= max(pow(dot(normalize(normal+direction), light), 64.), 0.);

	color		= mix(color, hsv(1.-total_range * .5, .25, .5), .5);
	color 		+= incident 		* .0122;
	color 		+= fresnel 		* .125 * bg_color;
	color 		+= specular 		* .75;
	
	color 		*= float(.5*-bound.x>.5);

	color		= pow(color, vec3(2.5-fresnel*2.125));
	color 		+= scattering 		* .125 - .125;
//	

	gl_FragColor = vec4(color, 1. );
}//sphinx
