/*{
    "DESCRIPTION": "ApexSpiral87",
    "CREDIT": "GLSL Sandbox / various",
    "ISFVSN": "2.0",
    "CATEGORIES": [
        "misc"
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
        "misc"
    ]
}*/




#define time ((useFrameIndex ? (float(FRAMEINDEX) / fps) : TIME) * timeScale)
#define mouse vec2(mouseX, mouseY)
#define resolution RENDERSIZE
#ifdef GL_ES
precision mediump float;
#endif

void main( void ) {

	vec2 position = ( gl_FragCoord.xy / resolution.xy );

	vec3 col = vec3(sin(position.y*20.0));
	
	col.rb *= sin(time+position.x*50.0)*15.0;
	col.gb *= sin(-time*2.0+(6.0+position.x)*100.0)*5.0;

	if(position.x < 0.1) {
		float x = position.x*10.0;
		col *= vec3(x,x,x);
	} else if(position.x > 0.9) {
		float x = abs(1.0-position.x)*10.0;
		col *= vec3(x,x,x);
	}

	if(position.x < 0.1) {
		float x = position.x*10.0;
		col *= vec3(x,x,x);
	} else if(position.x > 0.9) {
		float x = abs(1.0-position.x)*10.0;
		col *= vec3(x,x,x);
	}
	
	if(position.y < 0.1) {
		float y = position.y*10.0;
		col *= vec3(y,y,y);
	} else if(position.y > 0.9) {
		float y = abs(1.0-position.y)*10.0;
		col *= vec3(y,y,y);
	}
	
	gl_FragColor = vec4(col,  10.0);
}
