/*{
    "DESCRIPTION": "DotMatrix-Emerald-LitSurface-7",
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
precision mediump float;

const float sphereSize = 1.0;

vec3 rotate(vec3 p, float angle, vec3 axis){
    vec3 a = normalize(axis);
    float s = sin(angle);
    float c = cos(angle);
    float r = 1.0 - c;
    mat3 m = mat3(
        a.x * a.x * r + c,
        a.y * a.x * r + a.z * s,
        a.z * a.x * r - a.y * s,
        a.x * a.y * r - a.z * s,
        a.y * a.y * r + c,
        a.z * a.y * r + a.x * s,
        a.x * a.z * r + a.y * s,
        a.y * a.z * r - a.x * s,
        a.z * a.z * r + c
    );
    return m * p;
}

vec3 trans(vec3 p){
    return vec3(p.x,mod(p.y,8.0)-4.0,mod(p.z, 4.0) - 2.0);
}

float distanceFunc(vec3 p){
	vec3 q;
	q = trans(p);
	q = rotate(q,floor(p.z/4.0)*0.3,vec3(0,0,-1.0));
	//q = p;
    //return length(q) - sphereSize;
	return length(max(abs(q) - vec3(2.0, 2.0, 1.0), 0.0));
}

vec3 getNormal(vec3 p){
    float d = 0.0001;
    return normalize(vec3(
        distanceFunc(p + vec3(  d, 0.0, 0.0)) - distanceFunc(p + vec3( -d, 0.0, 0.0)),
        distanceFunc(p + vec3(0.0,   d, 0.0)) - distanceFunc(p + vec3(0.0,  -d, 0.0)),
        distanceFunc(p + vec3(0.0, 0.0,   d)) - distanceFunc(p + vec3(0.0, 0.0,  -d))
    ));
}

void main(void){
    // fragment position
    vec2 p = (gl_FragCoord.xy * 2.0 - resolution) / min(resolution.x, resolution.y);

	vec3 lightDir = normalize(vec3(-.577, 1.577, 3.577));

    // camera
    float nowtime = mod(time,1000.0);
    vec3 cPos = vec3(40.0+30.0*sin(time*0.3),  3.0,  10.0-nowtime*1.0);
    vec3 cDir = normalize(vec3(-.50,  -.0, +sin(nowtime*0.3)));
    vec3 cUp  = vec3(0.0,  1.0,  0.0);
    vec3 cSide = cross(cDir, cUp);
    float targetDepth = 3.0;

    // ray
    vec3 ray = normalize(cSide * p.x + cUp * p.y + cDir * targetDepth);
    
    // marching loop
    float distance = 0.0;
    float rLen = 0.0;
    vec3  rPos = cPos;
	int j = 0;
	int hitflag = 0;
    for(int i = 0; i < 128; i++){
        distance = distanceFunc(rPos);
	    j++;
        rLen += distance;
        rPos = cPos + ray * rLen*0.8;
	    if(abs(distance)<0.001){
	            hitflag = 1;
		    break;
	    }
	    if(rLen > 10000.0){
		    j = 128;
		    break;
	    }
    }
    
    // hit check
    if(hitflag == 1){
        vec3 normal = getNormal(rPos);
        float diff = clamp(dot(lightDir, normal), 0.1, 1.0);
        gl_FragColor = vec4(vec3(0.2,0.2,0.9)*diff, 1.0);
 gl_FragColor += vec4(vec3(1.0,1.0,1.0)*(abs(1.0/rPos.x) + abs(1.0/(mod(rPos.y,8.0)-4.0))) * 0.1,1.0);
    }else{
        gl_FragColor = vec4(vec3(0.0,0.0,0.0), 1.0);
    }
	gl_FragColor += vec4(vec3(1.0,0.8,1.0)*float(j)/128.0*0.8,1.0);
	
}
