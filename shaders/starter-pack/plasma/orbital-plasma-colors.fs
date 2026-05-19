/*{
    "DESCRIPTION": "orbital plasma colors",
    "CREDIT": "GLSL Sandbox / various",
    "ISFVSN": "2.0",
    "CATEGORIES": [
        "plasma"
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
        "plasma"
    ]
}*/



#define time ((useFrameIndex ? (float(FRAMEINDEX) / fps) : TIME) * timeScale)
#define mouse vec2(mouseX, mouseY)
#define resolution RENDERSIZE
#ifdef GL_ES
precision mediump float;
#endif

// http://www.bidouille.org/prog/plasma

#define PI 3.1415926535897932384626433832795
 
vec2 scale = resolution.xy / 10.0;
float time_scaled = time * mouse.x;
 
void main() {
    vec2 position = ( gl_FragCoord.xy / resolution.xy );
    vec2 c = position * scale - scale/2.0;
    
    // sins build up
    float v = 0.0;
    v += sin((c.x + time_scaled));
    v += sin((c.y + time_scaled) / 41.0);
    v += sin((c.x + c.y + time_scaled) / 2.0);
    c += scale / 1.0 * vec2(sin(time_scaled / 1.0), cos(time_scaled / 1.0));
    v += sin(sqrt(c.x * c.x + c.y * c.y + 1.0) + time_scaled);
    v = v / 3.0;
	
    float r = sin(v * PI + 5.0 * PI / 6.0);
    float g = sin(v * PI + 7.0 * v * PI);(v * PI + 11.0 * PI / 7.0);
    float b = sin(v * PI + 23.0 * PI / 17.0);
    vec3 col = vec3(r, g, b) * 0.9 + 0.2;
    gl_FragColor = vec4(col, 1.0);
}
