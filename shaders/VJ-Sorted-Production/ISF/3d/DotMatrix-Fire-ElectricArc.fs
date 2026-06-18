/*{
    "DESCRIPTION": "DotMatrix-Fire-ElectricArc",
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
        "color",
        "geometric",
        "3d"
    ]
}*/




#define time ((useFrameIndex ? (float(FRAMEINDEX) / fps) : TIME) * speed)
#define mouse vec2(mouseX, mouseY)
#define resolution (RENDERSIZE / zoom)

//http://www.youtube.com/watch?v=rOoCixFA8OI - dance, velvet

#ifdef GL_ES
precision mediump float;
#endif

vec3 dc;

float map(vec3 p)
{
    const int MAX_ITER = 8;
    const float BAILOUT=10.0;
    float Power= sin(time*.1)*8.0+10.;

    vec3 v = p;
    vec3 c = v;

    float r=0.0;
    float d=1.0;
    for(int n=0; n<=MAX_ITER; ++n)
    {
        r = length(v);
        if(r>BAILOUT) break;

        float theta = acos(v.z/r);
        float phi = atan(v.y, v.x);
        d = abs(pow(r,Power-1.0)*Power*d)+1.0;

        float zr = abs(pow(r,Power))-.5;
        theta = abs(theta*Power);
        phi = abs(phi*Power);
        v = (vec3(0.6-sin(theta)*cos(phi), sin(phi)*sin(theta), cos(theta))*sqrt(zr))-c*1.5;
    }
    dc = cos(atan(p-v))*.15;
    return 0.5*log(r)*r/d;
}

void _userMain( void )
{
    vec2 pos = (gl_FragCoord.xy*2.0 - resolution.xy) / resolution.y;
    vec3 camPos = vec3(cos(time*0.3), sin(time*0.3), 2.5);
    vec3 camTarget = vec3(0., 0., 0.);

    vec3 camDir = normalize(camTarget-camPos);
    vec3 camUp  = normalize(vec3(1.0, 0.0, 0.0));
    vec3 camSide = cross(camDir, camUp);
    float focus = 1.7+cos(time*.3)*.5;

    vec3 rayDir = normalize(camSide*pos.x + camUp*pos.y + camDir*focus);
    vec3 ray = camPos;
    float m = 0.0;
    float d = 0.0, total_d = 0.0;
    const int MAX_MARCH = 48;
    const float MAX_DISTANCE = 32.0;
    vec3 tc;
    ray += rayDir;
    for(int i=0; i<MAX_MARCH; ++i) {
	float s = 1.-float(i)/64.;
	d = map(ray);
        total_d += d * s;
        ray += rayDir * d * s;
        m += 1.0;
	tc += dc * s;
        if(d <= 0.001 || tc.x < 0. || tc.y < 0. || tc.z < 0.) { break; }
        if(total_d > MAX_DISTANCE) { total_d = MAX_DISTANCE; break; }
    }

    float c = (total_d)*0.004;
    tc = pow(tc, .5-vec3(c*c));
    tc = tc*tc-c*m-c;
    vec3 h = clamp(tc+(c*m), 0., 1.) * .5;
    h = h * h + h * h;
    tc = max(tc, h);
    vec4 result = vec4(tc, 1.);
    gl_FragColor = result;
    gl_FragColor.r = fract(gl_FragColor.r + 0.5*gl_FragColor.g*sin(time) + 0.5*gl_FragColor.b*cos(time));
    gl_FragColor.g = fract(gl_FragColor.g + 0.5*gl_FragColor.b*sin(time) + 0.5*gl_FragColor.r*cos(time));
    gl_FragColor.b = fract(gl_FragColor.b + 0.5*gl_FragColor.r*sin(time) + 0.5*gl_FragColor.g*cos(time));
    //sphinx 
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