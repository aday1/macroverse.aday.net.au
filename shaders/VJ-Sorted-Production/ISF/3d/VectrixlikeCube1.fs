/*{
    "DESCRIPTION": "VectrixlikeCube1",
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
        "3d"
    ]
}*/



#define time ((useFrameIndex ? (float(FRAMEINDEX) / fps) : TIME) * timeScale)
#define mouse vec2(mouseX, mouseY)
#define resolution RENDERSIZE
#ifdef GL_ES
precision mediump float;
#endif

#define lineCap 1
#define lineWidth 1.

#define pos gl_FragCoord.xy

#define PI (atan(1.) * 4.)

void drawLine(vec2 p1, vec2 p2){
   	vec2 delta = p2 - p1;
	float len = length(delta);
	float dist = abs(delta.y * pos.x - delta.x * pos.y + p2.x * p1.y - p2.y * p1.x) / len;
	
	vec2 center = (p1 + p2) / 2.;
	vec2 perp2 = vec2(center.y - p1.y, p1.x - center.x) + center;
	
	float cDist = abs((perp2.y - center.y) * pos.x - (perp2.x - center.x) * pos.y + perp2.x * center.y - perp2.y * center.x) / len * 4.;
	
	if (cDist > len){
		if (lineCap == 1){
		    dist = min(length (p1 - pos), length (p2 - pos)) - lineWidth;
		}else{
		    dist = max(dist - lineWidth, cDist - len);
		}
	}else{
		dist -= lineWidth;
	}
	
	gl_FragColor = mix(gl_FragColor, vec4(1), 1. - clamp(dist, 0., 1.));
}

void drawLine(vec4 p1, vec4 p2){
	vec2 p21 = p1.xy / p1.w;
	vec2 p22 = p2.xy / p2.w;
	
	p21 = (p21 / 2. + .5) * resolution;
	p22 = (p22 / 2. + .5) * resolution;
	
	drawLine(p21, p22);
}

void main( void ) {
	gl_FragColor = vec4(vec3(cos(PI)), 1);
	
	float fov = PI / 2.;
	float aspect = resolution.x / resolution.y;
	
	float f = 1. / tan(fov / 2.);
	
	mat4 mat = mat4(
		f / aspect, 0, 0, 0,
		0, f, 0, 0,
		0, 0, 0, -1,
		0, 0, 0, 0
	);
	
	mat *= mat4(
		1, 0, 0, 0,
		0, 1, 0, 0,
		0, 0, 1, 0,
		0, 0, -3, 1
	);
	
	mat *= mat4(
		cos(time), 0, sin(time), 0,
		0, 1, 0, 0,
		-sin(time), 0, cos(time), 0,
		0, 0, 0, 1
	);
	
	float size1 = sin(time);
	float size2 = cos(time);
	
	drawLine(mat * vec4(size1, size1, size1, 1), mat * vec4(size1, -size1, size1, 1));
	drawLine(mat * vec4(-size1, -size1, size1, 1), mat * vec4(size1, -size1, size1, 1));
	drawLine(mat * vec4(-size1, -size1, size1, 1), mat * vec4(-size1, size1, size1, 1));
	drawLine(mat * vec4(size1, size1, size1, 1), mat * vec4(-size1, size1, size1, 1));
	
	drawLine(mat * vec4(size1, size1, -size1, 1), mat * vec4(size1, -size1, -size1, 1));
	drawLine(mat * vec4(-size1, -size1, -size1, 1), mat * vec4(size1, -size1, -size1, 1));
	drawLine(mat * vec4(-size1, -size1, -size1, 1), mat * vec4(-size1, size1, -size1, 1));
	drawLine(mat * vec4(size1, size1, -size1, 1), mat * vec4(-size1, size1, -size1, 1));
	
	drawLine(mat * vec4(size1, size1, size1, 1), mat * vec4(size1, size1, -size1, 1));
	drawLine(mat * vec4(size1, -size1, size1, 1), mat * vec4(size1, -size1, -size1, 1));
	drawLine(mat * vec4(-size1, size1, size1, 1), mat * vec4(-size1, size1, -size1, 1));
	drawLine(mat * vec4(-size1, -size1, size1, 1), mat * vec4(-size1, -size1, -size1, 1));
	
	drawLine(mat * vec4(size2, size2, size2, 1), mat * vec4(size2, -size2, size2, 1));
	drawLine(mat * vec4(-size2, -size2, size2, 1), mat * vec4(size2, -size2, size2, 1));
	drawLine(mat * vec4(-size2, -size2, size2, 1), mat * vec4(-size2, size2, size2, 1));
	drawLine(mat * vec4(size2, size2, size2, 1), mat * vec4(-size2, size2, size2, 1));
	
	drawLine(mat * vec4(size2, size2, -size2, 1), mat * vec4(size2, -size2, -size2, 1));
	drawLine(mat * vec4(-size2, -size2, -size2, 1), mat * vec4(size2, -size2, -size2, 1));
	drawLine(mat * vec4(-size2, -size2, -size2, 1), mat * vec4(-size2, size2, -size2, 1));
	drawLine(mat * vec4(size2, size2, -size2, 1), mat * vec4(-size2, size2, -size2, 1));
	
	drawLine(mat * vec4(size2, size2, size2, 1), mat * vec4(size2, size2, -size2, 1));
	drawLine(mat * vec4(size2, -size2, size2, 1), mat * vec4(size2, -size2, -size2, 1));
	drawLine(mat * vec4(-size2, size2, size2, 1), mat * vec4(-size2, size2, -size2, 1));
	drawLine(mat * vec4(-size2, -size2, size2, 1), mat * vec4(-size2, -size2, -size2, 1));
	
	drawLine(mat * vec4(size2, size2, size2, 1), mat * vec4(size1, size1, size1, 1));
	drawLine(mat * vec4(-size2, size2, size2, 1), mat * vec4(-size1, size1, size1, 1));
	drawLine(mat * vec4(-size2, -size2, size2, 1), mat * vec4(-size1, -size1, size1, 1));
	drawLine(mat * vec4(size2, -size2, size2, 1), mat * vec4(size1, -size1, size1, 1));
	
	drawLine(mat * vec4(size2, size2, -size2, 1), mat * vec4(size1, size1, -size1, 1));
	drawLine(mat * vec4(-size2, size2, -size2, 1), mat * vec4(-size1, size1, -size1, 1));
	drawLine(mat * vec4(-size2, -size2, -size2, 1), mat * vec4(-size1, -size1, -size1, 1));
	drawLine(mat * vec4(size2, -size2, -size2, 1), mat * vec4(size1, -size1, -size1, 1));
}
