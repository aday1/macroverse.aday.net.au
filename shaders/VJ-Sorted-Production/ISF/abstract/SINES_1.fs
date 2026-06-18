/*{
    "DESCRIPTION": "SINES",
    "CREDIT": "GLSL Sandbox / various",
    "ISFVSN": "2.0",
    "CATEGORIES": [
        "abstract"
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
            "NAME": "speed",
            "TYPE": "float",
            "DEFAULT": 1.0,
            "MIN": 0.0,
            "MAX": 5.0,
            "LABEL": "Speed"
        },
        {
            "NAME": "mouseX",
            "TYPE": "float",
            "DEFAULT": 0.0,
            "MIN": -1.0,
            "MAX": 1.0,
            "LABEL": "Mouse X"
        },
        {
            "NAME": "mouseY",
            "TYPE": "float",
            "DEFAULT": 0.0,
            "MIN": -1.0,
            "MAX": 1.0,
            "LABEL": "Mouse Y"
        },
        {
            "NAME": "zoom",
            "TYPE": "float",
            "DEFAULT": 1.0,
            "MIN": 0.1,
            "MAX": 4.0,
            "LABEL": "Zoom"
        },
        {
            "NAME": "colorR",
            "TYPE": "float",
            "DEFAULT": 1.0,
            "MIN": 0.0,
            "MAX": 2.0,
            "LABEL": "Color Red"
        },
        {
            "NAME": "colorG",
            "TYPE": "float",
            "DEFAULT": 1.0,
            "MIN": 0.0,
            "MAX": 2.0,
            "LABEL": "Color Green"
        },
        {
            "NAME": "colorB",
            "TYPE": "float",
            "DEFAULT": 1.0,
            "MIN": 0.0,
            "MAX": 2.0,
            "LABEL": "Color Blue"
        },
        {
            "NAME": "brightness",
            "TYPE": "float",
            "DEFAULT": 0.0,
            "MIN": -1.0,
            "MAX": 1.0,
            "LABEL": "Brightness"
        },
        {
            "NAME": "saturation",
            "TYPE": "float",
            "DEFAULT": 1.0,
            "MIN": 0.0,
            "MAX": 3.0,
            "LABEL": "Saturation"
        },
        {
            "NAME": "contrast",
            "TYPE": "float",
            "DEFAULT": 1.0,
            "MIN": 0.0,
            "MAX": 3.0,
            "LABEL": "Contrast"
        },
        {
            "NAME": "hueShift",
            "TYPE": "float",
            "DEFAULT": 0.0,
            "MIN": 0.0,
            "MAX": 1.0,
            "LABEL": "Hue Shift"
        },
        {
            "NAME": "invert",
            "TYPE": "bool",
            "DEFAULT": 0,
            "LABEL": "Invert Colors"
        }
    ],
    "TAGS": [
        "abstract"
    ]
}*/



#define time ((useFrameIndex ? (float(FRAMEINDEX) / fps) : TIME) * speed)
#define mouse vec2(mouseX, mouseY)
#define resolution (RENDERSIZE / zoom)
#ifdef GL_ES
precision highp float;
#endif

float aspect = resolution.x / resolution.y;
 
float function( float x ) {
  return sin(time+pow(0.00001*x,-1./x))*sin(x);
//  return sin(x*x*x)*sin(x) + 0.1*sin(x*x);
//  return sin(x);
}
 
//note: does one sample per x, thresholds on distance in y
float discreteEval( vec2 uv ) {
  const float threshold = 0.015;
  float x = uv.x;
  float fx = function( x );
  float dist = abs( uv.y - fx );
  float hit = step( dist, threshold );
  return hit;
}
 
//note: samples graph by checking multiple samples being above / below function
//original from http://blog.hvidtfeldts.net/index.php/2011/07/plotting-high-frequency-functions-using-a-gpu/
float stochEval( vec2 uv ) {
  const int samples = 255; //note: on AMD requires 255+ samples, should be ~50
  const float fsamples = float(samples);
  vec2 maxdist = 0.075 * vec2( aspect, 1.0 );
  vec2 stepsize = maxdist / vec2(samples);
  float count = 0.0;
  vec2 initial_offset = - 0.5 * fsamples * stepsize;
  uv += initial_offset;
  for ( int ii = 0; ii<samples; ii++ ) {
    float i = float(ii);
    float fx = function( uv.x + i*stepsize.x );
    for ( int jj = 0; jj<samples; jj++ ) {
      float j = float(jj);
      float diff =  fx - float(uv.y + j*stepsize.y);
      count = count + step(0.0, diff) * 2.0 - 1.0;
    }
  }
  return 1.0 - abs( count ) / float(samples*samples);
}
 
