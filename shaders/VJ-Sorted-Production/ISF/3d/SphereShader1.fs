/*{
    "DESCRIPTION": "SphereShader1",
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
// http://glslsandbox.com/e#17665.7
// Ball mask
#ifdef GL_ES
precision mediump float;
#endif

float pi = atan(1.)*4.;

float backStripes = 32.0;
vec3 backColor1 = vec3(0.345,0.812,0.929);
vec3 backColor2 = vec3(0.039,0.580,0.717);

float sphereStripes = 55.0;
float sphereMinSize = 1;
float sphereMaxSize = 3;
float spherePulseSpeed = 1.0;
float sphereShadowSize = 0.06;
float sphereSpinSpeed = 2.0;
vec3 sphereRotationAxis = vec3(0.5,1,0.5);
vec3 sphereColor1 = vec3(0.25);
vec3 sphereColor2 = vec3(1.00);

//Axis-Angle rotation using Rodrigues' rotation formula
vec3 rotate(vec3 axis, float ang, vec3 vec)
{
	axis = normalize(axis);
	return vec * cos(ang) + cross(axis, vec) * sin(ang) + axis * dot(axis, vec) * (1.0 - cos(ang));
}

void main( void ) 
{
	vec2 res = vec2(resolution.x / resolution.y, 1.0);
	vec2 cen = res / 2.0;
	vec2 p = ( gl_FragCoord.xy / resolution.y ) - cen;
	p *= 4.0;
	
	vec3 col;
	
	//Sphere size & blending mask
	float midSize = (sphereMaxSize + sphereMinSize) / 2.0;
	float deltaSize = (sphereMaxSize - sphereMinSize);
	float size = sin(time * spherePulseSpeed) * deltaSize + midSize;	
	float mask = smoothstep(size, size - 0.01, length(p));
	
	//Background stripes
	float split = step(p.y, 0.0) * 2.0 - 1.0;
	float back = sin((p.x + p.y * split) * pi * 0.125 * backStripes);
	back = smoothstep(0.0, 0.01, back);
	col += mix(backColor1, backColor2, back);
	
	//Shadow
	col *= smoothstep(size + sphereShadowSize, size + sphereShadowSize + 0.01, length(p)) * 0.5 + 0.5;
	col *= 1.0 - mask;
	
	//Sphere height at point p
	float height = sqrt(abs(p.x * p.x + p.y * p.y - size * size)) * mask;
	
	//Pixel's position in 3D based on screen position and sphere height
	vec3 pos = vec3(p, height);
	
	//Rotate the 3d position around the rotation axis
	pos = rotate(sphereRotationAxis, time * sphereSpinSpeed, pos);
	
	//Stripes on the sphere
	float pitch = atan(length(pos.xz), pos.y);
	float bands = sin(pitch * sphereStripes - pi);
	bands = smoothstep(0.0, 0.1, bands);
	col += mix(sphereColor1, sphereColor2, bands) * mask;
	
	gl_FragColor = vec4( col, 1.0 );

}
