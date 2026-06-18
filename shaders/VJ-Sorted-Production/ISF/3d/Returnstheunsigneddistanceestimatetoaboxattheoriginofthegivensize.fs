/*{
    "DESCRIPTION": "Returnstheunsigneddistanceestimatetoaboxattheoriginofthegivensize",
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

// Returns the unsigned distance estimate to a box at the origin of the given size
float udBox(vec3 p, vec3 size)
{
	return length(max(abs(p) - size, vec3(0.0)));
}
// Returns the signed distance estimate to a box at the origin of the given size
float sdBox(vec3 p, vec3 size)
{
	vec3 d = abs(p) - size;
	return min(max(d.x, max(d.y, d.z)), 0.0) + udBox(p, size);
}
float sphere(vec3 p, float r)
{
	return length(p) - r;
}

float distScene(vec3 p)
{
	p.xz = mod(p.xz, 1.0) - vec2(0.5);
	vec3 offset = vec3(0.0, -0.25, 0.0);
	float sd = sphere(p - offset, 0.33);
	float sb = sdBox(p - offset, vec3(0.25));
	return max(-sd, sb);

	// p = rotateY(p, 0.5f * p.y);
	// float d1 = sdBox(p - vec3(0, 0.5, 0), vec3(0.5, 1.0, 0.5));
	// float d2 = sdBox(p, vec3(2.0, 0.3, 0.25));
	// return opSubtract(d1, d2);
}

void main()
{
    vec3 eye = vec3(0, 0, -2);
    vec2 offset = (mouse * 2.0 - 1.0) * 4.0;
    eye += vec3(offset, 0);
	
    vec3 up = vec3(0, 1, 0);
    vec3 forward = normalize(-eye);
    vec3 right = normalize(cross(forward, up));
    up = normalize(cross(forward, right));
    float focal = 1.67;
	
    float aspect = resolution.x / resolution.y;
    float u = gl_FragCoord.x * 2.0 / resolution.x - 1.0;
    float v = gl_FragCoord.y * 2.0 / resolution.y - 1.0;
    u *= aspect;
    vec3 rd = normalize(forward * focal + right * u + up * v);
    vec3 ro = eye + right * u + up * v;

    vec4 color = vec4(0.0);

    float t = 0.0;
    const int maxSteps = 32;
    vec3 p;
    for(int i = 0; i < maxSteps; ++i)
    {
        p = ro + rd * t;
        float d = length(p) - 0.55;
	float b = length(max(abs(p) - vec3(0.4), 0.0)) - 0.05;
	float finalDist = distScene(p);//max(-d,b);
        if(finalDist < 0.001)
        {
            color = vec4(1.0 - (float(i) / float(maxSteps)));
            break;
        }

        t += finalDist;
    }
	float coord = floor(p.x)*127.0+floor(p.z);
	float rval = cos(coord);
	float red = fract(17.0*rval);
	float green = fract(23.0*rval);
	float blue = fract(31.0*rval);
    gl_FragColor = color * vec4(red, green, blue, 1.0);
}
