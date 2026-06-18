/*{
    "DESCRIPTION": "StarField-GlowBloom-8",
    "CREDIT": "GLSL Sandbox / various",
    "ISFVSN": "2.0",
    "CATEGORIES": [
        "particles"
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
        "particles"
    ]
}*/




#define time ((useFrameIndex ? (float(FRAMEINDEX) / fps) : TIME) * speed)
#define mouse vec2(0.0)
#define resolution RENDERSIZE

// StarTripV2     by I.G.P
// y position of mouse = travel speed

#ifdef GL_ES
precision mediump float;
#endif

#define speed 2.0
#define kRotate 0.002
#define k2PI (2.*3.14159265359)
#define kStarDensity 0.4
#define kMotionBlur 0.2
#define kNumAngles 256.

void main( void )
{
	vec2 position = .3*( gl_FragCoord.xy -  resolution.xy*.5 ) / resolution.x;
	position.x += 0.1*mouse.x - 0.05; // use this for mouse panning
	float angle0 = atan(position.y, position.x) / k2PI;
	float angle = fract(angle0 + kRotate*time);
	float rad = length(position);
	float angleFract = fract(angle*kNumAngles);
	float angleStep = floor(angle*kNumAngles)+1.;
	float angleToRandZ = 10.*fract(angleStep*fract(angleStep*.7535)*45.1);
	float angleSquareDist = fract(angleStep*fract(angleStep*.82657)*13.724);
	float t = (speed+mouse.y*22.) * time - 222.*angleToRandZ;
	float angleDist = (angleSquareDist+0.1);
	float adist = angleDist/rad*kStarDensity;
	float dist = abs(fract((t*.1+adist))-.3);
	float white1 = max(0.,1.0 - dist * 100.0 / ((kMotionBlur+mouse.y*3.0)*speed+adist));
	float white2 = max(0.,(.5-.5*cos(k2PI * angleFract))*1./max(0.6,2.*adist*angleDist));
	float white = white1*white2;
	vec3 color = vec3(0.0);
	color.r = .03*white1 + white*(0.3 + 5.0*angleDist);
	color.b = white*(0.1 + .5*angleToRandZ);
	color.g = 1.5*white;
	gl_FragColor = vec4( color, 1.0);
}
