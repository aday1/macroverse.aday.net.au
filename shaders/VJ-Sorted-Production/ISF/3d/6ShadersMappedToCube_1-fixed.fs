/*{
    "DESCRIPTION": "6ShadersMappedToCube",
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
}*/#define time TIME




#define mouse vec2(0.0)
#define resolution RENDERSIZE
// 6 shaders mapped on the 6 faces of a cube without any buffer and using raymarching
//
// This is a recode from previous version https://www.shadertoy.com/view/ld2yDz
// which was using rasterization and I decided to redo it using raymarching.
// Using raymarching gives a lot more possibilities than rasterization.
//
// Coded because I love cubes (don't ask me why)
// and also love the retro amiga scene.
//
// The six shaders I love and I used here:
//
// Face 1: WWDC14 by capnslipp - https://www.shadertoy.com/view/XdfyRB
// Face 2: Plasma by Klk - https://www.shadertoy.com/view/XsVSzW
// Face 3: YaraGui by dila - https://www.shadertoy.com/view/ldlyWS
// Face 4: Combustible Voronoi by Shane - https://www.shadertoy.com/view/4tlSzl
// Face 5: Ring twister by Flyguy - https://www.shadertoy.com/view/Xt23z3
// Face 6: Glenz by myself - https://www.shadertoy.com/view/4lt3R7 
//  Himred  2017-Apr-18

#extension GL_OES_standard_derivatives : enable

#define PI 3.14159265359
#define NUM_FACES 4
#define IN_RADIUS 0.25
#define OUT_RADIUS 0.70
#define XSCROLL_SPEED -0.9
#define COLOR_1 0.50, 0.90, 0.95
#define COLOR_2 0.95, 0.60, 0.10
#define BORDER 0.01
#define BORDERCOLOR vec4(0.8,0.8,0.8,1.)

int id=0;
vec3 xcolor = vec3(0.2, 0.5, 1.0);
float tau = atan(1.0) * 8.0;
float aaSize = 0.0;
vec3 cubevec;

//float time = time;  // 	lazy gigatron

// ---------------- Glenz Shader Code -----------------------
vec3 calcSine(vec2 uv, float frequency, float amplitude, float shift, float offset, vec3 color, float width, float exponent)
{
    float y = sin(time * frequency + shift + uv.x) * amplitude + offset;
    float d = distance(y, uv.y);
    float scale = smoothstep(width, 0.0, distance(y, uv.y));
    return color * scale;
}

vec3 Bars(vec2 uv)
{
    //vec2 uv = f / resolution.xy;
    vec3 color = vec3(0.0);
    color += calcSine(uv, 2.0, 0.25, 0.0, 0.5, vec3(0.0, 0.0, 1.0), 0.1, 3.0);
    color += calcSine(uv, 2.6, 0.15, 0.2, 0.5, vec3(0.0, 1.0, 0.0), 0.1, 1.0);
    color += calcSine(uv, 0.9, 0.35, 0.4, 0.5, vec3(1.0, 0.0, 0.0), 0.1, 1.0);
    return color;
}

float rand (in vec2 uv) { return fract(sin(dot(uv,vec2(12.4124,48.4124)))*48512.41241); }
const vec2 O = vec2(0.,1.);
float noise (in vec2 uv) {
	vec2 b = floor(uv);
	return mix(mix(rand(b),rand(b+O.yx),.5),mix(rand(b+O),rand(b+O.yy),.5),.5);
}

#define DIR_RIGHT -1.
#define DIR_LEFT 1.
#define DIRECTION DIR_LEFT

#define LAYERS 5
#define SPEED 210.
#define SIZE 5.

vec3 Stars(vec3 q)
	
{
	float t=time;
	float stars = 0.;
	float fl, s;
	for (int layer = 0; layer < LAYERS; layer++) {
		fl = float(layer);
		s = (400.-fl*20.);
		stars += step(.1,pow(noise(mod(vec2(q.x*s + t*SPEED*DIRECTION - fl*100.,q.y*s),resolution.x)),18.)) * (fl/float(LAYERS));
	}
    
     return vec3(stars).xyz ;
}

vec3 Twister(vec3 p)
{
    float f = sin(time/3.)*1.45;
    float c = cos(f*p.y);
    float s = sin(f/2.*p.y);
    mat2  m = mat2(c,-s,s,c);
    return vec3(m*p.xz,p.y);
}

