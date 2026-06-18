/*{
    "DESCRIPTION": "DotMatrix-Ocean-GradientShift",
    "CREDIT": "GLSL Sandbox / various",
    "ISFVSN": "2.0",
    "CATEGORIES": [
        "noise"
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
        "noise"
    ]
}*/




#define time ((useFrameIndex ? (float(FRAMEINDEX) / fps) : TIME))
#define mouse vec2(0.0)
#define resolution RENDERSIZE
#ifdef GL_ES
precision mediump float;
#endif

#define STEPSEC 0
#define HASCHRONO 0;
#define PI 3.14159265359
#define SCALE 0.75

const float radius   = 0.33;
const float radius_h = 0.29;
const float radius_m = 0.30;
const vec3 color1 = 0.8*vec3(0.2,0.9,0.9);
const vec3 color2 = vec3(1.1,0.05,0.5);

float d2y(float d){return 1./(0.2+d);}

vec2 p;
float a,r;

bool is_inside(float x, float y, float r)
{
	
	float rsq = radius*radius;
	float x2 = x*x;
	float y2 = y*y;
	
	if((x2+y2)<rsq)
	  return true;
	
	return false;
}

float angle(vec2 orig){
	vec2 t = p-orig;
	return 0.5-atan(t.x, -t.y)/(2.*PI);
}

float dtick(float r, float a, float n, float l, float f0, float f1){
	float h = f1*abs(fract(n*a+0.5)-0.5);
	float hi = f0*max(0., l-r);
	return 15.*length(vec2(h,hi));
}

float ticks(){

	float anglev = atan(p.x, -p.y);
	
	float a = 0.5-anglev/(2.*PI);
	
	float dh = dtick(r, a, 12., radius_h, 35., 6.);
	float dm = dtick(r, a, 60., radius_m, 200., 5.);
	
	if(anglev <0.75 && anglev > -0.75)
	   return 0.0;
	else return d2y(dh) + d2y(dm);
}

float circle(float R){
    float d=distance(r, R);
    return d2y(200.*d);
}

float hands(float e){
	
	float s = mod(90.0+time,60.); // 0.00->0.99
	
	#if STEPSEC
	s=floor(s);
	#endif
	
	float ah = 60.*a;
	float dr = 0.15*r*min(min(abs(s-ah),abs(s-ah+60.)),abs(s-ah-60.));
	float dl = 4.*max(0., r-(radius-0.15*radius));
	float d = length(vec2(dr, dl));
	float x = 0.01;
	return d2y(e*d) * step(x,r) + circle(x);
}

float chrono(float e, vec2 orig, float k){
	float s = mod(0.99,k); // 0.00->0.99
	float r = distance(p,orig);
	float ah = k*angle(orig);
	float dr = 20.*r*min(min(abs(s-ah),abs(s-ah+k)),abs(s-ah-k));
	float dl = 60.*max(0., r-0.03);
	float d = length(vec2(dr, dl));
	return d2y(e*d);
}

vec4 mod289(vec4 x)
{
    return x - floor(x * (1.0 / 289.0)) * 289.0;
}
 
vec4 permute(vec4 x)
{
    return mod289(((x*34.0)+1.0)*x);
}
 
vec4 taylorInvSqrt(vec4 r)
{
    return 1.79284291400159 - 0.85373472095314 * r;
}
 
vec2 fade(vec2 t) {
    return t*t*t*(t*(t*6.0-15.0)+10.0);
}
 
// Classic Perlin noise
float cnoise(vec2 P)
{
    vec4 Pi = floor(P.xyxy) + vec4(0.0, 0.0, 1.0, 1.0);
    vec4 Pf = fract(P.xyxy) - vec4(0.0, 0.0, 1.0, 1.0);
    Pi = mod289(Pi); // To avoid truncation effects in permutation
    vec4 ix = Pi.xzxz;
    vec4 iy = Pi.yyww;
    vec4 fx = Pf.xzxz;
    vec4 fy = Pf.yyww;
     
    vec4 i = permute(permute(ix) + iy);
     
    vec4 gx = fract(i * (1.0 / 41.0)) * 2.0 - 1.0 ;
    vec4 gy = abs(gx) - 0.5 ;
    vec4 tx = floor(gx + 0.5);
    gx = gx - tx;
     
    vec2 g00 = vec2(gx.x,gy.x);
    vec2 g10 = vec2(gx.y,gy.y);
    vec2 g01 = vec2(gx.z,gy.z);
    vec2 g11 = vec2(gx.w,gy.w);
     
    vec4 norm = taylorInvSqrt(vec4(dot(g00, g00), dot(g01, g01), dot(g10, g10), dot(g11, g11)));
    g00 *= norm.x;  
    g01 *= norm.y;  
    g10 *= norm.z;  
    g11 *= norm.w;  
     
    float n00 = dot(g00, vec2(fx.x, fy.x));
    float n10 = dot(g10, vec2(fx.y, fy.y));
    float n01 = dot(g01, vec2(fx.z, fy.z));
    float n11 = dot(g11, vec2(fx.w, fy.w));
     
    vec2 fade_xy = fade(Pf.xy);
    vec2 n_x = mix(vec2(n00, n01), vec2(n10, n11), fade_xy.x);
    float n_xy = mix(n_x.x, n_x.y, fade_xy.y);
    return 2.3 * n_xy;
}
 
