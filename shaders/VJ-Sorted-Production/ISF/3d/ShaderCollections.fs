/*{
    "DESCRIPTION": "ShaderCollections",
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
#ifdef GL_ES
precision mediump float;
#endif

float hash( float n )
{
    return fract(sin(n*12.435)*43758.5453123);
}

bool notEmpty(ivec3 p)
{
	if (mod(float(p.z), 2.0) < 0.5)
		return length(vec2(p.xy)) > 2.3+1.25*sin(0.7*time)+4.0*hash(float(p.z));
	else
		return length(vec2(p.xy)) > 5.66*sin(0.8769*time+1.3)+7.77;
}

// return location of first non-empty block
// marching accomplished with fb39ca4's non-branching dda: https://www.shadertoy.com/view/4dX3zl
vec3 intersect(vec3 ro, vec3 rd, out ivec3 ip, out ivec3 n)
{
	ivec3 mapPos = ivec3(floor(ro + 0.));
	vec3 deltaDist = abs(vec3(length(rd)) / rd);
	ivec3 rayStep = ivec3(sign(rd));
	vec3 sideDist = (sign(rd) * (vec3(mapPos) - ro) + (sign(rd) * 0.5) + 0.5) * deltaDist; 
	
	bvec3 mask;
	for (int i = 0; i < 25; i++) {
		if (notEmpty(mapPos)) continue;
		
		bvec3 b1 = lessThan(sideDist.xyz, sideDist.yzx);
		bvec3 b2 = lessThanEqual(sideDist.xyz, sideDist.zxy);
		mask.x = b1.x && b2.x;
		mask.y = b1.y && b2.y;
		mask.z = b1.z && b2.z;
		
		//All components of mask are false except for the corresponding largest component
		//of sideDist, which is the axis along which the ray should be incremented.			
		sideDist += vec3(mask) * deltaDist;
		mapPos += ivec3(mask) * rayStep;
	}
	ip = mapPos;
	n = ivec3(mask)*ivec3(sign(-rd));
	
	// intersect the cube (copied from iq :] )
	vec3 mini = (vec3(mapPos)-ro + .25 - .25*sign(rd))/rd;
	float t = 3.14756465*sin(time)*max ( mini.x, max ( mini.y, mini.z ) );
	
	return ro-rd*t;
}

void mainImage( out vec4 fragColor, in vec2 fragCoord )
{
	vec2 uv = (fragCoord.xy-resolution.xy/2.0) / resolution.yy * 8.0;
	//uv.y = -uv.y;

	// perspective projection
	vec3 ro = vec3(0.5,0.5,time*6.0);
	vec3 rd = normalize(vec3(uv+vec2(0.01,0.01),1.0));
	
	ivec3 ip;
	ivec3 n;
	vec3 pos = intersect(ro,rd, ip,n);
	vec3 col = vec3(1.0);
	col.r = -sin(float(ip.y-999)/23.0);
	col.g = 1.2*sin(float(ip.yzx-99)/324.0)+.3;
	col.b = sin(float(ip.yz)/12.0);
	col *= sin(time*.82362+0.2*hash(float(ip.z+23*ip.x+187*ip.y+187)));
	
	vec3 cubeP = mod(pos-0.001,1.0);
	vec3 edgeD = 0.5-abs(cubeP-0.5);
	edgeD += abs(fract(abs(fract(cos(23.68*time))+sin(abs(fract(vec3(n))))*sin(-time*0.234+time))));
	float closest = fract(fract(time*0.001)-min(edgeD.x,min(edgeD.y,edgeD.z)));
	float glow = smoothstep(.1,0.0,closest);
	col += 1.1618*glow;
	col += float(n.yz)*.15;
	col *= 8.8/pow(length(pos.z-ro.z),sin(time*0.617845)*.37619816+1.);

	fragColor = vec4(col,1.0);
}

void main()
{
	vec4 color;
	mainImage(gl_FragColor, gl_FragCoord.xy);
}
