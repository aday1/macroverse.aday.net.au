/*{
    "DESCRIPTION": "RaymarchPrimitive-Fire-FrostCrystal-3",
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
        "3d",
        "texture-input"
    ]
}*/#define iResolution vec3(RENDERSIZE, 1.0)
#define iGlobalTime TIME





#define mouse vec2(0.0)
#define resolution RENDERSIZE
// Red screen test shader
/*void main(void) {
    gl_FragColor = vec4(1.0, 0.0, 0.0, 1.0);

}*/

/*
//
// Shadertoy example 1
//
//
// https://www.shadertoy.com/view/MdfSzn
//
// MetaTunnel
//
// Created by Anatole Duprat - XT95/2014 - http://www.aduprat.com/portfolio/?page=shaders
// License Creative Commons Attribution-NonCommercial-ShareAlike 3.0 Unported License.
//
// http://www.pouet.net/prod.php?which=52777
// By Frequency - http://www.frequency.fr/
// 1st place at Numerica Artparty in march 2009 in France
//

float time = iGlobalTime*.5;

const float s=0.4; //Density threshold

// The scene define by density
float obj(vec3 p)
{
    float d = 1.0;
    d *= distance(p, vec3(cos(time)+sin(time*0.2),0.3,2.0+cos(time*0.5)*0.5) );
    d  = distance(p,vec3(-cos(time*0.7),0.3,2.0+sin(time*0.5)));
    d *= distance(p,vec3(-sin(time*0.2)*0.5,sin(time),2.0));
    d *=cos(p.y)*cos(p.x)-0.1-cos(p.z*7.+time*7.)*cos(p.x*3.)*cos(p.y*4.)*0.1;
    return d;
}

void main()
{
    vec2 q = gl_FragCoord.xy/iResolution.xy;
    vec2 v = -1.0+2.0*q;
    v.x *= iResolution.x/iResolution.y*.5+.5;
	
    vec3 o = vec3(v.x,v.y,0.0);
    vec3 d = normalize(vec3(v.x+cos(time)*.3,v.y,1.0))/64.0;
	
    vec3 color = vec3(0.0);
    float t = 0.0;
    bool hit = false;
	
    for(int i=0; i<100; i++) {
        if(!hit) {
            if(obj(o+d*t) < s) {
                t-=5.0;
                for(int j=0; j<5; j++)
                    if(obj(o+d*t) > s)
                        t+=1.0;
                vec3 e=vec3(0.01,.0,.0);
                vec3 n=vec3(0.0);
                n.x=obj(o+d*t)-obj(vec3(o+d*t+e.xyy));
                n.y=obj(o+d*t)-obj(vec3(o+d*t+e.yxy));
                n.z=obj(o+d*t)-obj(vec3(o+d*t+e.yyx));
                n = normalize(n);
                color = vec3(1.) * max(dot(vec3(0.0,0.0,-0.5),n),0.0)+max(dot(vec3(0.0,- .5,0.5),n),0.0)*0.5;
                hit=true;
            }
            t+=5.0;
        }
    }

    gl_FragColor= vec4(color,1.)+vec4(0.1,0.2,0.5,1.0)*(t*0.025);

}
*/

/*
//
// Shadertoy example 2 - needs a texture input..
//
// https://www.shadertoy.com/view/Xsl3zn
//
//
// Warping Texture
//
// Created by inigo quilez - iq/2013
// License Creative Commons Attribution-NonCommercial-ShareAlike 3.0 Unported License.
//
                                      
void main(void)
{
	vec2 uv = 0.5*gl_FragCoord.xy / iResolution.xy;

	float d = length(uv);
    vec2 st = uv*0.1 + 0.2*vec2(cos(0.071*iGlobalTime+d), sin(0.073*iGlobalTime-d));

    vec3 col = texture2D( iChannel0, st ).xyz;
    float w = col.x;

    col *= 1.0 - texture2D( iChannel0, 0.4*uv + 0.1*col.xy  ).xyy;
    col *= w*2.0;
    
    col *= 1.0 + 2.0*d;
    gl_FragColor = vec4(col,1.0);

}
*/

