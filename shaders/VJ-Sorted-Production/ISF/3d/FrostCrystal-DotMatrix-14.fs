/*{
    "DESCRIPTION": "FrostCrystal-DotMatrix-14",
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
// http://glslsandbox.com/e#29907.0
// SPIDER

// Verlet Spider. By David Hoskins - 2015
// License Creative Commons Attribution-NonCommercial-ShareAlike 3.0 Unported License.
// It uses Verlet Integration to place the 'knees' correctly.

// From:- https://www.shadertoy.com/view/ltjXzt

// Beautiful, and beautiful solution. 
// I'm getting a bit of banding on the body (shadow bug, a visualization, or something to do with the verlet?)
// Please do spring mass physics next. =) -Sphinx

#ifdef GL_ES
precision mediump float;
#endif

#define MOD3 vec3(.1031,.11369,.13787)

struct SPID_LEGS
{
    vec3 point;
    vec3 knee;
    vec3 fix;
};
    
SPID_LEGS spiderLegs[8];
float gTime = time*.7+11.;
vec3 body = vec3(mouse.x);
vec2 add = vec2(1.0, mouse.y);

//----------------------------------------------------------------------------------------
float hash11(float p)
{
	vec3 p3  = fract(vec3(p) * MOD3);
    p3 += dot(p3, p3.yzx + 19.19);
    return fract((p3.x + p3.y) * p3.z);
}
float hash12(vec2 p)
{
	vec3 p3  = fract(vec3(p.xyx) * MOD3);
    p3 += dot(p3, p3.yzx + 19.19);
    return fract((p3.x + p3.y) * p3.z);
}
float noise11(float n)
{
    float f = fract(n);
     f = f*f*(3.0-2.0*f);
    n = floor(n);
    return mix(hash11(n),  hash11(n+1.0), f);
}
float noise12( in vec2 x )
{
    vec2 p = floor(x);
    vec2 f = fract(x);
    f = f*f*(3.0-2.0*f);
    float n = mix(mix( hash12(p),          hash12(p + add.xy),f.x),
                  mix( hash12(p + add.yx), hash12(p + add.xx),f.x),f.y);
    return n;
}
//----------------------------------------------------------------------------------------
float  sphere(vec3 p, vec3 x, float s )
{
    return length(p-x)-s;
}

//----------------------------------------------------------------------------------------
float upperLeg( vec3 p, vec3 a, vec3 b, float r )
{
	vec3 pa = p-a, ba = b-a;
	float h = clamp( dot(pa,ba)/dot(ba,ba), 0.0, 1.0 );
	return length( pa - ba*h ) - r*(sin(h*2.14+.4));
}

//----------------------------------------------------------------------------------------
float lowerLeg(vec3 p,  vec3 a, vec3 b, float r1, float r2)
{
	vec3 pa = p - a;
	vec3 ba = b - a;
	float h = clamp( dot(pa,ba)/dot(ba,ba), 0.0, 1.0 );
	return length( pa - ba*h ) - r1 + r2*h*pow(sin(h*1.9), 4.0);
}

//----------------------------------------------------------------------------------------
float smoothMin( float a, float b, float k )
{
    
	float h = clamp(0.5 + 0.5*(b-a)/k, 0.0, 1.0 );
	return mix( b, a, h ) - k*h*(1.-h);
}

//----------------------------------------------------------------------------------------
// Map the distance estimation...
float mapDE(vec3 p)
{
    float d;
p.x = -abs(p.x);
    d = sphere(p, body+vec3(0.0, 0.25, -1.4), .9);
    d = smoothMin(d, sphere(p, body+vec3(0.0, 0., .5 ), .62), .9);
    vec3 p2 = (p);
    p2.x = abs(p2.x);
    d = min(d, sphere(p2, body+vec3(0.2, 0.4, .9 ), .1));
    d = min(d, sphere(p2, body+vec3(0.1, 0.44, .85), .1));
    for (int i = 0; i < 4; i++)
    {
        d = min(d, upperLeg(p, spiderLegs[i].fix, spiderLegs[i].knee, .3)); 
        d = min(d, lowerLeg(p, spiderLegs[i].knee, spiderLegs[i].point, .17, .13)); 
    }
    return d;
}

//----------------------------------------------------------------------------------------
// Map the colour material...
vec3 mapCE(vec3 p)
{
    // Default red...
    vec3 mat  = vec3(.07, 0.00, 0.001);

    float d = sphere(p, body+vec3(0.0, 0.25, -1.4), .9);

    vec3 p2 = (p);
    p2.x = abs(p2.x);
    // Eye balls...  
    if (sphere(p2, body+vec3(0.2, 0.40, .9 ), .1) < 0.018 || sphere(p2, body+vec3(0.1, 0.44, .85), .1) <0.018)
   		 mat  = vec3(.0, 0.00, 0.00);
    
    return mat;
}

//----------------------------------------------------------------------------------------
float translucency(vec3 p, vec3 nor)
{
    float d = max(mapDE(p-nor*2.), 0.0);
    return min(d*d*d, 3.3);
}

//----------------------------------------------------------------------------------------
float rayMarch(vec3 pos, vec3 dir)
{
    float d =  0.1;
    for (int i = 0; i < 50; i++)
    {
        vec3 p = pos + dir * d;
        float de = mapDE(p);
        if(de < 0.015 || d > 80.0) break;
        d += de;
    }

    return d;
}
//----------------------------------------------------------------------------------------
float shadow( in vec3 ro, in vec3 rd)
{
	float res = 1.0;
    float t = 0.1;
	float h;
	
    for (int i = 0; i < 10; i++)
	{
		h = mapDE( ro + rd*t );
		res = min(4.0*h / t, res);
		t += h+.1;
	}
    return max(res, 0.15);
}

//----------------------------------------------------------------------------------------
vec3 normal( in vec3 pos)
{
	vec2 eps = vec2(.01, 0.0);
	vec3 nor = vec3(
	    mapDE(pos+eps.xyy) - mapDE(pos-eps.xyy),
	    mapDE(pos+eps.yxy) - mapDE(pos-eps.yxy),
	    mapDE(pos+eps.yyx) - mapDE(pos-eps.yyx) );
	return normalize(nor);
}

//----------------------------------------------------------------------------------------
vec3 cameraLookAt(in vec2 uv, in vec3 cam, in vec3 tar)
{
	vec3 cw = normalize(tar-cam);
	vec3 cp = vec3(0.0,1.0,0.0);
	vec3 cu = normalize(cross(cw,cp));
	vec3 cv = normalize(cross(cu,cw));
	return normalize(-uv.x*cu + uv.y*cv +2.*cw );
}

//----------------------------------------------------------------------------------------
// Verlet integration. Only effects the second vector because there is always an anchor point...
void verlet (in vec3 anchor, inout vec3 knee, float len)
{

		vec3 delta = anchor-knee;
		float deltalength = length(delta);
		float diff = (-len / (deltalength + len)) + 0.5;
		delta = delta * diff;
    	knee += delta*2.0;
}

//----------------------------------------------------------------------------------------
void moveSpider()
{
    float t  = gTime+sin(noise11(gTime*.7)+gTime+4.0);
	body.z = 3.*mod(t*1.2, 12.0)-2.0;
    body.y = 1.5 + sin(noise11(gTime*.9)*6.28);

    for (int i = 0; i < 8 ; i++)
    {
	    float s = sign( float(i)-3.5 );
		float h = mod( float(7-i),4. )/4.0;
        
		float z = (body.z + h*4.+s*.66 )/3.0;
		float iz = floor(z);
		float fz = fract(z);
	    float az = smoothstep(.65,  1., fz);
        
        spiderLegs[i].point = spiderLegs[i].fix;
        spiderLegs[i].point.y += sin(az*3.141);
        spiderLegs[i].point.z +=  (iz*3.0 + az*3.0 -h * 4.) + (s<.0?1.5:0.);
        spiderLegs[i].fix = spiderLegs[i].fix*.12 + body - vec3(.0, .3, -0.2);
        spiderLegs[i].knee  = (spiderLegs[i].point+spiderLegs[i].fix)*.5;
        spiderLegs[i].knee.y+=1.3;

        // Iterate twice for stronger constraints..
	    for (int n = 0; n < 1; n++)
		{
            // Over exagerate the limbs size to increase the contraint effect,
            // without the need for too many iterations...
        	verlet(spiderLegs[i].fix, spiderLegs[i].knee, 2.4);
        	verlet(spiderLegs[i].point, spiderLegs[i].knee, 4.);
     	}
    }
}

vec3 getWood(vec2 p)
{
    p *= vec2(1.4, 10.0);
    vec3 mat = vec3(.5, .3, .2);
    float f = 0.0;
    float a = 1.;
    for (int i = 0; i < 4; i++)
    {
        f+= noise12(p.xy) * a;
        p.xy *= 2.;
        a*= .5;
    }
    
    return (0.4 + .4*sin( f + vec3(1.0,1.5,2.0) ) )* mat;
}

//----------------------------------------------------------------------------------------
void main( void ) 
{

    vec2 xy = (gl_FragCoord.xy / resolution.xy);
	vec2 uv = (xy-.5)*vec2( resolution.x / resolution.y, 1);
    
    // Set initial feet positions...
    spiderLegs[0].fix = vec3(-1.8,0.0, 3.5);
    spiderLegs[1].fix = vec3(-2.5,0.0, 1.4);
    spiderLegs[2].fix = vec3(-2.5, 0.0, -.9);
    spiderLegs[3].fix = vec3(-2.25, 0.0, -3.);
    
    spiderLegs[4].fix = vec3(1.8,0.0, 3.5);
    spiderLegs[5].fix = vec3(2.5, 0.0, 1.4);
    spiderLegs[6].fix = vec3(2.5, 0.0, -.9);
    spiderLegs[7].fix = vec3(2.25, 0.0, -3.);
    
        // Do the animation..
    moveSpider();

    vec3 pos = vec3(-10.0, 10.0, 15.0)+0.04*cos(gTime*vec3(2.4,2.5,2.1) );
    vec3 dir = cameraLookAt(uv, pos, vec3(1.0, 0.0, 1.0+body.z)+0.04*sin(gTime*vec3(2.7,2.1,2.4) ));
    vec3 col = vec3(0.5), mat;

    float d = rayMarch(pos, dir);
    vec3 nor, loc;
    float tra = 0.0;
    if (d < 80.0)
    {
        // Spider...
        loc = pos+dir*d;
        nor = normal(loc);
        mat = mapCE(loc);
        tra = translucency(loc, nor);

    }else    
    {
        
        // Floor...
        dir.y = min(dir.y, 0.0);
		d = (.0-pos.y) / dir.y;
        nor = vec3(0.0, 1.0, 0.0);
        loc = pos+dir*d;
		mat = getWood(loc.zx);//texture2D(iChannel0, loc.zx*.2).xyz;
        float f =  fract(loc.x*.14);
        mat = mix(mat, vec3(0.0), smoothstep(0., .025,f)*smoothstep(.05, .025, f)*.75);
    }
    vec3 sun = normalize(vec3(-4.5, 8.4, 7.)- loc);
    float sha = shadow(loc, sun);
    float spec = .7;
    if (dot(sun, nor) < .1) spec= 0.0;
    vec3 ref = reflect(sun, nor);
    col = (mat * (tra+max(dot(sun, nor), 0.0))+pow(max(dot(dir, ref), 0.0), 17.0)*spec) *sha;
    col += mat * abs(nor.y*nor.y*.1);
    
     col *= .4+.6*70.0*xy.x*xy.y*(1.0-xy.x)*(1.0-xy.y);

	gl_FragColor = vec4(sqrt(col),1.0);
}

