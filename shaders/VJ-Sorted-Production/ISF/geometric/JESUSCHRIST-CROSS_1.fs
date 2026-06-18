/*{
    "DESCRIPTION": "JESUSCHRIST-CROSS",
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
        "geometric",
        "noise"
    ]
}*/



#define time ((useFrameIndex ? (float(FRAMEINDEX) / fps) : TIME) * speed)
#define mouse vec2(mouseX, mouseY)
#define resolution (RENDERSIZE / zoom)
// Merry Christmas Everyone!
// By: Brandon Fogerty
// bfogerty at gmail dot com

// "Mary will bear a son, and you shall call his name Jesus, 
//  for he will save his people from their sins." - An Angel of God To Joseph (Matthew 1:21)

// "“Glory to God in the highest, and on earth peace among those with whom He is pleased!” - Luke 2:14

// "But he was pierced for our transgressions,
//  he was crushed for our iniquities;
//  the punishment that brought us peace was on him,

/*
One of the criminals who were hanged railed at him, saying, 
“Are you not the Christ? Save yourself and us!”
But the other rebuked him, saying, 
“Do you not fear God, since you are under the same sentence of condemnation?
And we indeed justly, for we are receiving the due reward of our deeds; 
but this man has done nothing wrong.”  
And he said, “Jesus, remember me when you come into your kingdom.” 
And he said to him, “Truly, I say to you, today you will be with me in Paradise.” - Luke 23:39-43
*/

// Special Thanks to my Beautiful Wife, Naomi, for the design!

#ifdef GL_ES
precision mediump float;
#endif

#define Resolution				resolution
#define Time					time

#define GloryGlowColor				vec3(0.26, 0.16, 0.06)
#define VerticalBarWidth			0.09
#define VerticalBarHeight			0.9
#define HorizontalBarWidth			0.7
#define HorizontalBarHeight			0.07
#define HorizontalBarVerticalOffset		0.4

#define ChristsCrossColor			vec3( 1.0, 1.0, 1.0 )
#define UnrepentantThiefsCrossColor		vec3( 0.45, 0.45, 0.45 )
#define RepentantThiefsCrossColor		vec3( 0.90, 0.90, 0.90 )

#define CrossGlowScale				0.02
#define CrossGloryGlowMin			.035
#define CrossGloryGlowMax			5.00

float hash( float x )
{
    return fract( sin( x ) * 43758.5453 );
}

float noise( vec2 uv )  // Thanks Inigo Quilez
{
    vec3 x = vec3( uv.xy, 0.0 );
    
    vec3 p = floor( x );
    vec3 f = fract( x );
    
    f = f*f*(3.0 - 2.0*f);
    
    float offset = 57.0;
    
    float n = dot( p, vec3(1.0, offset, offset*2.0) );
    
    return mix(	mix(	mix( hash( n + 0.0 ), 		hash( n + 1.0 ), f.x ),
        				mix( hash( n + offset), 	hash( n + offset+1.0), f.x ), f.y ),
				mix(	mix( hash( n + offset*2.0), hash( n + offset*2.0+1.0), f.x),
                    	mix( hash( n + offset*3.0), hash( n + offset*3.0+1.0), f.x), f.y), f.z);
}

float snoise( vec2 uv )
{
    return noise( uv ) * 2.0 - 1.0;
}

float perlinNoise( vec2 uv )
{   
    float n = 		noise( uv * 1.0 ) 	* 128.0 +
        		noise( uv * 2.0 ) 	* 64.0 +
        		noise( uv * 4.0 ) 	* 32.0 +
        		noise( uv * 8.0 ) 	* 16.0 +
        		noise( uv * 16.0 ) 	* 8.0 +
        		noise( uv * 32.0 ) 	* 4.0 +
        		noise( uv * 64.0 ) 	* 2.0 +
        		noise( uv * 128.0 ) * 1.0;
    
    float noiseVal = n / ( 1.0 + 2.0 + 4.0 + 8.0 + 16.0 + 32.0 + 64.0 + 128.0 );
    noiseVal = abs(noiseVal * 2.0 - 1.0);
    
    return 	noiseVal;
}

float fBm( vec2 uv, float lacunarity, float gain )
{
    float sum = 0.0;
    float amp = mouse.x;
    
    for( int i = 0; i < 10; ++i )
    {
        sum += ( perlinNoise( uv ) ) * amp;
        amp *= gain;
        uv *= lacunarity;
    }
    
    return sum;
}

float pulse( float value, float minValue, float maxValue )
{
	float t = step( minValue, value ) - step( maxValue, value );
	
	return t;
}

