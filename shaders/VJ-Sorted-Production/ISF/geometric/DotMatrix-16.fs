/*{
    "DESCRIPTION": "DotMatrix-16",
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
#ifdef GL_ES
precision mediump float;
#endif

#define Pi 3.1415926
vec2 fromPolar( vec2 uv ) //from=euclid
{
	float l = uv.y;
	float a = uv.x * Pi;
	uv = vec2( sin( a ), cos( a ) );
	
	return uv * l;
}
bool grumpy( vec2 uv )
{
	return( mod( uv.x, 0.625 ) < 0.3125 ) ^^
	      ( mod( uv.y, 0.625 ) < 0.3125 ) ;
}

void main( void ) {
	vec2 uv = ( gl_FragCoord.xy / resolution.xy ) * 2. - 1.;
	float background = uv.y;
	uv.x *= resolution.x/resolution.y;
	uv = uv * .5 + .5;
	uv = fromPolar( uv );
	uv.x += .1 * time;
	vec2 p = uv;
	p = mod(p*8., 2.5)-1.25; 
	
	vec2 wob = vec2( cos(time), sin(time) );
	if( grumpy( uv ) )
	p += .25 * wob * sin(time*3.);
	else
	p *= .825 + .025 * sin( (-pow(sin(p.x)+cos(p.y+time), 3.)) * 4.);
	
	vec3 n = vec3( p, cos(length(p)*3.1415926*.5) );
	n = normalize(n);
	vec3 light = 8. * vec3(sin(time*3.)+sin(time*4.)+.1*time,sin(time*.2),sin(time)+1.5);
	vec3 light_n = light - n - 8.*vec3(uv, 0.0);
	light_n = normalize( light_n );

	float f = dot( light_n, n);
	vec4 c1 = vec4( 1.5- clamp(pow(.25,f), 0., 1.) );
	vec4 c2 = vec4( n.x*.5+.5,n.y*.5+.5,n.z*.5+.5,1.);
	if( length(p) < 1.0)
	gl_FragColor = (c1*c2)*(vec4(!grumpy( uv ),0,0,1)+.333);
	else
	gl_FragColor = vec4(smoothstep(2.,-4.,background) );
}
