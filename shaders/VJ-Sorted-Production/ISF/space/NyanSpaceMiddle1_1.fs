/*{
    "DESCRIPTION": "NyanSpaceMiddle1",
    "CREDIT": "GLSL Sandbox / various",
    "ISFVSN": "2.0",
    "CATEGORIES": [
        "space"
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
        "space",
        "particles",
        "texture-input"
    ]
}*/uniform sampler2D inputImage;




#define time ((useFrameIndex ? (float(FRAMEINDEX) / fps) : TIME))
#define mouse vec2(0.0)
#define resolution RENDERSIZE
/*by mu6k, Creative Commons Attribution-NonCommercial-ShareAlike 3.0 Unported License.
 https://www.shadertoy.com/view/4dXGWH#
 I started animating the nyancat image. I looked up the original colors 
 for rainbow and background. I kept adding stuff until I ended up with 
 something I could watch for 10 hours. Nah 10 hours is too long... or is it? 

 Feel free to tweak the quality parameter... if you have a monster pc.

 quality 1 - no AA, extremely noisy blur.
 quality 2 - 2xAA, decent
 quality 3 - 3xAA, slow here on fullscreen
 quality 4 - 4xAA, doesn't even compile here

 Enjoy!

 04/05/2013:
 - published

 muuuuuuuuuuuuuuuuuuuuuuuuuuuuuuuusk!*/

#define quality 1

float hash(float x)
{
	return fract(sin(x*.0127863)*17143.321);
}

float hash(vec2 x)
{
	return fract(sin(dot(x,vec2(1.4,52.14)))*17143.321);
}

float hashmix(float x0, float x1, float interp)
{
	x0 = hash(x0);
	x1 = hash(x1);
	#ifdef noise_use_smoothstep
	interp = smoothstep(0.0,1.0,interp);
	#endif
	return mix(x0,x1,interp);
}

float hashmix(vec2 p0, vec2 p1, vec2 interp)
{
	float v0 = hashmix(p0[0]+p0[1]*128.0,p1[0]+p0[1]*128.0,interp[0]);
	float v1 = hashmix(p0[0]+p1[1]*128.0,p1[0]+p1[1]*128.0,interp[0]);
	#ifdef noise_use_smoothstep
	interp = smoothstep(vec2(0.0),vec2(1.0),interp);
	#endif
	return mix(v0,v1,interp[1]);
}

float hashmix(vec3 p0, vec3 p1, vec3 interp)
{
	float v0 = hashmix(p0.xy+vec2(p0.z*43.0,0.0),p1.xy+vec2(p0.z*43.0,0.0),interp.xy);
	float v1 = hashmix(p0.xy+vec2(p1.z*43.0,0.0),p1.xy+vec2(p1.z*43.0,0.0),interp.xy);
	#ifdef noise_use_smoothstep
	interp = smoothstep(vec3(0.0),vec3(1.0),interp);
	#endif
	return mix(v0,v1,interp[2]);
}

float hashmix(vec4 p0, vec4 p1, vec4 interp)
{
	float v0 = hashmix(p0.xyz+vec3(p0.w*17.0,0.0,0.0),p1.xyz+vec3(p0.w*17.0,0.0,0.0),interp.xyz);
	float v1 = hashmix(p0.xyz+vec3(p1.w*17.0,0.0,0.0),p1.xyz+vec3(p1.w*17.0,0.0,0.0),interp.xyz);
	#ifdef noise_use_smoothstep
	interp = smoothstep(vec4(0.0),vec4(1.0),interp);
	#endif
	return mix(v0,v1,interp[3]);
}

float noise(float p)
{
	float pm = mod(p,1.0);
	float pd = p-pm;
	return hashmix(pd,pd+1.0,pm);
}

float noise(vec2 p)
{
	vec2 pm = mod(p,1.0);
	vec2 pd = p-pm;
	return hashmix(pd,(pd+vec2(1.0,1.0)), pm);
}

float noise(vec3 p)
{
	vec3 pm = mod(p,1.0);
	vec3 pd = p-pm;
	return hashmix(pd,(pd+vec3(1.0,1.0,1.0)), pm);
}

float noise(vec4 p)
{
	vec4 pm = mod(p,1.0);
	vec4 pd = p-pm;
	return hashmix(pd,(pd+vec4(15.0,1.0,1.0,1.0)), pm);
}

