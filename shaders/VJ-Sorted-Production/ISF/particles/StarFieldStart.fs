/*{
    "DESCRIPTION": "StarFieldStart",
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
        }
    ],
    "TAGS": [
        "color",
        "space",
        "particles"
    ]
}*/



#define time ((useFrameIndex ? (float(FRAMEINDEX) / fps) : TIME) * timeScale)
#define mouse vec2(mouseX, mouseY)
#define resolution RENDERSIZE
#ifdef GL_ES
precision mediump float;
#endif

const vec3 SKY = vec3( 0.1, 0.2, 0.4 );

float hash(vec2 p) {
 return fract(sin(dot(p*0.05, vec2(14.52, 76.38)))*43256.2895-(time*0.0001));   
}

float noise(vec2 pos) {
  vec2 a = vec2(1.0, 0.0);
  vec2 p = floor(pos);
  vec2 f = fract(pos);
  f = f*f*f*(3.0-2.0*f);
    float h = mix(mix(hash(p+a.yy), hash(p+a.xy), f.x), 
                  mix(hash(p+a.yx), hash(p+a.xx), f.x), f.y);
    return h;
}

float snoise(vec2 p) {
   float h = 0.0;
    float a = 0.5;
    for (float i=0.0;i<3.0;i++) {
        h+=noise(p)*a;
        p*=1.9;
        a*=0.4;
    } 
    return h;
}

float snoiser(vec2 p) {
   float h = 0.0;
    float a = 0.5;
    for (float i=0.0;i<3.0;i++) {
        h+= abs(noise(p)-0.5)*a*2.0;
        p*=2.5;
        a*=0.7;
    } 
    return h;
}

void main( void ) {

    vec2 uv = ( gl_FragCoord.xy / (resolution.xy)) * 2.0 - 1.0;
    uv.x *= resolution.x /  resolution.y;
	// uv.x -= time;
    
    vec3 dir = normalize(vec3(uv, 1.0))-time;

    vec3 col = vec3(0.0);

    //stars  
    col = vec3(smoothstep(0.9+0.1*smoothstep(0.0, 0.035, abs((uv.x*0.5)*resolution.y/resolution.x+uv.y)), 4.0, hash(gl_FragCoord.xy))*hash(gl_FragCoord.xy*2.0));

    //sky gradient
    col += SKY*(abs(uv.y-1.2)*0.4);

    //milky way
    //inner glow
    col += mix(vec3(1.0, 0.2, 0.7), col,0.5+0.5*smoothstep(0.0,fract(abs(sin(sqrt(time))-atan(time*0.5))+1.0), abs((uv.x*0.5-1.0)*resolution.y/resolution.x+uv.y)*snoise(5.0*(uv*vec2(resolution.y/resolution.x, 0.0)-vec2(1.0, -uv.y)))));
    
    //outer shape
 //  col *= mix(SKY*0.2, col,0.8+0.2*smoothstep(0.0, 0.5, abs((uv.x*0.5-1.0)*resolution.y/resolution.x+uv.y)*snoise(4.0*(uv*vec2(resolution.y/resolution.x, time)-vec2(1.0, -uv.y)))));
	
    //milky way clouds
 //   col *= mix(SKY*(abs(uv.y*5.0)*0.4), col, 0.5-0.5*smoothstep(0.2,abs(hash(vec2(exp(time*0.0005)+1.0,(cos(time*0.00005))))), abs((uv.x*5.5)*resolution.y/resolution.x+uv.y)*snoiser(5.0*(uv*vec2(resolution.y/resolution.x, 1.0)-vec2(1.0,uv.y)))));
   col *= mix(SKY*(abs(uv.x-2.0)*0.4), col, smoothstep(0.0, 0.3, abs((uv.x*0.5-time)*resolution.y/resolution.x+uv.y)*0.02+0.03*snoiser(15.0*(uv*vec2(resolution.y/resolution.x, 0.0)-vec2(1.0, -uv.y-time)))));

    //add nearby stars
    col += vec3(smoothstep(0.99, 1.0, hash(gl_FragCoord.xy))*hash(gl_FragCoord.xy*2.0));

    gl_FragColor = vec4(col,1.0);
}


