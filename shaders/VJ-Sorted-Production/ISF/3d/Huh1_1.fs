/*{
    "DESCRIPTION": "Huh1",
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
            "NAME": "speed",
            "TYPE": "float",
            "DEFAULT": 1.0,
            "MIN": 0.0,
            "MAX": 5.0,
            "LABEL": "Speed"
        },
        {
            "NAME": "mouseX",
            "TYPE": "float",
            "DEFAULT": 0.0,
            "MIN": -1.0,
            "MAX": 1.0,
            "LABEL": "Mouse X"
        },
        {
            "NAME": "mouseY",
            "TYPE": "float",
            "DEFAULT": 0.0,
            "MIN": -1.0,
            "MAX": 1.0,
            "LABEL": "Mouse Y"
        },
        {
            "NAME": "zoom",
            "TYPE": "float",
            "DEFAULT": 1.0,
            "MIN": 0.1,
            "MAX": 4.0,
            "LABEL": "Zoom"
        },
        {
            "NAME": "colorR",
            "TYPE": "float",
            "DEFAULT": 1.0,
            "MIN": 0.0,
            "MAX": 2.0,
            "LABEL": "Color Red"
        },
        {
            "NAME": "colorG",
            "TYPE": "float",
            "DEFAULT": 1.0,
            "MIN": 0.0,
            "MAX": 2.0,
            "LABEL": "Color Green"
        },
        {
            "NAME": "colorB",
            "TYPE": "float",
            "DEFAULT": 1.0,
            "MIN": 0.0,
            "MAX": 2.0,
            "LABEL": "Color Blue"
        },
        {
            "NAME": "brightness",
            "TYPE": "float",
            "DEFAULT": 0.0,
            "MIN": -1.0,
            "MAX": 1.0,
            "LABEL": "Brightness"
        },
        {
            "NAME": "saturation",
            "TYPE": "float",
            "DEFAULT": 1.0,
            "MIN": 0.0,
            "MAX": 3.0,
            "LABEL": "Saturation"
        },
        {
            "NAME": "contrast",
            "TYPE": "float",
            "DEFAULT": 1.0,
            "MIN": 0.0,
            "MAX": 3.0,
            "LABEL": "Contrast"
        },
        {
            "NAME": "hueShift",
            "TYPE": "float",
            "DEFAULT": 0.0,
            "MIN": 0.0,
            "MAX": 1.0,
            "LABEL": "Hue Shift"
        },
        {
            "NAME": "invert",
            "TYPE": "bool",
            "DEFAULT": 0,
            "LABEL": "Invert Colors"
        }
    ],
    "TAGS": [
        "3d"
    ]
}*/



#define time ((useFrameIndex ? (float(FRAMEINDEX) / fps) : TIME) * speed)
#define mouse vec2(mouseX, mouseY)
#define resolution (RENDERSIZE / zoom)
#ifdef GL_ES
precision highp float;
#endif

//this is a proof of concept
//this is using an algorithm that works on the bit level of integers so that
//if written to a lossy output such as a 32 bit - 4 component texture, the output
//would be the exact same as what was origionally written. Precision is the goal.
//Unfortunantly, this algorithm is not perfect. The visuals given by this shader
//is the absolute difference between the source float and the output float
//multiplied by 100. The magin of error is very small but it is not good enough.

//UPDATE: I applied an offset in a spot I should have not. The innacuracy issues
//are gone now. Tesing this algorithm with shadow maps reveald that this algorithm
//is slower then other solutions, but is by far the most accurate. Setting the
//near view of 0.1 and a far view of 1,000,000 (when a far view of 10 would still be
//adequite) showed no degredation of the shadow with no sign of peter panning
//I used a cube map with a point light approximately 6 units away from my object
//with a resolution of 1024x1024 pixels on each 6 textures to make up the cube map.

int packFloat(inout float f, int off){
    int o = 0;
    
    for (int i = 0; i < 12; i++){
        float mul = 1. / exp2(float(i + off + 1));
        
        if (f >= mul){
            f -= mul;
            
            o += int(exp2(float(i)));
        }
    }
    
    return o;
}

vec4 packFloat4(float f){
    return vec4(
        float(packFloat(f,  0)) / 255.,
        float(packFloat(f,  8)) / 255.,
        float(packFloat(f, 16)) / 255.,
        float(packFloat(f, 24)) / 255.
    );
}

float unpackFloat(int f, int off){
    float o = 0.;
    
    for (int i = 0; i < 8; i++){
        if (bool(mod(floor(float(f) / exp2(float(i))), 2.))){
            o += 1. / exp2(float(i + off + 1));
        }
    }
    
    return o;
}

float unpackFloat4(vec4 value){
    return
        unpackFloat(int(value.x * 255.),  0) + 
        unpackFloat(int(value.y * 255.),  8) + 
        unpackFloat(int(value.z * 255.), 16) + 
        unpackFloat(int(value.w * 255.), 24);
}

void _userMain( void ) {
	float f = sin (gl_FragCoord.y / resolution.x * 20. + time * 5.) / 2. + .5;
	
	vec4 pack = packFloat4(f);
	
	//artifitially introduce a lossy output
	pack = floor(clamp(pack, 0., 1.) * 255.) / 255.;
	
	float o = unpackFloat4(pack);
	
	if (gl_FragCoord.y / resolution.y > 1.0){
		gl_FragColor = vec4(vec3(o), 1);	
	}else if (gl_FragCoord.y / resolution.y < 0.00){
		gl_FragColor = vec4(vec3(abs(o ) * 100.), 1);	
	}else{
		gl_FragColor = vec4(pack.xyz, 1);
	}
	
}

void main() {
    _userMain();
    vec3 c = gl_FragColor.rgb;
    float a = gl_FragColor.a;
    float luma = dot(c, vec3(0.299, 0.587, 0.114));
    c = mix(vec3(luma), c, saturation);
    c = (c - 0.5) * contrast + 0.5;
    c *= vec3(colorR, colorG, colorB);
    c += brightness;
    if (hueShift > 0.001) {
        float cosH = cos(hueShift * 6.28318);
        float sinH = sin(hueShift * 6.28318);
        c = vec3(
            c.r * (0.299 + 0.701*cosH + 0.168*sinH) + c.g * (0.587 - 0.587*cosH + 0.330*sinH) + c.b * (0.114 - 0.114*cosH - 0.497*sinH),
            c.r * (0.299 - 0.299*cosH - 0.328*sinH) + c.g * (0.587 + 0.413*cosH + 0.035*sinH) + c.b * (0.114 - 0.114*cosH + 0.292*sinH),
            c.r * (0.299 - 0.300*cosH + 1.250*sinH) + c.g * (0.587 - 0.588*cosH - 1.050*sinH) + c.b * (0.114 + 0.886*cosH - 0.203*sinH)
        );
    }
    if (invert) c = 1.0 - c;
    gl_FragColor = vec4(clamp(c, 0.0, 1.0), a);
}