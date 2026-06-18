/*{
    "DESCRIPTION": "TextGlyph-CosmicEye",
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
	#ifdef GL_ES
precision mediump float;
#endif

#define M_2_PI 0.63661977236
#define M_PI2  1.57079632679

vec3 squereTexture(vec2 uv, vec3 firstColor, vec3 secondColor) { // Flag texture
	if(bool(mod(floor(20.0 * uv.x), 2.0)) ^^ bool(mod(floor(20.0 * uv.y), 2.0))) // I made a true table and this is the result
		return secondColor;
	else
		return firstColor;
}

void main(){
	vec2 uv= gl_FragCoord.xy / resolution.xy; // Calculates UV screen coordinates
	vec2 mousePos = mouse;
	vec3 colorA = vec3(0.467, 0.161, 0.325);
	vec3 colorB = vec3(0.867, 0.282, 0.078);
	float radio = 0.2;
	
	uv.y *= resolution.y / resolution.x;
	mousePos.y *= resolution.y / resolution.x;
	
	if(length(uv - mousePos) <= radio) { // Fish eye effect!!
		float newLength;
		vec3 sphereNormal;
		vec3 rayDirection;
		float angle;
		
		uv -= mousePos;
		
		sphereNormal.x = uv.x;
		sphereNormal.y = uv.y;
		sphereNormal.z = sqrt(radio*radio - length(uv)*length(uv));
		
		newLength = radio * (1.0 - M_2_PI*acos(length(uv)/radio));
		uv /= length(uv);
		uv *= newLength;
		uv += mousePos;
		
		rayDirection = vec3(cos(time), sin(time), sin(time));
		
		angle = acos(((sphereNormal.x * rayDirection.x) + (sphereNormal.y * rayDirection.y) + (sphereNormal.z * rayDirection.z)) / (length(sphereNormal) * length(rayDirection)));
		
		if(angle <= M_PI2)
			gl_FragColor = vec4(squereTexture(uv, colorA, colorB), 1.0);
		else
			gl_FragColor = vec4(0.5 * squereTexture(uv, colorA, colorB), 1.0);
	}
}


