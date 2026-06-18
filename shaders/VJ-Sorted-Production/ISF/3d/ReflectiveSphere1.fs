/*{
    "DESCRIPTION": "ReflectiveSphere1",
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
        }
    ],
    "TAGS": [
        "3d"
    ]
}*/



#define time ((useFrameIndex ? (float(FRAMEINDEX) / fps) : TIME) * timeScale)
#define mouse vec2(mouseX, mouseY)
#define resolution RENDERSIZE
#ifdef GL_ES
precision mediump float;
#endif

//heh, it's david bowie

//origional code for trinoise from ???

//original code for dust cloud from https://www.shadertoy.com/view/MtX3Ws
//simplified edit: Robert 25.11.2015
//compositing by sphinx

float tri(in float x){return abs(fract(x)-.5);}
vec3 tri3(in vec3 p){return vec3( tri(p.z+tri(p.y*1.)), tri(p.z+tri(p.x*1.)), tri(p.y+tri(p.x*1.)));}

mat2 rmat(float r)
{
    float c = cos(r);
    float s = sin(r);
    return mat2(c, s, -s, c);
}

float map_dust(in vec3 p) 
{
	vec3	o = p;
	p 		*= .25;

	float res 	= 1.;
	vec3 c 		= p;
	for (int i = 0; i < 4; i++) 
	{	
		p 	= fract(abs(p-.125)/dot(p,p));
		p	+= p.yzx-.5;
		res 	+= clamp(1./abs(16.*dot(p,c)), .001, res)/1.5;
	}

	return max(.00001, res);
}

vec3 hsv(float h,float s,float v)
{
	return mix(vec3(1.),clamp((abs(fract(h+vec3(3.,2.,1.)/3.)*6.-3.)-1.),0.,1.),s)*v;
}

vec3 dust()
{
	vec2 uv = gl_FragCoord.xy/resolution;
	uv	= (uv - .5) * resolution/min(resolution.x, resolution.y);
	
	uv.x 		+= .25;
	vec3 origin 	= vec3(.01, .05, 4.);
	origin.xz 	*= rmat(time * .0125);
	vec3 up 		= vec3(0.5, .75, .25);
	float fov 	= .125;
	vec3 u 		= normalize(cross(origin, up));
	vec3 v 		= normalize(cross(u,  origin));
	vec3 direction	= normalize(uv.x * u + uv.y * v - fov * origin);

	vec3 color = vec3(0);
	vec3 position 	= origin;
	float mass 	= 0.;
	float density 	= 1.;

	float depth 	= 4.0;
	for( int i = 0; i < 32; i++ )
	{
		depth 		+= log(max(.1, depth/float(i+1)));		
		position	= origin	 + direction * depth;
		mass 		+= map_dust(position)*.065;
		density		+= log(max(0.6, mass*.5))/8.;
		color		+= hsv(depth*.015-mass/5., 1., 1.)*mass/65.;
	}    

	color			+= hsv(density/32., 1., 1.)*.3-.1;
	
	return color * density;;
}

float triNoise3d(in vec3 p, in float spd)
{
	float z=1.;
	float rz = 0.;
 	vec3 bp = p;
	float f = 2.;
	for (float i=0.; i<3.; i++ )
	{
		vec3 dg = tri3(bp*f);
		f *= 2.;
		p += (dg+time*spd);
	
		bp *= 1.1;
		z *= 1.5;
		p *= 1.4+rz*.0005;
		
		rz+= (tri(p.z+tri(p.x+tri(p.y))))/z;
		bp += 0.5;
	}
	return rz;
}
vec3 gp = vec3(0.);
vec3 gc = vec3(0.);
float tf(in vec3 p, in float spd, in float f, in float a, in float r)
{
	float n = 0.;
	p -= 1.25;

	for(int i = 0; i < 2; i++)
	{
			n = abs(a-n-triNoise3d(p * f, spd)*a);
			a *= .3;
			f *= 3.;		
			p = mix(p, p.zxy, fract(n-.5)*r*-.0005);
			gc += hsv(length(p), 1., .125);
	}
	
	return n;
}
float capsule( vec3 p, vec3 a, vec3 b, float r )
{
	vec3 pa = p - a;
	vec3 ba = b - a;
	float h = clamp( dot(pa,ba)/dot(ba,ba), 0.0, 1.0 );
	
	return length( pa - ba*h ) - r;
}

vec3 lig_pos = vec3(128.,032.,512.);
vec3 ip = vec3(0.);
vec3 ld = vec3(0.);
float haze = 0.0;

float map(vec3 p) 
{	

	float n = tf(vec3(p.xy, p.z)*.0125, 0., 1.2, 64., length(p));
	
	float d = length(p) - (0.25+n*.01+pow(n, .1)*.001+n*.0005)*.125;
	
	return d-.45;
}

float map_haze(vec3 p) 
{

	float cn = triNoise3d(vec3(p.xz*rmat(time*-.0165),p.y).xzy*3., .12)*.125;
	float c = capsule(p,vec3(0.),-lig_pos*2., .5 + cn*.5);
	return abs(c-cn);
}

float ambient_occlusion(vec3 p, vec3 n)
{
	float a       = 1.;
	const float r = 2.;
        vec3  op = n * 4. + p;
       	float e  = map(op);
        a 	 += (e-4.);

    return max(0., a);
}

