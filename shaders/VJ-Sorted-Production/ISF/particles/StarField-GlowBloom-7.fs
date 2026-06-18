/*{
    "DESCRIPTION": "StarField-GlowBloom-7",
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
        "particles",
        "texture-input"
    ]
}*/




#define time ((useFrameIndex ? (float(FRAMEINDEX) / fps) : TIME) * speed)
#define mouse vec2(0.0)
#define resolution RENDERSIZE

// StarTrip

#ifdef GL_ES
precision mediump float;
#endif

uniform sampler2D backbuffer;
#define speed (25.2)
#define k2PI (2.*3.14159265359)
#define kStarDensity .011
#define kMotionBlur 0.4
#define kNumAngles 90. + sin(time)*30.

void main( void )
{
	vec2 position = ( gl_FragCoord.xy -  resolution.xy*.5 ) / resolution.x;
	position -= 0.5*(mouse - 0.5)*vec2(1.,resolution.y / resolution.x); // use this for mouse panning
	float A = atan(position.y,position.x);
	float angle0 = A / k2PI;
	float angle = fract(angle0 + .002*time);
	float rad = .3*length(position);
	float angleFract = fract(angle*kNumAngles);
	float angleStep = floor(angle*kNumAngles);
	float angleToRandZ = 10.*fract(angleStep*fract(angleStep*.7535)*45.1);
	float angleSquareDist = fract(angleStep*fract(angleStep*.82657)*13.724);
	float t = speed * time - angleToRandZ;
	float angleDist = (angleSquareDist+0.1);
	float adist = angleDist/rad*kStarDensity;
	float dist = abs(fract((t*.1+adist))-.5);
	float white1 = max(0.,1.0 - dist * 100.0 / (kMotionBlur*speed+adist));
	float white2 = max(0.,(.5-.5*cos(k2PI * angleFract))*1./max(0.6,2.*adist*angleDist));
	float white = white1*white2;
	vec3 color;
	color.r = .03*white1 + white*(0.3 + 5.0*angleDist);
	color.b = white*(0.1 + .5*angleToRandZ);
	color.g = 1.5*white;
	
	float nebD1 = 1.0/rad + 4.5*(1.0 + sin(1.1 + 3.0*A + 0.71*cos(2.0*A)));
	float nebD2 = 1.0/rad + 3.7*(1.0 + sin(3.7 + 2.0*A + 0.46*sin(3.0*A)));
	float R1 = 1.0 * rad * (1.0 + sin(0.3+3.0*A + 2.4 * cos(0.2+3.0*A)*sin(2.1+0.42*(nebD1+speed*time)) + sin(2.0*6.283*position.x) ));
	float R2 = 1.0 * rad * (1.0 + sin(1.1+4.0*A + 3.2 * cos(0.7+4.0*A)*sin(1.7+0.27*(nebD2+speed*time)) + cos(3.0*6.283*position.y) ));
	float P1 = 0.5 + .5*sin(5.7*position.x+.22*(speed*time));
	float P2 = 0.5 + .5*sin(4.44*position.y+.17*(speed*time)) ;
	color.r += 0.6*R1 + 0.3*R2 + 0.1*P1*P2 ;
	color.b += 0.3*R1 + 0.8*R2 + .1*P2*R1;
	color.g += 1.1*R1*R2*P2;
	gl_FragColor = vec4( (color.grb+ vec3(texture2D(backbuffer, (gl_FragCoord.xy /  resolution.xy))))/1.1, 1.0);
}