float Cube( vec3 p )
{
    p=Twister(p);
    cubevec.x = sin(time);
    cubevec.y = cos(time);
    mat2 m = mat2( cubevec.y, -cubevec.x, cubevec.x, cubevec.y );
    p.xy *= m;p.xy *= m;p.yz *= m;p.zx *= m;p.zx *= m;p.zx *= m;
    cubevec = p;
    return length(max(abs(p)-vec3(0.4),0.0))-0.08;
}

float Face( vec2 uv )
{
        uv.y = mod( uv.y, 1.0 );
        return ( ( uv.y < uv.x ) != ( 1.0 - uv.y < uv.x ) ) ? 1.0 : 0.0;
}

vec3 getNormal( in vec3 p )
{
    vec2 e = vec2(0.005, -0.005);
    return normalize(
        e.xyy * Cube(p + e.xyy) +
        e.yyx * Cube(p + e.yyx) +
        e.yxy * Cube(p + e.yxy) +
        e.xxx * Cube(p + e.xxx));
}

vec4 Glenz(in vec2 uv )
{
    if(uv.x<BORDER || uv.y<BORDER || uv.x>1.-BORDER || uv.y>1.-BORDER) return BORDERCOLOR;
    float pat = time*5.0;
    float Step = 1.0;
    float Distance = 0.0;
    float Near = -1.0;
    float Far = -1.0;
    vec3 lightPos = vec3(1.5, 0, 0);
    vec2 p = -1.0 + uv *2.0;
    vec2 kp=uv;
    vec4 m =  resolution.xxxx;
    float hd=-1.;
    
    vec3 ro = vec3( 0.0, 0.0, 2.1 );
    vec3 rd = normalize( vec3( p, -2. ) );
    for( int i = 0; i < 256; i++ )
        {
        	Step = Cube( ro + rd*Distance );
            Distance += Step*.5;

            if( Distance > 4.0 ) break;
            if( Step < 0.001 )
            	{
                 	Far = Face( cubevec.yx ) + Face( -cubevec.yx ) + Face( cubevec.xz ) + Face( -cubevec.xz ) + Face( cubevec.zy ) + Face( -cubevec.zy );
            		if(hd<0.) hd=Distance;
                    if( Near < 0.0 ) Near = Far;
            		if(m.z<=0.0) Distance += 0.05; else break; // 0.05 is a magic number 
                }
        }

    vec3 Color=Bars(uv);
    if( Near > 0.0 )
    	{
            vec3 sp = ro + rd*hd;
        	vec3 ld = lightPos - sp;
            float lDist = max(length(ld), 0.001);
            ld /= lDist;
            float atten = 1./(1. + lDist*.2 + lDist*.1); 
            float ambience = 0.7;
            vec3 sn = getNormal( sp);
            float diff = min(0.3,max( dot(sn, ld), 0.0));
            float spec = pow(max( dot( reflect(-ld, sn), -rd ), 0.0 ), 32.);
            if(m.z<=0.) Color = Color/5. + mix( vec3( 0.2, 0.0, 1.0 ), vec3( 1.0, 1.0, 1.0 ), vec3( ( Near*0.45 + Far*Far*0.04 ) ) );
            else Color = mix( vec3( 0.2, 0.0, 1.0 ), vec3( 1.0, 1.0, 1.0 ), vec3( ( Near*0.45 + Far*Far*0.04 ) ) );
            Color = Color*(diff+ambience)+vec3(0.78,0.5,1.)*spec/1.5;
        }
    return vec4( Color, 1.0 );
}

// ---------------- Ring Shader Code -----------------------
vec4 slice(float x0, float x1, vec2 uv)
{
    float u = (uv.x - x0)/(x1 - x0);
    float w = (x1 - x0);
    vec3 col = vec3(0);
    col = mix(vec3(COLOR_1), vec3(COLOR_2), u);
    col *= w / sqrt(2.0 * IN_RADIUS*IN_RADIUS * (1.0 - cos(tau / float(NUM_FACES))));
    col *= smoothstep(0.05, 0.10, u) * smoothstep(0.95, 0.90, u) + 0.5;
    uv.y += time * XSCROLL_SPEED; //Scrolling
    col *= (-1.0 + 2.0 * smoothstep(-0.03, 0.03, sin(u*PI*4.0) * cos(uv.y*16.0))) * (1.0/16.0) + 0.7;
    float clip = 0.0;
    clip = (1.0-smoothstep(0.5 - aaSize/w, 0.5 + aaSize/w, abs(u - 0.5))) * step(x0, x1);
    return vec4(col, clip);
}

