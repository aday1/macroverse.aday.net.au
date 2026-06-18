/*{
    "DESCRIPTION": "EchoDance98",
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
#ifdef GL_ES
precision mediump float;
#endif

#define PI 3.1415927

float r = 0.003;
float speed = 0.5;
float tail = 0.3;
vec3 col =  vec3(1.0, 0.2, 0.2);
vec3 color ; 
vec2 center(float k) {
	vec2 cen = vec2(0.0);
	cen.x = fract(k * time);
	cen.y = 0.5;
	return cen;
}

void main( void ) {
	
	vec2 position =  gl_FragCoord.xy / resolution.x ;
	 
	float aspect = resolution.x / resolution.y;
	vec2 c = center(speed);
	c.y  /=  aspect;
	float d = distance(position , c);
	if( d < r) {
		color = col;
	}else if( d>=r && c.x > position.x && abs(position.y - c.y) < r){
		color  = col * ( max ( 0.8 - min ( pow ( d - r , tail ) , 0.9 ) , -0.2 ) );
	}else{
		color = vec3(0.0);
	}	

	gl_FragColor = vec4( color , 1.0 );

}
