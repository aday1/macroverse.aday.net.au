/*{
    "DESCRIPTION": "ColorDecoder2-XY",
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
        }
    ],
    "TAGS": [
        "geometric",
        "texture-input"
    ]
}*/



#define time ((useFrameIndex ? (float(FRAMEINDEX) / fps) : TIME))
#define mouse vec2(0.0)
#define resolution RENDERSIZE
#ifdef GL_ES
precision highp float;
#endif
#define PI (4./atan(1.))
#define ITERATIONS 8
#define SCALE pow(.5, float(ITERATIONS))

//adapted from https://www.shadertoy.com/view/XdSGRR by Knightly

uniform sampler2D renderbuffer;

vec2    compose(vec2 a, vec2 b);
vec2    rotate(vec2 t, vec2 p);
vec2    postorot(vec2 p);
float   cartolin(float c);
float   cartohilbert(vec2 p);
vec2    hilberttocar(float l);

float   line(vec2 p, vec2 a, vec2 b, float w);

float   curve(vec2 p);
vec4    faketexture(sampler2D t, vec2 uv);

mat2    rmat(float r);
float   kali(vec2 p, vec3 c, float r);

void frosty(void);

void main(void)
{
    float a     = resolution.y/resolution.x;
	vec2 uv     = gl_FragCoord.xy/resolution.xy; //normalized xy pixel position

    bool right  = uv.x < a;
    uv.x        = right ? uv.x : uv.x - a ;
    uv.x        *= resolution.x/resolution.y;    //pixels centered and split into left and right output
    
    float t     = mouse.x * 4.;                  //translation

    float o     = .5 * SCALE;                    //offset
    
    vec4 result = vec4(0.);
	if(right)
    {
        //hash the texture via the curve
        float area = SCALE*SCALE;
        
        float l   = cartohilbert(uv);
    	float t   = mod( 1./4. * t,    2.) * 1. / area;
    	l         = mod(l + t * area, 1.);

        vec2 p    = hilberttocar(l)+o;
        result    = faketexture(renderbuffer, p);
    }
    else
    {
	    //show the texture along the hilbert curve
	    uv        = floor(uv / SCALE) * SCALE;
    	float l   = uv.x * SCALE + uv.y;
    	vec2 p    = hilberttocar(l) + o;
        
    	result    = faketexture(renderbuffer,p);
    }
    
    float c   = curve(uv);
    result   -= c ;
    
    gl_FragColor = result;
	
	frosty();
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
float cartohilbert(vec2 p)
{
	vec2 t  = vec2(1., 0.);                    //initial rotation is the identity
	float l = 0.;                              //initial linear coordinate
	for(int i = 0; i<ITERATIONS;i++){
		p        *= 2.; 
        
        vec2 lp  = floor(p); 
        p        -= lp;                         //extract leading bits from p. Those are the "elementary" (cartesian) coordinates.
		lp       = rotate(t,lp);                //rotate p0 by the current rotation
		
        t        = compose( t, postorot(lp));   //update the current rotation

        float c  = lp.x * 2. + lp.y;
		l        = l * 4. + cartolin(c);        //update l
	}

    return l * SCALE * SCALE;                   //scale the result in order to keep between 0. and 1.
}

//given the linear coordinate of a point (in [0,1]), return the coordinates in unit square
//it's the reverse of cartohibert
vec2 hilberttocar(float l)
{
	vec2 t = vec2(1., 0.);
	vec2 p = vec2(0.);
	for(int i=0; i<ITERATIONS;i++){
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

float line(vec2 p, vec2 a, vec2 b, float w){
	if(a==b)return(0.);
	float d = distance(a, b);
	vec2  n = normalize(b - a);
    vec2  l = vec2(0.);
	l.x = max(abs(dot(p - a, n.yx * vec2(-1.0, 1.0))), 0.0);
	l.y = max(abs(dot(p - a, n) - d * 0.5) - d * 0.5, 0.0);
	return smoothstep(w, 0., l.x+l.y);
}

mat2 rmat(float r)
{
    float c = cos(r);
    float s = sin(r);
    return mat2(c, s, -s, c);
}

float kali(vec2 p, vec3 c, float r)
{
    p *= 2.;
	mat2 rot = rmat(r);
	rot     *= c.x; 
	for (int i = 0; i < 12; i++) 
	{
        p   *= rot;
    	p    = abs(p)-c.x;
	    c.z *= c.x;
	}
   
    float k = (length(p)-c.y)/c.z;
	return abs(fract(k*2.)-.5)*2.;
}

//returns the distance to hilbert curve.
float curve(vec2 p) 
{
    float area    = SCALE*SCALE;
    vec2 offset   = vec2(.5*SCALE);

	float l  = cartohilbert(p);                            //get linear coordinate of the nearest vertex in the curve to p
	vec2 lp  = SCALE*floor(p/SCALE)+offset;                //nearest vertex in the curve to p

    vec2 p0  = hilberttocar(max(l-area,0.))+offset;        //previous vertex
	vec2 p1  = hilberttocar(min(l+area,1.-area))+offset;   //next vertex

    float w  =  .01 / float(ITERATIONS);
    float l0 = line(p, lp, p0, w);
    float l1 = line(p, lp, p1, w);
	return max(l0, l1);
}

vec4 faketexture(sampler2D t, vec2 uv)
{
    vec4 result = vec4(uv, 1.-length(uv)*.5, 1.);
    
    /*/decorative fractal
    const float p = 1.57;
    vec3 c        = vec3(2., 1., .025);
    float r       = .5 * p;
    float k       = kali(uv, c, r);
    result *= k * .25 + .75;
    */
    return result;
}

uniform sampler2D backbuffer;
void frosty(void){
	
	vec2 uv = gl_FragCoord.xy/resolution;
	vec2 d = uv-mouse;
	d = pow(d, vec2(8.))*pow(10., 9.);
	if(length(d) < 1.){
		const float brightening = 0.02;
		const float rmdr = (1.-brightening);
		const float blurring = 0.03;
		gl_FragColor = brightening + blurring * rmdr * gl_FragColor;
		gl_FragColor += 0.25 * (1.-blurring) * rmdr * texture2D(backbuffer, (gl_FragCoord.xy+vec2(2,0))/resolution);
		gl_FragColor += 0.25 * (1.-blurring) * rmdr * texture2D(backbuffer, (gl_FragCoord.xy+vec2(-2,0))/resolution);
		gl_FragColor += 0.25 * (1.-blurring) * rmdr * texture2D(backbuffer, (gl_FragCoord.xy+vec2(0,2))/resolution);
		gl_FragColor += 0.25 * (1.-blurring) * rmdr * texture2D(backbuffer, (gl_FragCoord.xy+vec2(0,-2))/resolution);
		
	}
}
