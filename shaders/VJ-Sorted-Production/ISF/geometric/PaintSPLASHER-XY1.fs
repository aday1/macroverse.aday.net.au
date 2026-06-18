/*{
    "DESCRIPTION": "PaintSPLASHER-XY1",
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
        "geometric",
        "texture-input"
    ]
}*/



#define time ((useFrameIndex ? (float(FRAMEINDEX) / fps) : TIME) * timeScale)
#define mouse vec2(mouseX, mouseY)
#define resolution RENDERSIZE
// Conway's game of life

#ifdef GL_ES
precision highp float;
#endif

uniform sampler2D backbuffer;

vec4 live = vec4(1.0,.1,1.,1.);
vec4 dead = vec4(0.,0.,0.,1.);
vec4 blue = vec4(1.,0.,1.,1.);

void main( void ) {
	vec2 position = ( gl_FragCoord.xy / resolution.xy );
	vec2 pixel = 1./resolution;

	if (length(position-mouse) < 0.01) {
		float rnd1 = mod(fract(sin(dot(position + time * 0.001, vec2(14.9898,78.233))) * 43758.5453), 1.0);
		if (rnd1 > 0.5) {
			gl_FragColor = live;
		} else {
			gl_FragColor = blue;
		}
	} else {
		float sum = 0.;
		sum += texture2D(backbuffer, position + pixel * vec2(-1., -1.)).g;
		sum += texture2D(backbuffer, position + pixel * vec2(-1., 0.)).g;
		sum += texture2D(backbuffer, position + pixel * vec2(-1., 1.)).g;
		sum += texture2D(backbuffer, position + pixel * vec2(1., -1.)).g;
		sum += texture2D(backbuffer, position + pixel * vec2(1., 0.)).g;
		sum += texture2D(backbuffer, position + pixel * vec2(1., 1.)).g;
		sum += texture2D(backbuffer, position + pixel * vec2(0., -1.)).g;
		sum += texture2D(backbuffer, position + pixel * vec2(0., 1.)).g;
		vec4 me = texture2D(backbuffer, position);

		if (me.g <= 0.01) {
			if ((sum >= 0.29) && (sum <= 0.31)) {
				gl_FragColor = live;
			} else if (me.r > 0.004) {
				gl_FragColor = vec4(me.r-0.004, 0.0, 0.8, 0.);
			} else if (me.b > 0.004) {
				gl_FragColor = vec4(0.0, 0.0, me.b - 0.004, 0.);
			} 
			else {
				gl_FragColor = dead;
			}
		} else {
			if ((sum >= 0.19) && (sum <= 0.31)) {
				gl_FragColor = live;
			} else {
				gl_FragColor = blue;
			}
		}
	}
}
