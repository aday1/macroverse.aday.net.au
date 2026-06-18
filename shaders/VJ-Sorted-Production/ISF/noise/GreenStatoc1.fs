/*{
    "DESCRIPTION": "GreenStatoc1",
    "CREDIT": "GLSL Sandbox / various",
    "ISFVSN": "2.0",
    "CATEGORIES": [
        "noise"
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
        "noise",
        "texture-input"
    ]
}*/



#define time ((useFrameIndex ? (float(FRAMEINDEX) / fps) : TIME))
#define mouse vec2(0.0)
#define resolution RENDERSIZE
#ifdef GL_ES
precision mediump float;
#endif

// An attempt to simulate a nice chemical reaction I saw somewhere... - kabuto

uniform sampler2D backbuffer;

float kernel[9];
vec2 offset[9];

float k1 = 0.03;
float k2 = 0.07;
float f1 = 0.0;
float f2 = 0.1;

float noise2D(vec2 uv)
{
	uv = fract(uv)*1e3;
	vec2 f = fract(uv);
	uv = floor(uv);
	float v = uv.x+uv.y*1e3;
	vec4 r = vec4(v, v+1., v+1e3, v+1e3+1.);
	r = fract(1e5*sin(r*1e-2*time));
	f = f*f*(3.0-2.0*f);
	return (mix(mix(r.x, r.y, f.x), mix(r.z, r.w, f.x), f.y));	
}

void main( void ) {
	
	vec2 pos   = gl_FragCoord.xy / resolution;
	vec2 pix   = gl_FragCoord.xy;
	
	kernel[0] = 0.707106781;
	kernel[1] = 1.0;
	kernel[2] = 0.707106781;
	kernel[3] = 1.0;
	kernel[4] = -6.82842712;
	kernel[5] = 1.0;
	kernel[6] = 0.707106781;
	kernel[7] = 1.0;
	kernel[8] = 0.707106781;
	
	offset[0] = vec2( -1.0, -1.0);
	offset[1] = vec2(  0.0, -1.0);
	offset[2] = vec2(  1.0, -1.0);
	
	offset[3] = vec2( -1.0, 0.0);
	offset[4] = vec2(  0.0, 0.0);
	offset[5] = vec2(  1.0, 0.0);
	
	offset[6] = vec2( -1.0, 1.0);
	offset[7] = vec2(  0.0, 1.0);
	offset[8] = vec2(  1.0, 1.0);
				       
	vec2 back = texture2D( backbuffer, pos ).rb;

	vec2 lap = vec2( 0.0, 0.0 );
				       
	for( int i=0; i < 9; i++ ){
	   vec2 tmp = texture2D( backbuffer, (pix + offset[i])/resolution ).rb;
	   lap += tmp * kernel[i];
	}
       
       	float K = k1 + (k2-k1)*pos.x*0.0;
       	float F = f1 + (f2-f1)*pos.y*0.0;
	
	float diffU = 0.2;
	float diffV = 0.1;
	
       	float u = back.r;
       	float v = back.g;
       
      	float uvv = u * v * v;
       
       	float du = diffU * lap.r - uvv + F * (1.0 - u);
       	float dv = diffV * lap.g + uvv - (F + K) * v;
       
       	u += du * 0.6;
       	v += dv * 0.6;

	float noise = noise2D(pos)*mouse.x;
	
	//float cu = clamp( u, 0.0, 1.0 ) ;
	//float cv = clamp( v, 0.0, 1.0 ) + noise;
	
	float cu = u + noise*0.001;
	float cv = v + noise;
	
	gl_FragColor = vec4(cu,cv,0.0, 1.0);
}
