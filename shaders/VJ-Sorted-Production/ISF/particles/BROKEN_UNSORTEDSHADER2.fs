/*{
    "DESCRIPTION": "BROKEN_UNSORTEDSHADER2",
    "CREDIT": "GLSL Sandbox / various",
    "ISFVSN": "2.0",
    "CATEGORIES": [
        "particles"
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
        },
        {
            "NAME": "points",
            "TYPE": "float",
            "MIN": 0.0,
            "MAX": 5.0,
            "DEFAULT": 1.0,
            "LABEL": "Points"
        }
    ],
    "TAGS": [
        "geometric",
        "particles"
    ]
}*/



#define time ((useFrameIndex ? (float(FRAMEINDEX) / fps) : TIME) * timeScale)
#define mouse vec2(mouseX, mouseY)
#define resolution RENDERSIZE


#ifdef GL_ES
precision mediump float;
#endif

 float scale = 0.001;
uniform float points;
uniform float zoom;
#define pi 3.14159
float indent = 0.06;
float angular= 10.0;
float hash( float n )
{
	return fract( (1.0 + cos(n)) * 415.92653);
}
float noise2d( in vec2 x )
{
	float xhash = hash( x.x * 37.0 );
	float yhash = hash( x.y * 57.0 );
	return fract( xhash + yhash );
}
float drawStar(vec2 o,float size,float startAngle){
	vec2 q=o;
	q*=normalize(resolution).xy;
	mat4 RotationMatrix = mat4( cos(startAngle), -sin(startAngle), 0.0, 0.0, sin(startAngle), cos(startAngle), 0.0, 0.0, 0.0, 0.0, 1.0, 0.0, 0.0, 0.0, 0.0, 1.0 );
	q = (RotationMatrix * vec4(q, 0.0, 1.0)).xy;
	float angle=atan( q.y,q.x )/(2.*pi);
	float segment = angle * angular;
	float segmentI = floor(segment);
	float segmentF = fract(segment);
	angle = (segmentI + 0.5) / angular;
	if (segmentF > 0.5) {
		angle -= indent;
	} else
	{
		angle += indent;
	}
	angle *= 2.0 * pi;
	vec2 outline;
	outline.y = sin(angle);
	outline.x = cos(angle);
	float dist = abs(dot(outline, q));
	float ss=size*(1.+0.2*sin(time*hash(size)*20. ) );
	float r=angular*ss;
	float star=smoothstep( r, r+0.005, dist );
	return star;
}
float drawFlare(vec2 o,float size){
	o*=normalize(resolution).xy;
	float flare=smoothstep(0.0,size,length(o) );
	return flare;
}
vec4 mainImage(in vec2 fragCoord)
{
	vec2 iResolution=resolution;
	float iGlobalTime=time; 

	vec2 uv = (( fragCoord.xy / resolution.xy ) * 2.0 - 1.0) / 1.0; 
	//uv*=normalize(resolution).xy; 
	vec3 color=mix(vec3(0.), vec3(0.1,0.2,0.4), uv.y ); 
	float fThreshhold = 0.995;
	float StarVal = noise2d( uv ); 
	if ( StarVal >= fThreshhold )
	{
		StarVal = pow( (StarVal - fThreshhold)/(1.0 - fThreshhold), 6.0 );
		color += vec3( StarVal );
	}
	for (float i=0.; i<100.; i++){

		float t0=i*0.1;
		if (iGlobalTime>t0)
		{
			float t=mod(iGlobalTime-t0,5.5) ;
			float size=1.+3.0*hash(i*10.);
			vec2 pos=uv-vec2( 0.05+0.25*(hash(i)-0.5)*t, -1.0+(0.5 +0.5*hash(i+1.) )*t- .25*t*t ) ;
			color+=mix(vec3(0.05,0.05,0.),vec3(.0),drawFlare(pos,0.05*size) );            
				color=mix( vec3(0.9+hash(i),0.9,0.0), color, drawStar(pos,(0.25*scale)*size, pi*hash(i+1.) ) );     
			} 
		} 
		return vec4( color,1.0); 
	} 
void main( void ) { 
	vec4 color=mainImage(gl_FragCoord.xy / 1.0 ); 	
	gl_FragColor=color; 
} 
