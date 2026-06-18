/*{
    "DESCRIPTION": "DotMatrix-AquaticField-5",
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
        "3d"
    ]
}*/




#define time ((useFrameIndex ? (float(FRAMEINDEX) / fps) : TIME) * speed)
#define mouse vec2(mouseX, mouseY)
#define resolution (RENDERSIZE / zoom)
#ifdef GL_ES
precision highp float;
#endif

#define CASE(n) if ( i == n )

vec3 getRgbColor( int i )
{
	vec3 result;
	CASE(0) result = vec3( float(  0.0/255.0), float(  0.0/255.0), float(  0.0/255.0)); // black
	CASE(1) result = vec3( float(255.0/255.0), float(255.0/255.0), float(255.0/255.0)); // white
	CASE(2) result = vec3( float(255.0/255.0), float(204.0/255.0), float(204.0/255.0)); // beige
	CASE(3) result = vec3( float(128.0/255.0), float(  0.0/255.0), float(  0.0/255.0)); // brown
	CASE(4) result = vec3( float(255.0/255.0), float(  0.0/255.0), float(  0.0/255.0)); // red
	CASE(5) result = vec3( float(255.0/255.0), float(255.0/255.0), float(  0.0/255.0)); // yellow
	CASE(6) result = vec3( float(  0.0/255.0), float(255.0/255.0), float(  0.0/255.0)); // green
	CASE(7) result = vec3( float(  0.0/255.0), float(255.0/255.0), float(255.0/255.0)); // water
	CASE(8) result = vec3( float(  0.0/255.0), float(  0.0/255.0), float(255.0/255.0)); // blue
	CASE(9) result = vec3( float(128.0/255.0), float(  0.0/255.0), float(128.0/255.0)); // purple
	return result;
}

float circle0(vec2 uv, vec2 pos, float radius)
{
	if(distance(pos, uv) < radius)
	{
		return 1.0;
	}
	
	return 0.0;
}

vec3 circle(vec2 uv, vec2 pos, float radius, vec3 col0, vec3 col1 ) 
{
	float circleMask = circle0(uv, pos, radius);
	vec3 result = mix(col0, col1, circleMask);
	return result;
}

vec3 rect( vec2 pos, float x ,float y, float w, float h, vec3 col0, vec3 col1 )
{
	vec3 result = col0;
	if ( pos.x > x && pos.x < (x + w) 
	  && pos.y > y && pos.y < (y + h) )
	{
		result = col1;
	}
	
	return result;
}

#define PI 3.14159

// from https://glsl.heroku.com/e#15131.0
vec2 nearestHex(float s, vec2 st){
    //float PI = 3.14159265359;
    float TAU = 2.0*PI;
    float deg30 = TAU/12.0;
    float h = sin(deg30)*s;
    float r = cos(deg30)*s;
    float b = s + 2.0*h;
    float a = 2.0*r;
    float m = h/r;

    vec2 sect = st/vec2(2.0*r, h+s);
    vec2 sectPxl = mod(st, vec2(2.0*r, h+s));
    
    float aSection = mod(floor(sect.y), 2.0);
    
    vec2 coord = floor(sect);
    if(aSection > 0.0){
        if(sectPxl.y < (h-sectPxl.x*m)){
            coord -= 1.0;
        }
        else if(sectPxl.y < (-h + sectPxl.x*m)){
            coord.y -= 1.0;
        }

    }
    else{
        if(sectPxl.x > r){
            if(sectPxl.y < (2.0*h - sectPxl.x * m)){
                coord.y -= 1.0;
            }
        }
        else{
            if(sectPxl.y < (sectPxl.x*m)){
                coord.y -= 1.0;
            }
            else{
                coord.x -= 1.0;
            }
        }
    }
    
    float xoff = mod(coord.y, 2.0)*r;
    return vec2(coord.x*2.0*r-xoff, coord.y*(h+s))+vec2(r*2.0, s);
}

vec2 pixel(float s, vec2 st){	
    return ceil(st / s) * s;
}

void _userMain( void ) {

	//vec2 pos = ( gl_FragCoord.xy / resolution.xy );
	//vec2 pos = nearestHex(5.0, gl_FragCoord.xy)/resolution.xy;
	vec2 pos = pixel(5.0, gl_FragCoord.xy)/resolution.xy;

	vec3 col = vec3( 0.0, 0.0, 0.0 );;

	for ( int i = 0; i < 30; i++ ) 
	{
		float x = 0.1 * sin( 2.0 * PI * float(i)/6.0 + time * 0.5) + 0.5;
		float y = 0.1 * cos( 2.0 * PI * float(i)/6.0 + time * 0.5) + 0.5;
		
		//col = rect( pos, float(i)/9.0, 0.5, 0.1, 0.1, col, getRgbColor(i) );
		//col = rect( pos, x, y, 0.1, 0.1, col, getRgbColor(int(mod(float(i),10.0))) );
		col = circle( pos, vec2(x, y), 0.05, col, getRgbColor(int(mod(float(i),10.0))) );
	}

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