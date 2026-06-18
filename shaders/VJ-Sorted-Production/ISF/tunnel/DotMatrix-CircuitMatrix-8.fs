/*{
    "DESCRIPTION": "DotMatrix-CircuitMatrix-8",
    "CREDIT": "GLSL Sandbox / various",
    "ISFVSN": "2.0",
    "CATEGORIES": [
        "tunnel"
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
        },
        {
            "NAME": "lowFreq",
            "TYPE": "float",
            "MIN": 0.0,
            "MAX": 5.0,
            "DEFAULT": 1.0,
            "LABEL": "Low Freq"
        }
    ],
    "TAGS": [
        "color",
        "tunnel"
    ]
}*/




#define time ((useFrameIndex ? (float(FRAMEINDEX) / fps) : TIME) * speed)
#define mouse vec2(mouseX, mouseY)
#define resolution (RENDERSIZE / zoom)
///////////////////////////////////////////////
//										//
//	Another Color Tech Tunnel	by			//
//					T_S=RTX1911=			//
//										//
//		Kings still playing since 1998 - 2012		//
//										//
//		twitter at	: @rtx1911				//
//				: @T_SDesignWorks			//
//										//
//		www.demoscene.jp					//
//										//
//////////////////////////////////////////////

#ifdef GL_ES
precision mediump float;
#endif

uniform float lowFreq;

vec3 roy(vec3 v, float x)
{
    return vec3(cos(x)*v.x - sin(x)*v.z,v.y,sin(x)*v.x + cos(x)*v.z);
}

vec3 rox(vec3 v, float x)
{
    return vec3(v.x,v.y*cos(x) - v.z*sin(x),v.y*sin(x) + v.z*cos(x));
}

float fdtun(vec3 rd, vec3 ro, float r)
{
	float a = dot(rd.xy,rd.xy);
  	float b = dot(ro.xy,rd.xy);
	float d = (b*b)-(32.0*a*(dot(ro.xy,ro.xy)+(r*r)));
  	return (-b+sqrt(abs(d)))/(3.75*a);
}

vec2 tunuv(vec3 pos){
	return vec2(pos.z,(atan(pos.y, pos.x))/0.31830988618379);
}

vec3 checkerCol(vec2 loc, vec3 col)
{
	return mix(col, vec3(0.25), mod(step(fract(loc.x), 0.75) + step(fract(loc.y), 0.25),7.5));
}

vec3 lcheckcol(vec2 loc, vec3 col)
{
	return checkerCol(loc*15.0,col)*checkerCol(loc*1.75,col);	
}
void _userMain( void ) {
	vec3 dif = vec3(0.15,0.0,0.0);
	vec3 scoll = vec3(10.75,0.5,1.0);
	vec3 scolr = vec3(2.0,2.75,0.5);
	vec2 uv = (gl_FragCoord.xy/resolution.xy);
	vec3 ro = vec3(0.0,0.0,time*2.5);
	vec3 dir = normalize( vec3( -5.0 + 2.0*vec2(uv.x - .2, uv.y)* vec2(resolution.x/resolution.y, 1.0), -1.33 ) );

	float ry = time*0.3;
	
	dir = roy(rox(dir,time*0.25),time*-0.5);
	vec3 lro = ro-dif;
	vec3 rro = ro+dif;

	const float r = 1.0;
	float ld = fdtun(dir,lro,r);
	float rd = fdtun(dir,rro,r);
	vec2 luv = tunuv(ro + ld*dir);
	vec2 ruv = tunuv(ro + rd*dir);
	vec3 coll = lcheckcol(luv*.25,scoll)*(20.0/exp(sqrt(ld)));
	vec3 colr = lcheckcol(ruv*.5,scolr)*(20.0/exp(sqrt(rd)));
	gl_FragColor = vec4(sqrt(coll+2.5*0.01*1.0+(colr-1.0)+0.01*20.0),1.0);
//gl_FragColor = vec4(sqrt(coll+2.5*lowFreq*1.0+(colr-1.0)+lowFreq*20.0),1.0);

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