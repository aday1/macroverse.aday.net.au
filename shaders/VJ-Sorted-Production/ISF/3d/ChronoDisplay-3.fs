/*{
    "DESCRIPTION": "ChronoDisplay-3",
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
        }
    ],
    "TAGS": [
        "3d"
    ]
}*/




#define time ((useFrameIndex ? (float(FRAMEINDEX) / fps) : TIME))
#define mouse vec2(0.0)
#define resolution RENDERSIZE
/*
  Daily an hour GLSL sketch by @chimanaco 20/30
  
  We are going to go watch Kraftwerk 3-D Concert tonight!
  Visual for Radio Activity might be like this...
*/


#define M_PI 3.1415926535897932384626433832795
#define RAYS 3.0
#define RAYS2 100.0

void main( void ) {

//vec2 p = ( gl_FragCoord.xy / resolution.xy ) - vec2(0.5, 0.5);
  vec2 p = (gl_FragCoord.xy * 2.0 -resolution) / resolution.y;
 
  float r = sqrt(p.x * p.x + p.y * p.y);
  float a = (atan(p.y, p.x) + M_PI) / (2.0 * M_PI);
  a += time * 0.025;

  float c = 1.0;
     
  if(r > 0.065) {
    c = 0.0;
  }
  
  if(r > 0.08) {
    c = mod(floor(a * RAYS * 2.0), 2.0) - r / 4.0;
  }
  
  if(r > 0.28) {
    c = 0.0;
  }
    
  if(r > 0.3) {
    c = mod(floor(a * RAYS2 * 2.0), 2.0) - r / 4.0;
  }
  
  for(int i =0; i < 15; i++) {
    float r1 = 0.3 + float(i) * 0.1 + (sin(time) + 1.0) / 2.;
      if(r > r1) {
      c = 0.0;
    }
	
    float r2 = 0.32 + float(i) * 0.1 + (sin(time) + 1.0) / 2.;
    if(r > r2) {
//	    #define time ((useFrameIndex ? (float(FRAMEINDEX) / fps) : TIME))
	r = log(r+time)*cos(r+time);
      c = mod(floor(a * RAYS2 * 2.0), 2.0) - r / 4.0;
      //c = mod(floor(a * RAYS * (10.0 + sin(time + float(i) ) * 1.)), 2.0) - r / 4.0;
      //c = mod(floor(a * RAYS * ((float(i) * 20. + 10.))), 2.0) - r / 4.0;
    }	 
  }	
	
  gl_FragColor = vec4(vec3(1.0, c, 0.), 1.0 );
}