/*
//
// Shadertoy example 3
//
//
// 'Menger Sponge' by iq (2010)
//
// https://www.shadertoy.com/view/4sX3Rn?
//
// Created by inigo quilez
// License Creative Commons Attribution-NonCommercial-ShareAlike 3.0 Unported License.
//
// http://www.iquilezles.org/apps/shadertoy/index2.html
//
// Three iterations of the famous fractal structure (pretty much inspired by untraceable/TBC).
// See http://www.iquilezles.org/articles/menger/menger.htm for the full explanation of how this was done
//

                                      /*
float maxcomp(in vec3 p ) { return max(p.x,max(p.y,p.z));}
float sdBox( vec3 p, vec3 b )
{
  vec3  di = abs(p) - b;
  float mc = maxcomp(di);
  return min(mc,length(max(di,0.0)));
}

mat3 ma = mat3( 0.60, 0.00,  0.80,
                0.00, 1.00,  0.00,
               -0.80, 0.00,  0.60 );

vec4 map( in vec3 p )
{
    float d = sdBox(p,vec3(1.0));
    vec4 res = vec4( d, 1.0, 0.0, 0.0 );

    float ani = smoothstep( -0.2, 0.2, -cos(0.5*iGlobalTime) );
	float off = 1.5*sin( 0.01*iGlobalTime );
	
    float s = 1.0;
    for( int m=0; m<4; m++ )
    {

        p = mix( p, ma*(p+off), ani );
	   
        vec3 a = mod( p*s, 2.0 )-1.0;
        s *= 3.0;
        vec3 r = abs(1.0 - 3.0*abs(a));
        float da = max(r.x,r.y);
        float db = max(r.y,r.z);
        float dc = max(r.z,r.x);
        float c = (min(da,min(db,dc))-1.0)/s;

        if( c>d )
        {
          d = c;
          res = vec4( d, min(res.y,0.2*da*db*dc), (1.0+float(m))/4.0, 0.0 );
        }
    }

    return res;
}

vec4 intersect( in vec3 ro, in vec3 rd )
{
    float t = 0.0;
    vec4 res = vec4(-1.0);
	vec4 h = vec4(1.0);
    for( int i=0; i<64; i++ )
    {
		if( h.x<0.002 || t>10.0 ) break;
        h = map(ro + rd*t);
        res = vec4(t,h.yzw);
        t += h.x;
    }
	if( t>10.0 ) res=vec4(-1.0);
    return res;
}

float softshadow( in vec3 ro, in vec3 rd, float mint, float k )
{
    float res = 1.0;
    float t = mint;
	float h = 1.0;
    for( int i=0; i<32; i++ )
    {
        h = map(ro + rd*t).x;
        res = min( res, k*h/t );
		t += clamp( h, 0.005, 0.1 );
    }
    return clamp(res,0.0,1.0);
}

vec3 calcNormal(in vec3 pos)
{
    vec3  eps = vec3(.001,0.0,0.0);
    vec3 nor;
    nor.x = map(pos+eps.xyy).x - map(pos-eps.xyy).x;
    nor.y = map(pos+eps.yxy).x - map(pos-eps.yxy).x;
    nor.z = map(pos+eps.yyx).x - map(pos-eps.yyx).x;
    return normalize(nor);
}

void main(void)
{
    vec2 p = -1.0 + 2.0 * gl_FragCoord.xy / iResolution.xy;
    p.x *= 1.33;

    // light
    vec3 light = normalize(vec3(1.0,0.9,0.3));

    float ctime = iGlobalTime;
    // camera
    vec3 ro = 1.1*vec3(2.5*sin(0.25*ctime),1.0+1.0*cos(ctime*.13),2.5*cos(0.25*ctime));
    vec3 ww = normalize(vec3(0.0) - ro);
    vec3 uu = normalize(cross( vec3(0.0,1.0,0.0), ww ));
    vec3 vv = normalize(cross(ww,uu));
    vec3 rd = normalize( p.x*uu + p.y*vv + 2.5*ww );

    // background color
    vec3 col = mix( vec3(0.3,0.2,0.1)*0.5, vec3(0.7, 0.9, 1.0), 0.5 + 0.5*rd.y );
	
    vec4 tmat = intersect(ro,rd);
    if( tmat.x>0.0 )
    {
        vec3  pos = ro + tmat.x*rd;
        vec3  nor = calcNormal(pos);
		
        float occ = tmat.y;
		float sha = softshadow( pos, light, 0.01, 64.0 );

		float dif = max(0.1 + 0.9*dot(nor,light),0.0);
		float sky = 0.5 + 0.5*nor.y;
        float bac = max(0.4 + 0.6*dot(nor,vec3(-light.x,light.y,-light.z)),0.0);

        vec3 lin  = vec3(0.0);
        lin += 1.00*dif*vec3(1.10,0.85,0.60)*sha;
        lin += 0.50*sky*vec3(0.10,0.20,0.40)*occ;
        lin += 0.10*bac*vec3(1.00,1.00,1.00)*(0.5+0.5*occ);
        lin += 0.25*occ*vec3(0.15,0.17,0.20);	 

        vec3 matcol = vec3(
            0.5+0.5*cos(0.0+2.0*tmat.z),
            0.5+0.5*cos(1.0+2.0*tmat.z),
            0.5+0.5*cos(2.0+2.0*tmat.z) );
        col = matcol * lin;
    }

    col = pow( col, vec3(0.4545) );

    gl_FragColor = vec4(col,1.0);
}
*/

