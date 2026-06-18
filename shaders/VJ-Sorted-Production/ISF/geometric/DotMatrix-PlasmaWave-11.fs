/*{
    "DESCRIPTION": "DotMatrix-PlasmaWave-11",
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
        }
    ],
    "TAGS": [
        "geometric"
    ]
}*/




#define time ((useFrameIndex ? (float(FRAMEINDEX) / fps) : TIME))
#define mouse vec2(0.0)
#define resolution RENDERSIZE

void main( void ) {

	vec2 position = ( gl_FragCoord.xy / resolution.xy )-0.5;
	vec2 mpos = mouse - 0.5;
	
	position.x = abs(position.x);
	position.y = abs(position.y);
	float rTime = time * 0.08123213;
	position = vec2(position.x*cos(rTime) - position.y*sin(rTime), position.x*sin(rTime) + position.y*cos(rTime));
	
	float localTime = 0.01*time + pow(position.y*64.*mpos.y, mpos.y*2.) + pow(position.x*64.*mpos.x, mpos.x*2.);
	
	position = vec2(position.x*cos(localTime) - position.y*sin(localTime), position.x*sin(localTime) + position.y*cos(localTime));
	
	vec2 c = position - vec2(0.5, 0.5);
	float dist = 100.0;
	for(int i=0;i<2;i++) {
		c = (1.0 - fract(c - 0.5)*2.0);
		c = atan(c)/dot(c,c);
		
		c+= localTime*0.19;
	
		//c *= 0.10;
		//c = clamp(c, 0.0, 1.0);
		
		//c = position*3.0;

		//dist = min(dist, length( 1.0 - fract(c.y - 0.5)*2.0 ));
		//dist = min(dist, length( 1.0 - fract(c.x - 0.5)*2.0 ));
		
	}
	dist = sin(length(c))*0.5;
	
	float color = 0.06 * 1.0 / dist;/*
	color += sin( position.x * cos( localTime / 15.0 ) * 80.0 ) + cos( position.y * cos( localTime / 15.0 ) * 10.0 );
	color += sin( position.y * sin( localTime / 10.0 ) * 40.0 ) + cos( position.x * sin( localTime / 25.0 ) * 40.0 );
	color += sin( position.x * sin( localTime / 5.0 ) * 10.0 ) + sin( position.y * sin( localTime / 35.0 ) * 80.0 );
	color *= sin( localTime / 10.0 ) * 0.5;*/

	gl_FragColor = vec4( vec3( color, color * 0.5, sin( color + localTime / 3.0 ) * 0.75 ), 1.0 );

}
