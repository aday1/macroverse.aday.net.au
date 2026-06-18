/*{
    "DESCRIPTION": "StarField-HorizonLight-2",
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
        "noise",
        "3d",
        "texture-input"
    ]
}*/




#define time ((useFrameIndex ? (float(FRAMEINDEX) / fps) : TIME) * speed)
#define mouse vec2(0.0)
#define resolution RENDERSIZE
#ifdef GL_ES
precision highp float;
#endif

vec3   iResolution = vec3(resolution, 1.0);

const int MAX_ITER = 210; // 

// Star Nest by Pablo Román Andrioli
// Modified a lot.
// This content is under the MIT License.

#define iterations 12
#define formuparam 0.530

#define volsteps 18
#define stepsize 0.120

#define zoom   3.700
#define tile   0.850
#define speed  0.00031

#define brightness 0.0012
#define darkmatter 0.600
#define distfading 0.860
#define saturation 0.800

#define EDGE_THRESHOLD .001
#define OCTAVES 8

uniform sampler2D sampler;

vec2 rotate(in vec2 v, in float a) {
	return vec2(cos(a)*v.x + sin(a)*v.y, -sin(a)*v.x + cos(a)*v.y);
}

float map(in vec3 p)
{
	float road = max(abs(p.y-0.025*(1.1+sin(time))), abs(p.z)-0.35);
	return road;
}

vec3 intersect(in vec3 rayOrigin, in vec3 rayDir)
{
	float total_dist = 0.0;
	vec3 p = rayOrigin;
	float d = 1.0;
	float iter = 0.0;
	float mind = 3.14159+sin(time*0.1)*0.2;
	
	for (int i = 0; i < MAX_ITER; i++)
	{		
		if (d < 0.001) continue;
		
		d = map(p);
		p += d*vec3(rayDir.x, rotate(rayDir.yz, sin(mind)));
		mind = min(mind, d);
		total_dist += d;
		iter++;
	}

	vec3 color = vec3(0.0);
		float x = (iter/float(MAX_ITER));
		float y = (d-0.01)/0.01/(float(MAX_ITER));
		float z = (0.01-d)/0.01/float(MAX_ITER);
			float w = smoothstep(mod(p.x*50.0, 4.0), 2.0, 2.01);
			w -= 1.0-smoothstep(mod(p.x*50.0+2.0, 4.0), 2.0, 1.99);
			w = fract(w+0.0001);
			float a = fract(smoothstep(abs(p.z), 0.0025, 0.0026));
			color = vec3((1.0-x-y*2.)*mix(vec3(0.8, 0.1, 0), vec3(0.1), 1.0-(1.0-w)*(1.0-a)));
	return color;
}

float noise(float x) {
	return fract(sin(x*123.321 + 2345.4232)*789.432);
}

float perlin(float x) {
	float y = 1.0;
	for (int i = 0; i < OCTAVES; i++) {
		float a = pow(2.0, -float(i));
		float m = mod(x, a);
		float b = x - m;
		y += ((noise(b)*(1.0-m/a) + noise(b+a)*(m/a))/2.0)*a;
	}
	return y;
}

float noise2d(vec2 p) {
	return fract(sin(dot(p.xy ,vec2(12.9898,78.233))) * 456367.5453);
}

vec4 sample(int x, int y)
{
	vec2 p = ((gl_FragCoord.xy + vec2(x, y)) / resolution.xy );
	float building = 0.0;
	float color=0.0;
	for (int i = 1; i < 20; i++) {
		float fi = float(i)*(1.+(1.*mouse.y));
		
		float s = floor(250.*p.x/fi+15.*fi*(mouse.x));
		if (p.y-fi/(222.*mouse.y) < noise2d(vec2(s,1./s)) - fi*(0.05*(0.5+mouse.y))+0.001*sin(time*0.5)) {
			building = fi/(21.);
			color=fi/abs((21.0-20.*sin(time*0.5)));
			color+= noise2d(vec2(s,-s))*tan((p.x*floor(fi)) )+atan(p.y/p.y);
			color+=0.0005*(1.0+sin(time*0.5))*floor(tan( p.x*160.0)+time + tan( p.y * 1230.0 ));
		}
	}
	return vec4(building,building,building*color,building);
}
	
float roundedRectangle (vec2 p, vec2 s, float r) {
	r *= 0.1;
	vec2 d = abs (p) - s + r;
	return min (max (d.x, d.y), 0.0) + length (max (d, 0.0)) - r;
}

bool edge(){
	return  distance(sample(0, 0), sample(-1, 1)) > EDGE_THRESHOLD;
}

vec3 flare(vec2 spos, vec2 fpos, vec3 clr){
	vec3 color;
	color= clr * max(0.0, 0.5*abs(cos(time/1.0)) / distance(spos, fpos));
	return color;
}