//
// Shadertoy example 4
//
// LJ - Example of revised specification using "mainImage" instead of "main"
// A fix can be made to include a main function right here, but it is included
// before compilation in "LoadShader" to be consistent with "ShaderLoader"
//
// https://www.shadertoy.com/view/ldl3W8#
//
// Voronoi - distances
//
// Created by inigo quilez - iq/2013
// License Creative Commons Attribution-NonCommercial-ShareAlike 3.0 Unported License.

// I've not seen anybody out there computing correct cell interior distances for Voronoi
// patterns yet. That's why they cannot shade the cell interior correctly, and why you've
// never seen cell boundaries rendered correctly. 

// However, here's how you do mathematically correct distances (note the equidistant and non
// degenerated grey isolines inside the cells) and hence edges (in yellow):

// http://www.iquilezles.org/www/articles/voronoilines/voronoilines.htm

// LJ - must avoid hash charaters in the code when using stringify. They are OK in comments
// #define ANIMATE
bool bAnimate = true;

vec2 hash2( vec2 p )
{
    // texture based white noise
    // return texture2D( iChannel0, (p+0.5)/256.0, -100.0 ).xy;
	
    // procedural white noise	
    return fract(sin(vec2(dot(p, vec2(127.1, 311.7)), dot(p, vec2(269.5, 183.3))))*43758.5453);
}

vec3 voronoi( in vec2 x )
{
    vec2 n = floor(x);
    vec2 f = fract(x);

    //----------------------------------
    // first pass: regular voronoi
    //----------------------------------
	// LJ - stringify does not like the comma here !
	// Declare variables separately
	// vec2 mg, mr;
	vec2 mg;
	vec2 mr;

    float md = 8.0;

    for( int j=-1; j<=1; j++ ) {
        for( int i=-1; i<=1; i++ ) {
            vec2 g = vec2(float(i),float(j));
            vec2 o = hash2( n + g );
			// LJ avoid the hash character here
			// #ifdef ANIMATE
			if(bAnimate)
			    o = 0.5 + 0.5*sin( iGlobalTime + 6.2831*o );
			// #endif
            vec2 r = g + o - f;
            float d = dot(r,r);

            if( d<md ) {
                md = d;
                mr = r;
                mg = g;
            }
        }
	}

    //----------------------------------
    // second pass: distance to borders
    //----------------------------------
    md = 8.0;
    for( int j=-2; j<=2; j++ ) {
        for( int i=-2; i<=2; i++ ) {
            vec2 g = mg + vec2(float(i),float(j));
            vec2 o = hash2( n + g );
			// LJ avoid the hash character here
			// #ifdef ANIMATE
			if(bAnimate)
				o = 0.5 + 0.5*sin( iGlobalTime + 6.2831*o );
			// #endif
            vec2 r = g + o - f;

            if( dot(mr-r,mr-r)>0.00001 )
                md = min( md, dot( 0.5*(mr+r), normalize(r-mr) ) );
        }
	}

    return vec3( md, mr );
}

void mainImage( out vec4 fragColor, in vec2 fragCoord )
{
    vec2 p = fragCoord.xy/iResolution.xx;

    vec3 c = voronoi( 8.0*p );

    // isolines
    vec3 col = c.x*(0.5 + 0.5*sin(64.0*c.x))*vec3(1.0);
    
    // borders	
    col = mix( vec3(1.0,0.6,0.0), col, smoothstep( 0.04, 0.07, c.x ) );
    
    // feature points
    float dd = length( c.yz );
    col = mix( vec3(1.0,0.6,0.1), col, smoothstep( 0.0, 0.12, dd) );
    col += vec3(1.0,0.6,0.1)*(1.0-smoothstep( 0.0, 0.04, dd));

    fragColor = vec4(col,1.0);
}

