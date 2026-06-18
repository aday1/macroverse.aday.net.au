/*{
    "DESCRIPTION": "DotMatrix-Rainbow-AtmosphericHaze-9",
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
        "3d",
        "texture-input"
    ]
}*/




#define time ((useFrameIndex ? (float(FRAMEINDEX) / fps) : TIME))
#define mouse vec2(0.0)
#define resolution RENDERSIZE
#ifdef GL_ES
precision highp float;
#endif

uniform sampler2D renderbuffer;
#define PI                  	(4.*atan(1.))       
#define TAU                 	(8.*atan(1.))   

#define ASPECT               	resolution.x/resolution.y
#define PHI                  	.00001
#define EPSILON                 .00002
#define FOV                     3.5
#define FARPLANE                24.
#define ITERATIONS              256

#define DEPTH 6
#define SCALE pow(.5, float(DEPTH))

#define VIEWPOSITION    vec3(sin(time*0.01), cos(time*0.01), -1.5)
#define VIEWTARGET      vec3(0.5, 0.5, 0.25)

struct ray
{
	vec3 origin;
	vec3 position;
	vec3 direction;
	float range;
	float steps;
}; 

ray         view(in vec2 uv);   
void        emit(inout ray r);
float       map(in vec3 position);
vec3        derive(in vec3 p);

float       sphere(vec3 position, float radius);
float       cube(vec3 position, vec3 scale);
float       torus( vec3 p, vec2 t );
float       cylinder(vec3 p, float l, float r);
float       cone(vec3 p, float l, vec2 r);
float       icosahedral(vec3 p, float e, float r);
float       partition_noise(vec2 uv);
float       cross(float x);

float       hash(float x);
vec2        hash(vec2 v);

mat2        rmat(in float r);

vec2        format_to_screen(vec2 uv);

vec2    compose(vec2 a, vec2 b);
vec2    rotate(vec2 t, vec2 p);
vec2    postorot(vec2 p);
float   cartolin(float c);
float   cartohilbert(vec2 p);
vec2    hilberttocar(float l);

float   curve(vec2 p);
vec4    faketexture(sampler2D t, vec2 uv);

vec3 hsv(in float h, in float s, in float v){
	return mix(vec3(1.),clamp((abs(fract(h+vec3(3.,2.,1.)/3.)*6.-3.)-1.),0.,1.),s)*v;
}

//composition of two "rotations"
vec2 compose(vec2 a, vec2 b){
	return vec2(dot(a, b), dot(a, b.yx));
}

//action of rotation on "elementary" coordinates
vec2 rotate(vec2 t, vec2 p)
{
	return compose(t, p - .5) + .5;
}

//given "elementary" coordinates of position, returns the corresponding "rotation."
vec2 postorot(vec2 p)
{
	return vec2(p.y, (1.-2. * p.x) * (1.-p.y));
}

//given "elementary" coordinates of position (c=2*p.x+p.y), returns the "elementary" linear coordinates.
float cartolin(float c)
{
	return max(0., .5 * ((-3. * c + 13.) * c - 8.));
}

//given a point inside unit square, return the linear coordinate
float cartohilbert(vec2 p, int iterations)
{
	vec2 t  = vec2(1., 0.);                    //initial rotation is the identity
	float l = 0.;                              //initial linear coordinate
	float scale = pow(.5, float(iterations));
	for(int i = 0; i<DEPTH;i++){
		if(i < iterations)
		{
			p        *= 2.; 
        
      		 	 vec2 lp  = floor(p); 
     			   p        -= lp;                         //extract leading bits from p. Those are the "elementary" (cartesian) coordinates.
			lp       = rotate(t,lp);                //rotate p0 by the current rotation
		
       			 t        = compose( t, postorot(lp));   //update the current rotation

      			  float c  = lp.x * 2. + lp.y;
			l        = l * 4. + cartolin(c);        //update l
		}
		else
		{
			 break;
		}
	}

    return l * scale * scale;                   //scale the result in order to keep between 0. and 1.
}