float shadow(vec3 vRay,vec3 vDir,vec3 vLight,float fLight,float fEps)
{
	float fShadow=1.0;

	float fLen=fEps*1.;
	float u_fSoftShadow = 1.5;
	for(int n=0;n<8;n++)
	{
		if(fLen>=fLight) break;

		float fDist=map(vRay+(vLight*fLen));
		if(fDist<fEps) return 1.0;

		if(u_fSoftShadow!=0.0)
			fShadow=min(fShadow,u_fSoftShadow*(fDist/fLen));

		fLen+=fDist;
	}

	return fShadow;
}

vec3 calcNormal(vec3 p, float t) 
{
	vec2 e = vec2(-1., 1.)*.3;

	vec3 nor = e.xyy*map_haze(p+e.xyy) + e.yxy*map_haze(p+e.yxy) + e.yyx*map_haze(p+e.yyx) + e.xxx*map_haze(p+e.xxx);
	return normalize(nor);
}

vec3 GetSceneNormal( const in vec3 vPos , const in float fDelta)
{
	return normalize( calcNormal(-vPos, fDelta));
	// tetrahedron normal
	vec3 vOffset1 = vec3( fDelta, -fDelta, -fDelta);
	vec3 vOffset2 = vec3(-fDelta, -fDelta,  fDelta);
	vec3 vOffset3 = vec3(-fDelta,  fDelta, -fDelta);
	vec3 vOffset4 = vec3( fDelta,  fDelta,  fDelta);

	float f1 = map( vPos + vOffset1 );
	float f2 = map( vPos + vOffset2 );
	float f3 = map( vPos + vOffset3 );
	float f4 = map( vPos + vOffset4 );
	
	vec3 vNormal = vOffset1 * f1 + vOffset2 * f2 + vOffset3 * f3 + vOffset4 * f4;

	return normalize( vNormal );
}

void main( void ) {

	vec2 p = ( gl_FragCoord.xy / resolution.xy );
	p = 2.0 * p - 1.0;
	p.x *= resolution.x / resolution.y;
	
	float tm = mouse.y * .5+8.+19.*mouse.x;
	
	float pn = floor(p.x*1.);
	//tm *= pn; //split panel view
	
	float t0 = tm * 0.00005;
	float t1 = t0 + 0.01;
	float r0 = 0.5;
	float k =1.;
	k = pow(k, 4.0);
	
	float r1 = 0.5 + k * 2.0;
	vec3 ro = vec3(0.0, r1 * cos(t0), r1 * sin(t0));
	
	vec3 ta = vec3(0.0, r0 * cos(t1), r0 * sin(t1));
	vec3 cw = normalize(ta - ro);
	vec3 up = normalize(ro - vec3(0.0));
	vec3 cu = normalize(cross(cw, up));
	vec3 cv = normalize(cross(cu, cw));
	
	vec3 rd = normalize(p.x * cu + p.y * cv + 3.0 * cw);

	float d = length(rd.xy);
	float e = 0.000001;
	float h = e * 2.0;
	float t = 0.0;
	float s = 0.;

	vec3 pos = vec3(0.);
	vec3 color = vec3(0.);
	float ice = 0.;
	
	vec3 dust_cloud = dust();
	color 	-= dust_cloud * .0125;
	vec3 lig = normalize(lig_pos-ro);
	float dif = 0.;
	for(int i = 0; i < 128; i++) {
		if(abs(h) < e || t > 8.0) continue;
		pos = ro + rd * t;
		h = map(pos);
		haze += h;
		t += h;
		s++;

		if(h < 0. && s > 16.){h -= h; ice += 1.; color += vec3(.5, .5, .9);}
		e *= 1.05;
	}
	vec3 nor = GetSceneNormal(pos, .00001);

	float delta = clamp(pow(t, 2.25)*.05, .000001, .0001);
	nor = GetSceneNormal(pos, delta);//calcNormal(pos,t);
		
	dif = clamp(dot(nor, lig), 0.0, 1.0);

	float fre = .125 * clamp(1.0 + dot(nor, rd), 0.0, 1.0);
	float spe = 128.-ice*3.;
	spe = pow(max(dot(reflect(rd, nor), lig), 0.0), spe)*.1;
	float sha = shadow(pos, rd, lig_pos, t, e);
	float ao = ambient_occlusion(pos, nor);
		
	color += dif * vec3(.2, .5, .973)  - min(s/128., .15)-d*.1;
	color += spe * 1.5 + fre;
	color *= .5;
	color *= hsv(length(pos*8.+nor*.1)-.9, .25, 1.) + ice * .5 - t*ice*.5;
	color += hsv(length(pos*nor*.5)-.29, .25, 1.)*fre*2.;
	color *= ao*sha*vec3(.9, .8, .8);
	color += s/128.;
	color += min(color, color+(.25 + d *.5 * dust_cloud)-.75);
	if(t>3.)
	{
		color -= vec3(.15/d);

	}
		
	color *= (color + 1.);
	color += dust_cloud*.35;
	color *= .61;
	
	gl_FragColor = vec4( vec3(color), 1.0 );
}
