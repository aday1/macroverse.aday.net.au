/*{
    "DESCRIPTION": "DotMatrix-Emerald-LitSurface-11",
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
        "3d"
    ]
}*/




#define time ((useFrameIndex ? (float(FRAMEINDEX) / fps) : TIME))
#define mouse vec2(0.0)
#define resolution RENDERSIZE
#ifdef GL_ES
precision mediump float;
#endif

#extension GL_OES_standard_derivatives : enable

//	???????
float distSphere( vec3 p, float r){
    vec3 q = abs(p);
	return length(q)-r;
}

//	??(???????????????)
float distPlane( vec3 p )
{
	return p.y;
}

//	???????(????????)
float smin( float a, float b, float k )
{
    float h = clamp( 0.5+0.5*(b-a)/k, 0.0, 1.0 );
    return mix( b, a, h ) - k*h*(1.0-h);
}

//	????(DistanceFunction)
float distanceFunc(vec3 p){
	vec3 obj = vec3( 0.0, 0.5,-5.0 );
	vec3 obj_plane = vec3(0.0, 0.0, 5.0 );

	float d2 = distSphere(p-obj, 0.7);
	float d5 = distPlane(p-obj_plane);

	return smin(d2, d5, 0.1);
}

//	?????????
vec3 getNormal(vec3 p){
    float d = 0.0001;
    return normalize(vec3(
        distanceFunc(p + vec3(  d, 0.0, 0.0)) - distanceFunc(p + vec3( -d, 0.0, 0.0)),
        distanceFunc(p + vec3(0.0,   d, 0.0)) - distanceFunc(p + vec3(0.0,  -d, 0.0)),
        distanceFunc(p + vec3(0.0, 0.0,   d)) - distanceFunc(p + vec3(0.0, 0.0,  -d))
    ));
}

//	?????
float getShadow(vec3 ro, vec3 rd){
    float h = 0.0;
    float c = 0.0;
    float r = 1.0;
    float shadowCoef = 0.5;
    for(float t = 0.0; t < 50.0; t++){
        h = distanceFunc(ro + rd * c);
        if(h < 0.001){
            return shadowCoef;
        }
        r = min(r, h * 16.0 / c);
        c += h;
    }
    return 1.0 - shadowCoef + r * shadowCoef;
}

//	AmbientOcclusion???
float AO(vec3 p,vec3 n)
{
	float dlt = 0.5;
	float oc = 0.0, d = 1.0;
	for(int i = 0; i < 6; i++)
	{
		oc += (float(i) * dlt - distanceFunc(p + n * float(i) * dlt)) / d;
		d *= 2.0;
	}
	return 1.0 - oc;
}

//	???????
vec3 filt( vec3 col ){
	if(col.x >0.99777 )
		col.x = 0.99777;
	if( col.y > 0.9987)
		col.y =0.9987;
	if( col.z > 0.9777)
		col.z = 0.9777;
	return col;
}
//	?????
void main( void ) {
	vec2 p = (gl_FragCoord.xy * 2.0 - resolution) / min(resolution.x, resolution.y) + mouse / 4.0;
	
	const vec3 lightDir = vec3(1.577, 1.577, 1.577);
	// camera
	vec3 cPos = vec3(0.9, 3.57, 6.0);	//??????
	vec3 cDir = vec3(-0.1,  -0.3, -1.0);	//??????
	vec3 cUp  = vec3(0.0,  1.0,  0.0);	//??????
	vec3 cSide = cross(cDir, cUp);
	float targetDepth = 8.0;
    
	// ??????
	vec3 ray = normalize(cSide * p.x + cUp * p.y + cDir * targetDepth);

	// ???
	vec3 light = normalize(lightDir + vec3(-1.5+3.*sin(time), 0.0, 0.0 ));
    
	// ???????????(??)
	float distance = 0.0;
	float rLen = 0.0;
	vec3  rPos = cPos;

	for(int i = 0; i < 512; i++){
        distance = distanceFunc(rPos);
		if( distance < 0.001 )break;
		rLen += distance;
		rPos = cPos + ray * rLen;
	}

	vec3 color;
	float shadow = 1.0;

	//	???
	if(abs(distance) < 0.001){
		//	??????????
		vec3 normal = getNormal(rPos);
		
		//	diffusion ????
		float diff = clamp(dot(lightDir, normal), 0.9, 0.7);
		// generate tile pattern
		float u = 1.0 - floor(mod(rPos.x*cos(rPos.x*rPos.z), 8.0));
		float v = 1.0 - floor(mod(rPos.z, 2.0));
		if((u == 1.0 && v < 1.0) || (u < 1.0 && v == 1.0)){
		    diff *= 0.7;	//	???????????
		}
		
		// ????????
		vec3 halfLE = normalize(light - ray);
		float spec = pow(clamp(dot(halfLE, normal), 0.0, 1.0), 50.0);
		shadow = getShadow(rPos + normal * 0.001, light);
		
		float a = AO( light, normal );
		//	????????
		float camLength = rLen;
		color = (((vec3(diff,(distance+diff), 1.0)*diff + 0.8*vec3(spec) )*max(0.2, shadow)
			+vec3(camLength)*0.0195	//	?????(?????????????????)
			))
			*a*0.56			//	AmbientoOcclusion???
			;
		}else{
			color = vec3(0.7);	//	??????????????(????)
		}
	color -= (mod(gl_FragCoord.y, 2.0) < 0.8 ? 0.14 : 0.0);	//	???????
	color = filt(color);			//	????
	gl_FragColor = vec4(color*0.94 , 0.6);	//??????+???????????
}