//note: averages distances over multiple samples along x, result is identical to superEval
float distAvgEval( vec2 uv ) {
  const int samples = 255; //note: on AMD requires 255+ samples, should be ~50
  const float fsamples = float(samples);
  vec2 maxdist = 0.075 * vec2( aspect, 1.0 );
  vec2 halfmaxdist = 0.5 * maxdist;
  float stepsize = maxdist.x / fsamples;
  float initial_offset_x = -0.5*fsamples * stepsize;
  uv.x += initial_offset_x;
  float hit = 0.0;
  for( int i=0; i<samples; ++i ) {
    float x = uv.x + stepsize * float(i);
    float y = uv.y;
    float fx = function( x );
    float dist = ( y - fx );
    float vt = clamp( dist / halfmaxdist.y -1.0, -1.0, 1.0 );
    hit += vt;
  }
  return 1.0 - abs(hit) / fsamples;
}
 
//note: does multiple thresholded samples
float proxyEval( vec2 uv ) {
  const int samples = 255; //note: on AMD requires 255+ samples, should be ~50
  const float fsamples = float(samples);
  vec2 maxdist = vec2(0.05) * vec2( aspect, 1.0 );
  vec2 halfmaxdist = vec2(0.5) * maxdist;
  float stepsize = maxdist.x / fsamples;
  float initial_offset_x = -0.5 * fsamples * stepsize;
  uv.x += initial_offset_x;
  float hit = 0.0;
  for( int i=0; i<samples; ++i ) {
    float x = uv.x + stepsize * float(i);
    float y = uv.y;
    float fx = function( x );
    float dist = abs( y - fx );
    hit += step( dist, halfmaxdist.y );
  }
  const float arbitraryFactor = 3.5; //note: to increase intensity
  const float arbitraryExp = 0.95;
  return arbitraryFactor * pow( hit / fsamples, arbitraryExp );
}

void _userMain(void)
{
  vec2 uv_norm = gl_FragCoord.xy / resolution.xy;
  vec4 dim = vec4( -2.0, 12.0, -3.0, 3.0 );
  uv_norm = (uv_norm ) * ( dim.yw - dim.xz ) + dim.xz;
 
  //float hitStoch = stochEval( uv_norm - vec2(0,2) );
  float hitDiscr = discreteEval( uv_norm  + vec2(0,2) );
  float hitProximity = proxyEval( uv_norm - vec2(0,2) );
  float hitDistAvgStoch = distAvgEval( uv_norm - vec2(0,0) );
 
 gl_FragColor = vec4( hitDistAvgStoch
                    , 0.8*hitProximity + 0.5*hitDiscr
                    , hitDiscr + 0.2*hitProximity
                    , 1.0);
}

void main() {
    _userMain();
    vec3 c = gl_FragColor.rgb;
    float a = gl_FragColor.a;
    float luma = dot(c, vec3(0.299, 0.587, 0.114));
    c = mix(vec3(luma), c, saturation);
    c = (c - 0.5) * contrast + 0.5;
    c *= vec3(colorR, colorG, colorB);
    c += brightness;
    if (hueShift > 0.001) {
        float cosH = cos(hueShift * 6.28318);
        float sinH = sin(hueShift * 6.28318);
        c = vec3(
            c.r * (0.299 + 0.701*cosH + 0.168*sinH) + c.g * (0.587 - 0.587*cosH + 0.330*sinH) + c.b * (0.114 - 0.114*cosH - 0.497*sinH),
            c.r * (0.299 - 0.299*cosH - 0.328*sinH) + c.g * (0.587 + 0.413*cosH + 0.035*sinH) + c.b * (0.114 - 0.114*cosH + 0.292*sinH),
            c.r * (0.299 - 0.300*cosH + 1.250*sinH) + c.g * (0.587 - 0.588*cosH - 1.050*sinH) + c.b * (0.114 + 0.886*cosH - 0.203*sinH)
        );
    }
    if (invert) c = 1.0 - c;
    gl_FragColor = vec4(clamp(c, 0.0, 1.0), a);
}