vec3 cross( 	vec2 uv,
		float verticalBarWidth, 
	    	float verticalBarHeight, 
	    	float horizontalBarWidth, 
	    	float horizontalBarHeight,
	    	float horizontalBarVerticalOffset,
	   	vec2 position,
	   	float scale,
	  	vec3 color )
{
	verticalBarWidth 		*= scale;
	verticalBarHeight 		*= scale;
	horizontalBarWidth 		*= scale;
	horizontalBarHeight 		*= scale;
	horizontalBarVerticalOffset 	*= scale;
	
	float verticleBar = pulse( uv.x, -verticalBarWidth + position.x, verticalBarWidth + position.x );
	verticleBar *= pulse( uv.y, -verticalBarHeight + position.y, verticalBarHeight + position.y );
	
	float horizontalBar = pulse( uv.x, -horizontalBarWidth + position.x, horizontalBarWidth + position.x );
	horizontalBar *= pulse( uv.y, -horizontalBarHeight  + horizontalBarVerticalOffset + position.y, horizontalBarHeight + horizontalBarVerticalOffset + position.y );
	
	float intensity = clamp(verticleBar + horizontalBar, 0.0, 1.0);
	
	vec3 finalColor = (color * intensity);
	
	return  finalColor;
}

vec3 gloryGlow( vec2 uv, vec3 glowColor, float minGlow, float maxGlow, float noiseFactor, float speed )
{
	float t = sin( Time ) * 0.50 + 0.50;
	float glowAmount = mix( minGlow, maxGlow, t );
	vec2 glowUV = uv + vec2( 0.0, 0.0 );
	float glowPulse = sin( glowUV.x * glowAmount );
	vec3 color = glowColor * abs( 1.0 / glowPulse ) * noiseFactor;
	return color;
}

vec3 beam( vec2 uv, vec3 glowColor, float noiseFactor, float offset, float speed )
{
	float t = sin( Time * speed ) * 0.50 + 0.50;
	float glowAmount = mix( 0.20, 1.0, t );
	vec2 glowUV = uv + vec2( 0.0, 0.0 );
	float glowPulse = sin( glowUV.x * glowAmount );
	float t2 = sin( Time * 0.50 ) * 0.50 + 0.50;
	float lengthOfBeam = mix( -1.0, 0.70, 1.0 - t2 );
	glowUV = uv + vec2( -1.24 - sin(offset + Time + uv.y * speed) * 0.10, 0.0 );
	glowPulse = sin( glowUV.x * 0.7  );
	float glowFactor = (( abs( 0.2 / glowPulse  ) * noiseFactor)) * (sin(uv.y + lengthOfBeam) * 1.0);
	vec3 color = clamp( glowColor *  glowFactor, 0.0, 1.50);
	return color;
}

void _userMain( void ) 
{

	vec2 uv = ( gl_FragCoord.xy / Resolution.xy ) * 2.0 - 1.0;
	uv.x *= ( Resolution.x / Resolution.y );
	
	vec3 finalColor = vec3( 0.0, 0.0, 0.0 );
	
	float noiseFactor = fBm( uv * 1.0, 2.0, 0.9 );
	
	finalColor += gloryGlow( uv, GloryGlowColor, CrossGloryGlowMin, CrossGloryGlowMax, noiseFactor, 1.0 );
	
	vec3 beamColor = vec3( 0.0, 0.0, 0.0 );
	beamColor += beam( uv, vec3( 0.03, 0.03, 0.13), noiseFactor, 0.0, 2.4 );
	beamColor += beam( uv, vec3( 0.03, 0.13, 0.03), noiseFactor, 0.2, 3.5 );
	beamColor += beam( uv, vec3( 0.13, 0.13, 0.03), noiseFactor, 1.4, 4.6 );
	beamColor += beam( uv, vec3( 0.13, 0.03, 0.03), noiseFactor, 4.0, 6.7 );
	beamColor += beam( uv, vec3( 0.13, 0.03, 0.13), noiseFactor, 6.7, 8.7 );
	
	float t = sin( Time + (uv.y / 2.0) ) * 0.5 + 0.5;
	finalColor += (beamColor * t);
	
	finalColor += cross( 	uv, 
			    	VerticalBarWidth, 
			    	VerticalBarHeight, 
			    	HorizontalBarWidth + 0.05, 
			    	HorizontalBarHeight, 
			    	HorizontalBarVerticalOffset,
			    	vec2( 0.0, 0.0 ),
			    	1.0,
			   	ChristsCrossColor );
	
	finalColor += cross( 	uv, 
			    	VerticalBarWidth, 
			    	VerticalBarHeight, 
			    	HorizontalBarWidth, 
			    	HorizontalBarHeight, 
			    	HorizontalBarVerticalOffset,
			    	vec2( -1.2, -0.5 ),
			    	0.2,
			   	UnrepentantThiefsCrossColor );
	
	finalColor += cross( 	uv, 
			    	VerticalBarWidth, 
			    	VerticalBarHeight, 
			    	HorizontalBarWidth, 
			    	HorizontalBarHeight, 
			    	HorizontalBarVerticalOffset,
			    	vec2( 1.2, -0.5 ),
			    	0.2,
			   	RepentantThiefsCrossColor );
	
	finalColor *= mix( 0.7, 1.0, uv.y );
	finalColor *= mix( 0.7, 1.0, -abs(uv.x));
	
	gl_FragColor = vec4( finalColor, 1.0 );
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