vec4 Ring(in vec2 uv)
{
    if(uv.x<BORDER || uv.y<BORDER || uv.x>1.-BORDER || uv.y>1.-BORDER) return BORDERCOLOR;
    aaSize = 2.0 / resolution.y;
    uv = uv * 2.0 - 1.0;
    vec2 uvr = vec2(length(uv), atan(uv.y, uv.x) + PI);
    uvr.x -= OUT_RADIUS;
    vec3 col = vec3(0.05);
    float angle = uvr.y + 2.0*time + sin(uvr.y) * sin(time) * PI;
    
    for(int i = 0;i < NUM_FACES;i++)
    {
        float x0 = IN_RADIUS * sin(angle + tau * (float(i) / float(NUM_FACES)));
        float x1 = IN_RADIUS * sin(angle + tau * (float(i + 1) / float(NUM_FACES)));
        vec4 face = slice(x0, x1, uvr);
        col = mix(col, face.rgb, face.a); 
    }
	return vec4(col, 1.0);
}

// ---------------- Voronoi Shader Code -----------------------
vec3 firePalette(float i){

    float T = 1400. + 1300.*i;
    vec3 L = vec3(7.4, 5.6, 4.4);
    L = pow(L,vec3(5.0)) * (exp(1.43876719683e5/(T*L))-1.0);
    return 1.0-exp(-5e8/L);
}

vec3 hash33(vec3 p){ 
    
    float n = sin(dot(p, vec3(7, 157, 113)));    
    return fract(vec3(2097152, 262144, 32768)*n); 
}

float xvoronoi(vec3 p){

	vec3 b, r, g = floor(p);
	p = fract(p);
	float d = 1.; 
	for(int j = -1; j <= 1; j++) {
	    for(int i = -1; i <= 1; i++) {
    		
		    b = vec3(i, j, -1);
		    r = b - p + hash33(g+b);
		    d = min(d, dot(r,r));
    		
		    b.z = 0.0;
		    r = b - p + hash33(g+b);
		    d = min(d, dot(r,r));
    		
		    b.z = 1.;
		    r = b - p + hash33(g+b);
		    d = min(d, dot(r,r));
    			
	    }
	}
	
	return d;
}

float noiseLayers(in vec3 p) {
    vec3 t = vec3(0., 0., p.z+time*1.5);

    const int iter = 5;
    float tot = 0., sum = 0., amp = 1.;

    for (int i = 0; i < iter; i++) {
        tot += xvoronoi(p + t) * amp;
        p *= 2.0;
        t *= 1.5;
        sum += amp;
        amp *= 0.5;
    }
    return tot/sum;
}

// ---------------- Plasma Shader Code -----------------------
vec4 Plasma(vec2 uv )
{
    if(uv.x<BORDER || uv.y<BORDER || uv.x>1.-BORDER || uv.y>1.-BORDER) return BORDERCOLOR;
	float localTime =time*4.0;
	uv = (uv-0.0)*6.0;
    vec2 uv0=uv;
	float i0=1.0;
	float i1=1.0;
	float i2=1.0;
	float i4=0.0;
	for(int s=0;s<7;s++)
	{
		vec2 r;
		r=vec2(cos(uv.y*i0-i4+localTime/i1),sin(uv.x*i0-i4+localTime/i1))/i2;
        r+=vec2(-r.y,r.x)*0.3;
		uv.xy+=r;
        
		i0*=1.93;
		i1*=1.15;
		i2*=1.7;
		i4+=0.05+0.1*localTime*i1;
	}
    float r=sin(uv.x-localTime)*0.5+0.5;
    float b=sin(uv.y+localTime)*0.5+0.5;
    float g=sin((uv.x+uv.y+sin(localTime*0.5))*0.5)*0.5+0.5;
	return vec4(r,g,b,1.0);
}