/*
//
// GLSL Sandbox example 1
//
//
//	Duelling Mandelbulbs
//
// http://glsl.herokuapp.com/e#1293.0
// http://www.thealphablenders.com/
// 2012 by Andrew Caudwell
//

// This has to be removed for stringify
// #ifdef GL_ES
// precision mediump float;
// #endif

struct Ray {
   vec3 o;
   vec3 d;
};

// dueling mandelbulbs
// @acaudwell

// http://www.fractalforums.com/mandelbulb-implementation/realtime-renderingoptimisations/

float mandelbulb(in vec3 p, float power) {
	
    float dr = 1.0;
    float r  = length(p);

    vec3 c = p;
	
    for(int i=0; i<2; i++) {
		    
        float zo0 = asin(p.z / r);
        float zi0 = atan(p.y, p.x);
        float zr  = pow(r, power-1.0);
        float zo  = (zo0) * power;
        float zi  = (zi0) * power;
        float czo = cos(zo);

        dr = zr * dr * power + 1.0;
        zr *= r;

        p = zr * vec3(czo*cos(zi), czo*sin(zi), sin(zo));
	        
	p += c;
	    
        r = length(p);
    }

    return 0.5 * r * log(r) / r;	
}

void main() {

    vec2 p = ((gl_FragCoord.xy / resolution.xy) * 2.0 - 1.0) * 3.5;
	
    float t = time;

    Ray ray1;
    ray1.o = vec3(0.0);
    ray1.d = normalize( vec3((p - 1.5*vec2(sin(t-2.0), cos(t+1.0))) * vec2(resolution.x/resolution.y, 1.0), 1.0 ) );

    Ray ray2;
    ray2.o = vec3(0.0);
    ray2.d = normalize( vec3((p - 1.5*vec2(cos(-t),sin(t))) * vec2(resolution.x/resolution.y, 1.0), 1.0 ) );
		
    ray1.d.xy = vec2( ray1.d.x * cos(t) - ray1.d.y * sin(t), ray1.d.x * sin(t) + ray1.d.y * cos(t)); 
    ray2.d.xy = vec2( ray2.d.x * cos(t) - ray2.d.y * sin(t), ray2.d.x * sin(t) + ray2.d.y * cos(t)); 
	
    float m1 =  mandelbulb(ray1.o + ray1.d, abs(cos(t)*13.0));
    float m2 =  mandelbulb(ray2.o + ray2.d, abs(sin(t)*13.0));
	
    float f = pow(max(m1,m2) , abs(m1-m2));
    vec3  c = m1 > m2 ? vec3(0.0, 0.05, 0.2) : vec3(0.2, 0.05, 0.0);
	
    gl_FragColor = vec4(c*f, 1.0);
}*/

/*
//
// GLSL Sandbox example 2
//
//
//	Relief tunnel
//
// http://glsl.herokuapp.com/e#3259.0
//

// This has to be removed for stringify
// #ifdef GL_ES
// precision mediump float;
// #endif

//gt

void main( void ) {
	
	vec2 p = -1.0 + 2.0 * gl_FragCoord.xy / resolution.xy;
   	vec2 uv;

	//shadertoy deform "relief tunnel"-gt
   	float r = sqrt( dot(p,p) );
   	float a = atan(p.y,p.x) + 0.9*sin(0.5*r-0.5*time);

	float s = 0.5 + 0.5*cos(7.0*a);
   	s = smoothstep(0.0,1.0,s);
   	s = smoothstep(0.0,1.0,s);
   	s = smoothstep(0.0,1.0,s);
   	s = smoothstep(0.0,1.0,s);

   	uv.x = time + 1.0/( r + .2*s);
  	  //uv.y = 3.0*a/3.1416;
	uv.y = 1.0*a/10.1416;

    float w = (0.5 + 0.5*s)*r*r;
   	// vec3 col = texture2D(tex0,uv).xyz;
    float ao = 0.5 + 0.5*cos(42.0*a);//amp up the ao-g
   	ao = smoothstep(0.0,0.4,ao)-smoothstep(0.4,0.7,ao);
    	ao = 1.0-0.5*ao*r;

	//faux shaded texture-gt
	float px = gl_FragCoord.x/resolution.x;
	float py = gl_FragCoord.y/resolution.y;
	float x = mod(uv.x*resolution.x,resolution.x/3.5);
	float y = mod(uv.y*resolution.y+(resolution.y/2.),resolution.y/3.5);
	float v =  (x / y)-.7;

	gl_FragColor = vec4(vec3(.1-v,.9-v,1.-v)*w*ao,1.0);

}
*/