vec3 background(vec2 p)
{
	vec2 pm = mod(p,vec2(0.5));
	float q = pm.x+pm.y;
    // Change background color
	vec3 color = vec3(24,11,21)/255.0;
	// SUMS FOR VU
	vec2 visuv = p*0.5;
	vec2 visuvm = mod(visuv,vec2(0.05,0.05));
	visuv-=visuvm;
	float vis = texture2D(inputImage,vec2((visuv.x+1.0)*.5,0.0)).y*2.0-visuv.y;
	
	// VU Color! and Shape I think
    if (vis>1.0&&visuvm.x<0.03&&visuvm.y<0.04)
		color.xyz *=12.755;
	
	vec2 p2;
	float stars;
	for (int i=0; i<15; i++)
	{
		float s = float(i)*0.2+1.0;
		//float ts
        // Stars motion bluring
		p2=p*s+vec2(time/s,s*16.0)-mod(p*s+vec2(time/s,s*1.0),vec2(0.05));
		stars=noise(p2*32.0);
		if (stars>0.98) color=vec3(4.0,7.0,8.0);
	}
	
	vec2 visuv2 = p*0.25+vec2(time*0.1,0);
	vec2 visuvm2 = mod(visuv2,vec2(0.05,0.05));
	visuv2-=visuvm2;
	
	float vis2p = hash(visuv2);
	float vis2 = texture2D(inputImage,vec2(vis2p,0)).y;
	
	vis2 = pow(vis2,20.0)*261.0;
	vis2 *= vis2p;
	
	if (vis2>1.0) vis2=1.0;
	
	if (vis2>0.2&&visuvm2.x<0.09&&visuvm2.y<0.09)
		color+=vec3(vis2,vis2,vis2)*1.0;
	
	return color;
}

vec4 rainbow(vec2 p)
{
	
	p.x-=mod(p.x,0.00);
	float q = max(p.x,-0.0);
	float s = sin(p.x*(0.0)+time*0.0)*0.00;
	s-=mod(s,0.00);
	p.y+=s;

//p.y += sin(p.x*8.0+time)*0.25;
	
	vec4 c;
	
	if (p.x>0.0) c=vec4(0,0,0,0); else
	if (0.0/6.0<p.y&&p.y<1.0/6.0) c= vec4(255,43,14,255)/255.0; else
	if (1.0/6.0<p.y&&p.y<2.0/6.0) c= vec4(255,168,6,255)/255.0; else
	if (2.0/6.0<p.y&&p.y<3.0/6.0) c= vec4(255,244,0,255)/255.0; else
	if (3.0/6.0<p.y&&p.y<4.0/6.0) c= vec4(51,234,5,255)/255.0; else
	if (4.0/6.0<p.y&&p.y<5.0/6.0) c= vec4(8,163,255,255)/255.0; else
	if (5.0/6.0<p.y&&p.y<6.0/6.0) c= vec4(122,85,255,255)/255.0; else
		c=vec4(11,0,10,0);
	return c;
}

vec4 nyan(vec2 p)
{
	vec2 uv = p*vec2(6.4,1.0);
	float ns=2.0;
	float nt = time*ns; nt-=mod(nt,240.0/256.0/6.0); nt = mod(nt,240.0/256.0);
	float ny = mod(time*ns,1.0); ny-=mod(ny,0.75); ny*=-0.05;
	vec4 color = texture2D(inputImage,vec2(uv.x/3.0+210.0/256.0-nt+0.05,.5-uv.y-ny));
	if (uv.x<-0.0) color.a = 0.0;
	if (uv.x>0.0) color.a=0.0;
	return color;
}

vec4 scene(vec2 uv)
{
	
	vec4 color = nyan(uv);
	vec3 c2 = background(uv+vec2(0,0));
	vec4 c3 = rainbow(vec2(uv.x+0.4,0.05-uv.y*2.0+.5));
	
	vec4 c4 = mix(vec4(c2,1.0),c3,c3.a);
		
	color = mix(c4,color,color[3]);
	return color;
}

void mainImage( out vec4 fragColor, in vec2 fragCoord )
{
	float vol = .0;
	for(float x = 0.0; x<1.0; x+=1.0/256.0)
	{
		vol += texture2D(inputImage,vec2(x,0.0)).y;
	}
	vol*=1.0/256.0;
	vol = pow(vol,16.0)*256.0;

	vec2 uv = fragCoord.xy / resolution.xy-0.5;
	uv.x*=resolution.x/resolution.y;
	vec2 ouv=uv;
	float t = time-length(uv)*0.5;
	
	uv.x+=sin(t*0.4)*0.05;
	uv.y+=cos(t)*0.00;

	// Spins!
	float angle = (cos(t*4.461)+cos(t*3.71)+cos(t*2.342)+cos(t*1.512))*0.2;
	angle+=sin(t*32.0)*vol*0.1;
	float zoom = (cos(t*1.364)+cos(t*2.686)+cos(t*3.286)+cos(t*4.496))*0.2;
	uv*=pow(2.0,zoom+1.0-vol*4.0);
	uv=uv*mat2(cos(angle),-sin(angle),sin(angle),cos(angle));

	vec4 color = vec4(0);
	
	uv.y+=vol;
// more noise like blur
	for(int x = 0; x<quality; x++)
	for(int y = 0; y<quality; y++)
	{
		float xx = float(x)*2.0/float(quality)/resolution.y;
		float yy = float(y)*2.0/float(quality)/resolution.y;
		float n = hash(color.xy+ouv+vec2(float(x),float(y)));
		float s = 1.0-pow(n,5.0)*vol*66.0;
		color += scene((uv+vec2(xx,yy))*s)/float(quality*quality)*(1.0+vol);
	}

    // Increase for Noise
	color-=vec4(length(uv)*0.07);
	color.xyz+=vec3(hash(color.xy+ouv))*0.00;
	fragColor = color;
}
