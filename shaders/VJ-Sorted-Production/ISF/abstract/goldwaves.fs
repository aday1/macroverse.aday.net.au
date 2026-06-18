/*{
    "DESCRIPTION": "goldwaves",
    "CREDIT": "GLSL Sandbox / various",
    "ISFVSN": "2.0",
    "CATEGORIES": [
        "abstract"
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
        "abstract"
    ]
}*/



#define time ((useFrameIndex ? (float(FRAMEINDEX) / fps) : TIME) * timeScale)
#define mouse vec2(mouseX, mouseY)
#define resolution RENDERSIZE
#ifdef GL_ES
precision mediump float;
#endif

vec2 Distort(vec2 p)
{
    float theta  = atan(p.y, p.x);
    float radius = length(p);
    radius = pow(radius, 1.3);
    p.x = radius * cos(theta);
    p.y = radius * sin(theta);
    return 0.5 * (p + 1.0);
}
vec4 pattern(vec2 p)
{
	vec2 m=mod(p.xy+p.x+p.y,2.)-1.;
	return vec4(length(m+p*0.1));
}

float hash(const float n)
{
	return fract(sin(n)*43758.5453);
}

float noise(const vec3 x)
{
	vec3 p=floor(x);
	vec3 f=fract(x);

    	f=f*f*(3.0-2.0*f);

    	float n=p.x+p.y*57.0+p.z*43.0;

    	float r1=mix(mix(hash(n+0.0),hash(n+1.0),f.x),mix(hash(n+57.0),hash(n+57.0+1.0),f.x),f.y);
    	float r2=mix(mix(hash(n+43.0),hash(n+43.0+1.0),f.x),mix(hash(n+43.0+57.0),hash(n+43.0+57.0+1.0),f.x),f.y);

	return mix(r1,r2,f.z);
}
void main( void ) {

	vec2 position = ( gl_FragCoord.xy / resolution.xy * 7. ) + mouse;

	float off = noise(position.xyx + time);
	vec4 c = pattern(Distort(position+off));

	c.xy = Distort(c.xy);
	gl_FragColor = vec4(c.x - off, sin(c.y) - off, cos(c.z), 1.0);

}
