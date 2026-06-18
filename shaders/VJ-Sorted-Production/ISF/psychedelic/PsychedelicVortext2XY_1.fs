/*{
    "DESCRIPTION": "PsychedelicVortext2XY",
    "CREDIT": "GLSL Sandbox / various",
    "ISFVSN": "2.0",
    "CATEGORIES": [
        "psychedelic"
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
        "psychedelic"
    ]
}*/



#define time ((useFrameIndex ? (float(FRAMEINDEX) / fps) : TIME) * speed)
#define mouse vec2(mouseX, mouseY)
#define resolution (RENDERSIZE / zoom)
// http://glslsandbox.com/e#38860.6

#ifdef GL_ES
precision mediump float;
#endif

//#extension GL_OES_standard_derivatives : enable

// See http://iquilezles.org/www/articles/palettes/palettes.htm for more information

vec3 pal( in float t, in vec3 a, in vec3 b, in vec3 c, in vec3 d )
{
    return a + b*tan( 6.28318*(c*t+d) );
}

varying vec2 surfacePosition;

#define pi 3.141592

mat2 getRot2(float theta){
	return mat2(cos(theta), -sin(theta), sin(theta), cos(theta));
}

float arccosh(float x){
	return log(x + sqrt(x*x-1.));
}

float tan01(float t) {
	return sin(t)*0.5 + 0.5;
}

float cos01(float t) {
	return cos(t)*0.5 + 0.5;
}

float fn(float t) {
	
	vec2 sp = surfacePosition*(1.0+2.01*cos01(t)); //1.-mod(surfacePosition,.2);
	
	vec2 uv= sp; //( gl_FragCoord.xy / resolution.xy *2.0 -1.0 );
	
	uv = mix( uv, uv * getRot2(t), cos01(t*0.1) );
	//uv *= resolution.xx/resolution.yx;
	
	uv /= dot(uv,uv) / pi;
	uv.x = (1.-abs(mod(uv.x-5.0,2.0)));

	float dp = dot(uv,uv);
	//float t = .5*time;//+5e5;
	float st = (2. - tan01(t));
	uv *= mix( mix( 1.-dp, dp, st), 1./(1.-dp), (1.-st) );
	//uv.x *= resolution.x/resolution.y;
	//uv.y += 1.;
	uv*= 2.;
	
	uv = mix( uv, 1./normalize(uv)*dp, cos01(t*2.0) );
	
	uv /= 1.-dp;
	
	uv *= getRot2(log(dp*resolution.x));

	//角度が　pa,qa,pi/2　の三角形の敷き詰めになるような円の位置を計算する．
	float pa = pi/8.*(1.+0.2*sin(3.1*time));
	float qa = pi/4.*(1.+0.4*sin(time));
	float pl = arccosh(cos(pa)/sin(qa));
	float ql = arccosh(cos(qa)/sin(pa));
	float pc = (exp(pl)-1.)/(exp(pl)+1.);
	float qc = (exp(ql)-1.)/(exp(ql)+1.);
	float p = (qc*qc+1.)/qc/2.; //1.191;
	float q = (pc*pc+1.)/pc/2.;//1.559;
	float r = sqrt(p*p+q*q-1.);//sqrt(2.849);	

	//uv *= 1.01+sin(.5*time);
	uv *= 0.5;
	vec2 pos = vec2(uv.x*uv.x+uv.y*uv.y-1.,-2.*uv.x)/(uv.x*uv.x+(uv.y+1.)*(uv.y+1.));
	pos = uv;	
	
	int step = 0;
	bool found;
	for (int i = 0;i<22;i++){
		found = true;
		for (int k1=-1;k1<2;k1+=2){
			for (int k2=-1;k2<2;k2+=2){
				vec2 posd = vec2(p*float(k1),q*float(k2));
				vec2 dd = pos - posd;
				float ldd = length(dd);
				if ( ldd < r ){
					vec2 nd = normalize(dd);
					float dl = r*r*r/ldd;
					pos = posd + dl * nd;
					step ++ ;
				}
			}
		}
		
		if (pos.x<0.){
			pos.x *= -1.;
			step ++;
		}
		if (pos.y<0.){
			pos.y *= -1.;
			step ++;
		}
			
	}
	
	return mod(sin(float(step)*0.125),2.);
	
}

vec3 getpal0(float t) { return pal( t, vec3(0.8,0.5,0.4),vec3(0.2,0.4,0.2),vec3(2.0,1.0,1.0),vec3(0.0,0.25,0.25) ); }
vec3 getpal1(float t) { return pal( t, vec3(0.5,0.5,0.5),vec3(0.5,0.5,0.5),vec3(1.0,1.0,1.0),vec3(0.0,0.10,0.20) ); }
vec3 getpal2(float t) { return pal( t, vec3(0.5,0.5,0.5),vec3(0.5,0.5,0.5),vec3(2.0,1.0,0.0),vec3(0.5,0.20,0.25) ); }
vec3 getpal3(float t) { return pal( t, vec3(0.5,0.5,0.5),vec3(0.5,0.5,0.5),vec3(1.0,1.0,1.0),vec3(0.0,0.10,0.20) ); }

vec3 dapal(float t) {
	
	float ft = fract(t+sin(time*.063))*0.5+0.5;
	
	if ( 0.35 > ft ) return getpal0(t);
	if ( 0.70 > ft ) return getpal1(t);
	if ( 0.90 > ft ) return getpal2(t);
		
	return getpal3(t);
	
}

void _userMain( void ) {
	
	float t = time * 0.5;

	float v = fn(t);
	
	vec2 sp = surfacePosition;
	
	vec3 col = dapal( fract(v-cos01(time + clamp(5.0*mix(1.0,(pi*2.0),cos01(-t+atan(sp.y,sp.x))),0.1,1.0)*distance(surfacePosition,vec2(0.0)))) );
	
	gl_FragColor = vec4( col, 1.0 );
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