// ---------------- Twirl Shader Code -----------------------
vec4 tw(vec2 uv)
{
    float j = sin(uv.y * 3.14 + time * 5.0);
    float i = sin(uv.x * 15.0 - uv.y * 2.0 * 3.14 + time * 3.0);
    float n = -clamp(i, -0.2, 0.0) - 0.0 * clamp(j, -0.2, 0.0);
    return 3.5 * (vec4(xcolor, 1.0) * n);
}

vec4 Twirl(vec2 p)
{
    if(p.x<BORDER || p.y<BORDER || p.x>1.-BORDER || p.y>1.-BORDER) return BORDERCOLOR;
    vec2 uv;
    p=-1.+2.*p;
    
    float r = sqrt(dot(p, p));
    float a = atan(
        p.y * (0.3 + 0.1 * cos(time * 2.0 + p.y)),
        p.x * (0.3 + 0.1 * sin(time + p.x))
    ) + time;
    
    uv.x = time + 1.0 / (r + .01);
    uv.y = 4.0 * a / 3.1416;
    
    return mix(vec4(0.0), tw(uv) * r * r * 2.0, 1.0);
}

// ---------------- Room Shader Code -----------------------
float sdBoxXY( vec3 p, vec3 b )
{
  vec2 d = abs(p.xy) - b.xy;
  return min(max(d.x,d.y),0.0) + length(max(d,0.0));
}

float udRoundBox( vec3 p, vec3 b, float r )
{
  return length(max(abs(p)-b,0.0))-r;
}

float smin( float a, float b, float k )
{
    float res = exp( -k*a ) + exp( -k*b );
    return -log( res )/k;
}

mat2 rot(float x) {
	return mat2(cos(x), sin(x), -sin(x), cos(x));
}

float map(vec3 p) {
    float k = 0.5 * 2.0;
	vec3 q = (fract((p - vec3(0.25, 0.0, 0.25))/ k) - 0.5) * k;
    vec3 s = vec3(q.x, p.y, q.z);
    float d = udRoundBox(s, vec3(0.1, 1.0, 0.1), 0.05);
    
    k = 0.5;
    q = (fract(p / k) - 0.5) * k;
    s = vec3(q.x, abs(p.y) - 1.5, q.z);
    float g = udRoundBox(s, vec3(0.17, 0.5, 0.17), 0.2);
    
    float sq = sqrt(0.5);
    vec3 u = p;
    u.xz *= mat2(sq, sq, -sq, sq);
    d = max(d, -sdBoxXY(u, vec3(0.8, 1.0, 0.8)));
    
    return smin(d, g, 16.0);
}

vec3 normal(vec3 p)
{
	vec3 o = vec3(0.001, 0.0, 0.0);
    return normalize(vec3(map(p+o.xyy) - map(p-o.xyy),
                          map(p+o.yxy) - map(p-o.yxy),
                          map(p+o.yyx) - map(p-o.yyx)));
}

float trace(vec3 o, vec3 r) {
    float t = 0.0;
    for (int i = 0; i < 32; ++i) {
        t += map(o + r * t);
    }
    return t;
}

vec4 Room(vec2 uv )
{
    if(uv.x<BORDER || uv.y<BORDER || uv.x>1.-BORDER || uv.y>1.-BORDER) return BORDERCOLOR;
    uv = uv * 2.0 - 1.0;
    uv.x *= resolution.x / resolution.y;
    
    float gt = time / 5.0;
    vec3 r = normalize(vec3(uv, 1.7 - dot(uv, uv) * 0.1));
    float sgt = sin(gt * 3.141592 * 2.0);
    r.xy *= rot(sgt * 3.141592 / 8.0);
    r.xz *= rot(3.141592 * 0.0 + gt * 3.141592 * 2.0);
    r.xz *= rot(3.141592 * -0.25);

    vec3 o = vec3(0.0, 0.0, gt * 5.0 * sqrt(2.0) * 2.0);
    o.xz *= rot(3.141592 * -0.25);

    float t = trace(o, r);
    vec3 w = o + r * t;
    vec3 sn = normal(w);
    float fd = map(w);

    vec3 col = vec3(0.514, 0.851, 0.933) * 0.5;
    vec3 ldir = normalize(vec3(-1, -0.5, 1.1));

    float fog = 1.0 / (1.0 + t * t * 0.1 + fd * 100.0);
    float front = max(dot(r, -sn), 0.0);
    float ref = max(dot(r, reflect(-ldir, sn)), 0.0);
    float grn = pow(abs(sn.y), 3.0);

    vec3 cl = vec3(grn);
    cl += mix(col*vec3(1.5), vec3(0.25), grn) * pow(ref, 16.0);
    cl = mix(col, cl, fog);

	return vec4(cl, 1.0);
}