void main( void )
{
	vec2 p = ( gl_FragCoord.xy / resolution.xy );
	float stars = fract(7153.1 * sin(pow(p.x/p.y, 2.)));
	if (stars < 0.99+sin(time)*0.005) stars = .1;
	vec3 sun = vec3(clamp(length(p.xy*.15) * .35, .20, 1.0), 0.0, 0.0) + smoothstep(0.15, 1.10, length((gl_FragCoord.xy/(1.05*resolution.xy))-1.0*sin(time/12.0))) * 1.9;
	vec3 sky = sun ;//+ vec3(1.9, .86, .8) * p.x * p.y * sin(time * 0.25) + (1.0 - sin(time * 0.25)) * stars;

	vec2 lightpos = abs(-.5+0.25*sin(time/5.0))*resolution;
	
	vec2 norm = lightpos - gl_FragCoord.xy;
	float sdist = norm.x * norm.x + norm.y * norm.y;
	
	vec3 light_color = vec3(1.0,0.6,1.0);
	
	float color = 1.0 / (sdist * 0.01);
	vec3 moon= vec3(color,color,color)*(light_color);

	sky = sun*sin(time/5.0)+moon*(sun*moon);//+ flare(position2, vec2(sin(omega)/2., cos(omega)) * 0.5 , vec3(20.5*sin(time/5000.0), 0.8, 1.5));
	
	vec3 bluesky=vec3(pow(1.0-p.y,2.0),1.0-p.y,0.6+(1.0-p.y)*0.4);
	sky=stars+sky;//+max(0.0,sin(time*0.5))*sky+cos(time*0.5)*bluesky;
	
	//get coords and direction
	vec2 uv = gl_FragCoord.xy/resolution.xy;
	uv.y *= resolution.y/resolution.x;
	vec3 dir=vec3(uv*zoom,1.);
	vec3 from=vec3(.05*time,.05*time,12.);

	//volumetric rendering
	float s=.4,fade=.2;
	vec3 v=vec3(0.4);
	for (int r=0; r<volsteps; r++) {
		vec3 p=from+s*dir*.5;
		p = abs(vec3(tile)-mod(p,vec3(tile*2.))); // tiling fold
		float pa,a=pa=0.;
		for (int i=0; i<iterations; i++) {
			p=abs(p)/dot(p,p)-formuparam; // the magic formula
			a+=abs(length(p)-pa); // absolute sum of average change
			pa=length(p);
		}
		float dm=max(0.,darkmatter-a*a*.001); //dark matter
		a*=a*a*2.; // add contrast
		if (r>3) fade*=1.-dm; // dark matter, don't render near
		v+=fade;
		v+=vec3(s,s*s,s*s*s*s)*a*brightness*fade; // coloring based on distance
		fade*=distfading; // distance fading
		s+=stepsize;
	}
	v=mix(vec3(length(v)),v,saturation); //color adjust
	sky= sky+vec3(v*.01*abs(sin(time)));

	vec4 building=sample(0,0);
	if(building.y>0.0){
		sky=vec3(0,0,0);
		if(edge()){
			building=vec4(0.);
		} 
	}
	
	float mountain = 0.00 ;
	for (int i = 1; i < 8; i++) {
		float fi = float(i)*(0.7+(0.1*mouse.y));
		mountain += 0.5*clamp(perlin(p.x*(fi*1.5)+0.0*time/(4.0))*(14.0*fi)- p.y*mouse.y*512.0-20.0, 0.00,fi);
	}
	if(mountain>.00){
		sky=vec3(0.0);
		building= vec4(0.201,mountain*0.1*p.y,0.01*mountain,1.0 );
	}
	
	vec4 window=vec4(0);	
	vec2 position = (2.0 * gl_FragCoord.xy - resolution.xy) / resolution.y;
	float h = roundedRectangle (position, vec2 (1.7, 0.7), mouse.x);
	window = vec4 (smoothstep (0.1, 0.0, h) * mix (vec3 (0.3, 0.7, 0.9), vec3 (0.0, 0.4, 1.0), position.y * 4.3), 1.0);
	
	gl_FragColor = window*(vec4(sky*2.0+vec3(building), 1.0));
	gl_FragColor += 0.6 * (texture2D(sampler, gl_FragCoord.xy/resolution*(1.0+0.00125*sin(time/4.0))) - gl_FragColor);
	
	vec3 upDirection = vec3(0, -1, 0);
	vec3 cameraDir = vec3(1,0,0);
	vec3 cameraOrigin = vec3(time*0.551, 0, 0);
	vec3 u = normalize(cross(upDirection, cameraOrigin));
	vec3 v2 = normalize(cross(cameraDir, u));
	vec2 screenPos = -0.5 + 2.0 * gl_FragCoord.xy / iResolution.xy;
	screenPos.x *= iResolution.x / iResolution.y;
	vec3 rayDir = normalize(u * screenPos.x + v2 * screenPos.y + cameraDir*(1.0-length(screenPos)*0.5));
	vec3 intersect2=intersect(cameraOrigin, rayDir);
	if (intersect2.z>0.0){
		gl_FragColor = vec4(intersect2, 10.0);
	} 
}