//given the linear coordinate of a point (in [0,1]), return the coordinates in unit square
//it's the reverse of cartohibert
vec2 hilberttocar(float l)
{
	vec2 t = vec2(1., 0.);
	vec2 p = vec2(0.);
	for(int i=0; i<DEPTH;i++){
		l       *= 4.; 
        	float c = floor(l); 
        	l       -= c;
		c       = 0.5* cartolin(c);
		vec2 lp = vec2(floor(c), 2. * (c-floor(c)));
        	t       = compose( t, postorot(lp));
		lp      = rotate(t,lp);
		p       = p * 2. + lp;
	}
	return p*SCALE;
}

float depth = 0.;
float map(in vec3 position)
{
	position.xz 	*= rmat(3.14);
	position.z 	+= time * .5;
	float c 	= 0.;
	
	float t = time * .25;
	float f = floor(0.);
	c += cartohilbert(fract(abs(position.xz)+c*f)+.5, 6);	
	depth 		= c;
	return abs(position.y*1.5)-.25 - abs(fract(c+t)*2./(1.+f))*.05;
}

void main( void ) 
{
	vec2 uv         	= gl_FragCoord.xy/resolution.xy;
	ray r           	= view(uv);
	
	emit(r);

	float distanceFog   	= clamp(r.range/FARPLANE, 0., 1.);
	float stepFog       	= clamp(r.steps/float(ITERATIONS), 0., 1.);
	stepFog        	 	= r.steps < 1. ? 1. : stepFog;
	
	vec4 result     	= vec4(0.);
	result          	= pow(stepFog, .5) * vec4(1., 1.2, 1.35,1.) * 1.15;
	result	        	+= r.steps > float(ITERATIONS - ITERATIONS/4) ? 1. : 0.;
	result      		= pow(result,vec4(2.));
	result      		-= r.range*.012;
	result.xyz		*= hsv(depth, 1.,1.);
	result.w   		 = 1.;

	gl_FragColor = result;
}// sphinx

void emit(inout ray r)
{
	float total_range       = r.range;
	float threshold     = PHI;
	
	for(int i = 1; i < ITERATIONS; i++)
	{
		if(total_range < FARPLANE)
		{
			if(r.range < threshold && r.range > 0.)
			{
				r.range = total_range;
				r.steps            = float(i);
				break;
			}

			threshold       *= 1.04;
			r.position      += r.direction * r.range * .2;
			r.range   	= map(r.position);
			
			if(r.range < 0.)
			{
				r.range		-= threshold;
				threshold	*= float(i);
			}
			total_range        += r.range;
		}
		else
		{
			r.range = 1.+length(r.origin + r.direction * FARPLANE);
			r.steps = float(i);
			break;
		}
	}
}

vec2 format_to_screen(vec2 uv)
{
	uv = uv * 2. - 1.;
	uv.x *= ASPECT;
	return uv;
}

ray view(in vec2 uv)
{ 
	uv = format_to_screen(uv);

	vec3 w          = normalize(VIEWTARGET-VIEWPOSITION);
	vec3 u          = normalize(cross(w,vec3(0.,1.,0.)));
	vec3 v          = normalize(cross(u,w));

	ray r           = ray(vec3(0.), vec3(0.), vec3(0.), 0., 0.);
	r.origin        = VIEWPOSITION;
	r.position      = VIEWPOSITION;
	r.direction     = normalize(uv.x*u + uv.y*v + FOV*w);;
	r.range	        = 0.;
	r.steps         = 0.;

	return r;
}   

float sphere(vec3 position, float radius)
{
	return length(position)-radius; 
}

float cube(vec3 p, vec3 s)
{
	vec3 d = (abs(p) - s);
	return min(max(d.x,max(d.y,d.z)),0.0) + length(max(d,0.0));
}

float torus( vec3 p, vec2 t )
{
	vec2 q = vec2(length(p.xz)-t.x, p.y);
	return length(q)-t.y;
}

float cylinder(vec3 p, float l, float r)
{
	return max(abs(p.y-l)-l, length(p.xz)-r);
}

float cone(vec3 p, float l, vec2 r)
{
	float m = 1.-(p.y*.5)/l;
	return max(length(p.xz)-mix(r.y, r.x, m), abs(p.y-l)-l);
}

mat2 rmat(in float r)
{
	float c = cos(r);
	float s = sin(r);
	return mat2(c, s, -s, c);
}

