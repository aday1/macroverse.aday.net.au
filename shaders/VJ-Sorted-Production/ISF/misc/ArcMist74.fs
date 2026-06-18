/*{
    "DESCRIPTION": "ArcMist74",
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

vec3 tex(vec2 position)
{
	// from http://glsl.heroku.com/e#8558.0
	float sum = 0.;
	float qsum = 0.;
	
	for (float i = 0.; i < 5.; i++) {
		float x2 = i*i*.3165+(250.*i*0.01)+.5;
		float y2 = i*.161235+(250.*i*0.01)+.5;
		vec2 p = (fract(position-vec2(x2,y2))-vec2(.5));
		float a = atan(p.y,p.x);
		float r = length(p)*200.;
		float e = exp(-r*.8);
		sum += sin(r+a+time)*e;
		qsum += e;
	}
	
	float color = sum/qsum;
	return step( 0.25, vec3( color, color-.5, -color ) );
}

void main( void ) {

	vec2 p = ( gl_FragCoord.xy / resolution.xy )*2.0-1.0;
	
	vec2 sp = mouse-0.5;
	
	vec2 move = vec2(0.2,0.6);

	vec2 uv = vec2(p.x/abs(p.y)+time*move.x,1./(abs(p.y))+time*move.y);
	
	vec3 color = tex(uv*.1);
	color *= vec3(pow(p.y,0.75));
	
	gl_FragColor = vec4(color, 1.0 );

}
