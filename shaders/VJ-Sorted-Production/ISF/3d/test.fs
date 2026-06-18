/*{
    "DESCRIPTION": "test",
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

#extension GL_OES_standard_derivatives : enable

float map(vec3 point) {
	vec3 pointInCurrentSpace = fract(point) * 2.0 - 1.0;
	
	//float xDifference = abs(pointInCurrentSpace.x) - mouse.x;
	//float yDifference = abs(pointInCurrentSpace.y) - 0.25;
	//float zDifference = abs(pointInCurrentSpace.z) - 0.25;
	
	//return max(xDifference, max(yDifference, zDifference));
	
	float yzDifference = length(pointInCurrentSpace.yz) - mouse.y;
	float xyDifference = length(pointInCurrentSpace.xy) - 0.25;
	return min(yzDifference, xyDifference);
	
	//return length(pointInCurrentSpace) - 0.25;
}

float trace(vec3 origin, vec3 ray) {
	float pointAlongRay = 0.0;
	for (int i = 0; i < 32; i++) {
		vec3 pointInSpace = origin + (ray * pointAlongRay);
		pointAlongRay += map(pointInSpace) * 0.5;
	}
	return pointAlongRay;
}

void main( void ) {

	vec2 position = gl_FragCoord.xy / resolution.xy;
	position -= 0.5;
	position.x *= resolution.x / resolution.y;
	
	vec3 origin = vec3(0.0, 0.0, time);
	vec3 ray = vec3(position, 1.0);
	
	float distanceToIntersectionPoint = trace(origin, ray);
	float pixelIntensity = 1.0 / (1.0 + distanceToIntersectionPoint * distanceToIntersectionPoint * 0.1);
	
	gl_FragColor = vec4(vec3(pixelIntensity), 1.0);

}
