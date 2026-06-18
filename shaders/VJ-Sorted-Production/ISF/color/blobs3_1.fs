/*{
    "DESCRIPTION": "blobs3",
    "CREDIT": "GLSL Sandbox / various",
    "ISFVSN": "2.0",
    "CATEGORIES": [
        "color"
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
        "color",
        "geometric"
    ]
}*/



#define time ((useFrameIndex ? (float(FRAMEINDEX) / fps) : TIME) * speed)
#define mouse vec2(mouseX, mouseY)
#define resolution (RENDERSIZE / zoom)
#ifdef GL_ES
precision mediump float;
#endif

// blobbies - by @P_Malin

float k_PI = 3.141592654;
float k_MoveSpeed = 0.5;
float k_BlobSize = 0.5;
float k_OrbitRadius = 0.5;

vec2 GetRandom2( in vec2 vInput )
{
	vec2 temp1 = vInput * vec2(17.1232, 33.32432);
	vec2 temp2 = vInput * vec2(34.32432, 12.9742);
	vec2 temp3 = vec2(sin(temp1.x) + sin(temp2.y), sin(temp1.y) + sin(temp2.x)) * 121.1231;
	return fract(temp3);	
}

vec2 GetOffset2( in vec2 vInput )
{
	return vec2(sin(vInput.x * k_PI * 2.0), cos(vInput.y * k_PI * 2.0));
}

float BlobTexture( in vec2 vPos )
{
	float fLen = length(vPos) / k_BlobSize;
	return max(1.0 - fLen, 0.0);
}

float GetBlobValue( in vec2 vPosition, in vec2 vTileOffset )
{
   	vec2 vTilePos = floor(vPosition - vTileOffset);

        vec2 vRandom = GetRandom2(vTilePos);
  	vec2 vTime2d = vec2(time, time * 0.92312) * k_MoveSpeed; 
	
	// blob orbits around the corner of the 4 tiles it can overlap
	vec2 vBlobPos = GetOffset2(vRandom + vTime2d) * k_OrbitRadius;
	
	vec2 vSubTilePos = fract(vPosition) + vTileOffset;
	
        return BlobTexture(vSubTilePos - vBlobPos);
}

vec4 GetColour( in vec2 vPixelPosition )
{
	vec2 vPosition = vPixelPosition;
	
   	float fValue = 0.0;
      
	// each blob wanders across 4 tiles, we accumulate the blobs that could be touching the current pixel
	fValue += GetBlobValue( vPosition, vec2( 0.0, 0.0 ) );
	fValue += GetBlobValue( vPosition, vec2( -1.0, 0.0 ) );
	fValue += GetBlobValue( vPosition, vec2( 0.0, -1.0 ) );
	fValue += GetBlobValue( vPosition, vec2( -1.0, -1.0 ) );
  
	fValue = max(fValue, 0.0);
	fValue = 1.0 - abs(0.7 - fValue);
	fValue = max(fValue, 0.0);
	
	fValue = pow(fValue, 1.5);
	
	vec4 cColA = vec4(1.0, 0., 0.4, 1.0);
	vec4 cColB = vec4(0.0, 0.0, 0.0, 0.0);
	
  	return cColA * fValue + cColB * (1.0 - fValue);
}

void _userMain( void ) {
	
	gl_FragColor = GetColour(gl_FragCoord.xy * ( mouse.x / 8.0 ));	

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