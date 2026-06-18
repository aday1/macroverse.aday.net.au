/*{
    "DESCRIPTION": "CubeBlackHole1",
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
        "3d"
    ]
}*/



#define time ((useFrameIndex ? (float(FRAMEINDEX) / fps) : TIME))
#define mouse vec2(0.0)
#define resolution RENDERSIZE

float xor(float a, float b)
{
	vec2 n		= vec2(a,b);
	float c		= 0.;
	const int bits	= 6;
	float word	= pow(2., float(bits));
	for(int i=0; i < bits; i++) 
	{

		c 	+= mod(dot(floor(n * word), vec2(1.)), 2.);		//xor
		c	*= .5;
		n 	*= .5;
 	}
	return c;
}

float and(float a, float b)
{
	vec2 n		= vec2(a,b);
	float c		= 0.;
	const int bits	= 5;
	float word	= pow(2., float(bits));
	for(int i=0; i < bits; i++) 
	{

		c 	+= mod(floor(n.x * word) * floor(n.y * word), 2.);	//and
		c	*= .5;
		n 	*= .5;
 	}
	return c;
}

float or(float a, float b)
{
	vec2 n		= vec2(a,b);
	float c		= 0.;
	const int bits	= 5;
	float word	= pow(2., float(bits));
	for(int i=0; i < bits; i++) 
	{

		c 	+= mod(dot(floor(n * word), vec2(1.)), 2.);		//xor
		c 	+= mod(floor(n.x * word) * floor(n.y * word), 2.);	//and
		c	*= .5;
		n 	*= .5;
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

vec3 orbit(vec3 p)
{
	return abs(p)/dot(p,p);	
}

float hash(float v)
{
	v *= 1234.5678;
	return fract(v*fract(v));
	
}

float map(in vec3 position) 
{
	position			= -abs(position);

	position			*= .125;

	float r			= xor(position.x, position.y)+xor(position.z, position.y)+xor(position.x, position.z);
	r			= pow(r+r,2.)*1.5;	
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

mat2 rmat(float r)
{
    float c = cos(r);
    float s = sin(r);
    return mat2(c, s, -s, c);
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

	float fov = 1.124;
	
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
    	vec3 origin 		= vec3(3.);
	float tau		= 8.*atan(1.);   // using const gives a compiler error!!!
	origin.yz		*= rmat(mpos.y*tau);
    	origin.xz		*= rmat(mpos.x*tau);

	//ray position
	vec3 position		= origin;

	// view direction
    	vec3 direction		= view(uv, origin);

   	//bounding sphere
	vec2 bound 		= iSphere( origin, direction, vec4(0.,0.,0.,3.5) );

	// raymarch
	float range 		= 0.;
	float total_range	= bound.x;
	vec3 light		= vec3(mpos * 3., 1.);
	vec3 normal 		= vec3(0.);
	vec3 color 		= vec3(0.);

	float decay		= .95;	
	for(float i = 1.; i < 48.; i++)
	{
		total_range	= mix(total_range, exp(range)*.05, .005);   
			
		if (total_range > bound.y) break;
		
		position 	= origin + direction * total_range;
		range 		= abs(map(position));

		color		+= hsv(.25-range * .5, .125+range*.05, range*.0075);
		color 		*= decay;
	}

	gl_FragColor = vec4(color, 1. );
}//sphinx