// Classic Perlin noise, periodic variant
float pnoise(vec2 P, vec2 rep)
{
    vec4 Pi = floor(P.xyxy) + vec4(0.0, 0.0, 1.0, 1.0);
    vec4 Pf = fract(P.xyxy) - vec4(0.0, 0.0, 1.0, 1.0);
    Pi = mod(Pi, rep.xyxy); // To create noise with explicit period
    Pi = mod289(Pi);        // To avoid truncation effects in permutation
    vec4 ix = Pi.xzxz;
    vec4 iy = Pi.yyww;
    vec4 fx = Pf.xzxz;
    vec4 fy = Pf.yyww;
     
    vec4 i = permute(permute(ix) + iy);
     
    vec4 gx = fract(i * (1.0 / 41.0)) * 2.0 - 1.0 ;
    vec4 gy = abs(gx) - 0.5 ;
    vec4 tx = floor(gx + 0.5);
    gx = gx - tx;
     
    vec2 g00 = vec2(gx.x,gy.x);
    vec2 g10 = vec2(gx.y,gy.y);
    vec2 g01 = vec2(gx.z,gy.z);
    vec2 g11 = vec2(gx.w,gy.w);
     
    vec4 norm = taylorInvSqrt(vec4(dot(g00, g00), dot(g01, g01), dot(g10, g10), dot(g11, g11)));
    g00 *= norm.x;  
    g01 *= norm.y;  
    g10 *= norm.z;  
    g11 *= norm.w;  
     
    float n00 = dot(g00, vec2(fx.x, fy.x));
    float n10 = dot(g10, vec2(fx.y, fy.y));
    float n01 = dot(g01, vec2(fx.z, fy.z));
    float n11 = dot(g11, vec2(fx.w, fy.w));
     
    vec2 fade_xy = fade(Pf.xy);
    vec2 n_x = mix(vec2(n00, n01), vec2(n10, n11), fade_xy.x);
    float n_xy = mix(n_x.x, n_x.y, fade_xy.y);
    return 2.3 * n_xy;
}
 
float fbm(vec2 P, const int octaves, float lacunarity, float gain)
{
    float sum = 0.0;
    float amp = 1.0;
    vec2 pp = P;
     
    //int i;
     
    for(int i = 0; i < 8; i++)
    {
        amp *= gain; 
        sum += amp * cnoise(pp);
        pp *= lacunarity;
    }
	
    return sum;
 
}

float pattern(in vec2 p) {
    float l = 2.5;
    float g = 0.4;
    int oc = 10;
     
    vec2 q = vec2( fbm( p + vec2(0.0,0.0),oc,l,g),fbm( p + vec2(5.2,1.3),oc,l,g));
    vec2 r = vec2( fbm( p + 4.0*q + vec2(1.7+time,9.2+time),oc,l,g ), fbm( p + 4.0*q + vec2(8.3+time,2.8+time) ,oc,l,g));
    return fbm( p + 4.0*r ,oc,l,g);    
}
 
float pattern2( in vec2 p, out vec2 q, out vec2 r , in float _time)
{
    float l = 2.3;
    float g = 0.4;
    int oc = 10; 
     
    q.x = fbm( p + vec2(1.,1.),oc,l,g);
    q.y = fbm( p + vec2(5.2,1.3) ,oc,l,g);
     
    r.x = fbm( p + 4.0*q + vec2(1.7,9.2),oc,l,g );
    r.y = fbm( p + 4.0*q + vec2(8.3,2.8) ,oc,l,g);
     
    return fbm( p + 4.0*r ,oc,l,g);
}

void main( void ) {
	
	p = SCALE*(gl_FragCoord.xy-0.5*resolution)/ resolution.y ;
	r = length(p);
	a = angle(vec2(0.));
	float inCircle = step(r,radius);
	
	vec2 y = vec2(0.);

	y.x += circle(radius);
	y.x += ticks() * inCircle;
	
	if(is_inside(p.x,p.y,radius)){
	  float col = pattern(p);
	  y += vec2(col+0.15, col+0.55);
	}
	
	y.y += hands(50.);
	//y.y += chrono(80., vec2(0.15,-0.12),1.); 
	//y = pow(y, vec2(0.9));
	
	vec3 rgb = y.x*color1+y.y*color2;
	
	//rgb = 0.8*mix(rgb, rgb.gbr+rgb.brg, 0.15);

	rgb = vec3(rgb.x*rgb.z, rgb.y*rgb.z, 0.10);
	
	gl_FragColor = vec4(rgb, 1.0);
	
}