// ---------------- Main Shader Code -----------------------
vec3 Face(vec3 p,vec3 n,int f )
{    
    n = max(abs(n) - 0.2, 0.001);
    p+=0.5;
    n /= (n.x + n.y + n.z ); 

	if(f==1) return Stars(p.yzx )*n.x ;
    if(f==2) return (Ring(p.yz)*n.x + Ring(p.zx)*n.y + Ring(p.xy)*n.z).xyz;
    if(f==3) return (Glenz(p.yz)*n.x + Glenz(p.zx)*n.y + Glenz(p.xy)*n.z).xyz;
    if(f==4) return (Plasma(p.yz)*n.x + Plasma(p.zx)*n.y + Plasma(p.xy)*n.z).xyz;
    if(f==5) return (Twirl(p.yz)*n.x + Twirl(p.zx)*n.y + Twirl(p.xy)*n.z).xyz;
    if(f==6) return (Room(p.yz)*n.x + Room(p.zx)*n.y + Room(p.xy)*n.z).xyz;
}

mat2 Rotate(float angle)
{
	return mat2(cos(angle), sin(angle), -sin(angle), cos(angle));   
}

float Scene(vec3 p)
{ 
    return max(max(abs(p.x), abs(p.y)), abs(p.z)) - 5.;
}

vec3 Normal(vec3 p)
{
	vec3 o = vec3(0.01, 0, 0);
    return normalize(vec3(Scene(p-o.xyz)-Scene(p+o.xyz),Scene(p-o.zxy)-Scene(p+o.zxy),Scene(p-o.yzx)-Scene(p+o.yzx)));
}

vec3 RayMarch(vec3 ro,vec3 rd)
{
    float hd = 0.0;
    id=0;
    for(int i = 0;i < 128;i++)
    {
        float d = Scene(ro + rd * hd);
        hd += d;
        if(d < 0.0001) {id=1; break;}
    }   
    return ro + rd * hd;
}

vec3 GetColor(vec3 p, vec3 n)
{   
    p/=10.;
    if(dot(n,vec3(1,0,0))>0.) return Face(p,n,1);
    if(dot(n,vec3(1,0,0))<0.) return Face(p,n,2);
    if(dot(n,vec3(0,0,1))>0.) return Face(p,n,3);
    if(dot(n,vec3(0,0,1))<0.) return Face(p,n,4);
    if(dot(n,vec3(0,1,0))>0.) return Face(p,n,5);
    if(dot(n,vec3(0,1,0))<0.) return Face(p,n,6);
    return vec3(1);
}

vec3 Lightning(vec3 sp,vec3 sn,vec3 rd,vec3 lp,vec3 color,vec3 lc)
{
    // From a Shane Shader
    vec3 ld = lp - sp;
    float lDist = max(length(ld), 0.001);
    ld /= lDist;
    float atten = 1./(1. + lDist*.2 + lDist*.1); 
    float ambience = 1.;
    float diff = min(0.3,max( dot(sn, ld), 0.0));
    float spec = pow(max( dot( reflect(-ld, sn), -rd ), 0.0 ), 32.); 
    return color*(diff+ambience)+lc*spec/1.5;
}

void main()
{
    vec2 ratio = resolution.xy / resolution.y;
	vec2 uv = gl_FragCoord.xy / resolution.y;
    
    vec3 ro = vec3(0 , 0, -20.0);
    vec3 rd = normalize(vec3(uv - ratio / 2.0, 1.0));
    
    mat2 rx = Rotate(time*0.6);
    mat2 ry = Rotate(time*0.4);
    
    ro.yz *= rx; ro.xz *= ry;
    rd.yz *= rx; rd.xz *= ry;
    
    vec3 sp = RayMarch(ro, rd);
    vec3 sn = Normal(sp);
    vec3 color = vec3(0);
    
    if(id==1)
    	{
    	color = GetColor(sp,sn);
    	color=Lightning(sp,sn,rd,ro-vec3(5),color,vec3(1,1,1));
        }
    
	gl_FragColor = vec4(color, 1.0);
}
