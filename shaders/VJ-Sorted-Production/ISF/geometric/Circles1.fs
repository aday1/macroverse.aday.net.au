/*{
    "DESCRIPTION": "Circles1",
    "CREDIT": "GLSL Sandbox / various",
    "ISFVSN": "2.0",
    "CATEGORIES": [
        "geometric"
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
        "geometric"
    ]
}*/



#define time ((useFrameIndex ? (float(FRAMEINDEX) / fps) : TIME) * timeScale)
#define mouse vec2(mouseX, mouseY)
#define resolution RENDERSIZE
//co3moz 

#ifdef GL_ES
precision mediump float;
#endif

vec2 center = vec2(0.5, 0.5);

#define silindir(dis, ic) if(distance(pos, center) < dis && distance(pos, center) > ic)
#define silindir_cap(dis, genislik) silindir(dis, dis - genislik)
#define dondur(neyi, nekadar) temp = neyi.x; neyi.x = neyi.x * cos(nekadar) - neyi.y * sin(nekadar); neyi.y = temp * sin(nekadar) + neyi.y * cos(nekadar)
#define dongu(i, start, step, max) for(float i = start; i<max; i+=step)

void main() {
	float temp;
	vec2 pos = (gl_FragCoord.xy / resolution.xy);
	vec3 color;
	
	pos /= normalize(pos);
	
	dondur(pos, sin(time)/10.);
	
	color.x = sin(pos.x);
	color.y = cos(pos.y);
	color.z = cos(pos.x/pos.y);

	dongu(i, 0.01, 0.01, 0.9) {
		silindir_cap(0.4 - i * (8. * abs(sin(time) + 2.) / 2.), i) {
			color = vec3(1);	
		}
	}
	
	color *= vec3(normalize(pos), 1);
	gl_FragColor = vec4(color, 1); 
}
