uniform bool invert;
uniform float hueShift;
uniform float brightness;
uniform float colorB;
uniform float colorG;
uniform float colorR;
uniform float contrast;
uniform float saturation;
uniform float zoom;
uniform float speed;
/*{
    "DESCRIPTION": "2Box1Plane",
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
precision mediump float;
#endif

#define PI 3.1415
#define TAU PI * 2.0

//I know, there is no less effiecient way of implementing per-pixel 3D graphics
mat4 view = mat4(1), pMatrix;
vec2 clipspace;

vec3 colorBuffer = vec3(0);  // [s r   sly]
float depthBuffer = 1.0;

void translate(vec3 pos){
	view *= mat4(
		1, 0, 0, 0,
		0, 1, 0, 0,
		0, 0, 1, 0,
		pos, 1
	);	
}

void scale(vec3 pos){
	view[0] *= pos.x;
	view[1] *= pos.y;
	view[2] *= pos.z;
}

void rotate (vec3 u, float a){
	u = normalize(u);
	float s = sin(a), c = cos(a), c1 = 1. - c;
	
	view *= mat4 (
		c + u.x * u.x * c1, u.x * u.y * c1 - u.z * s, u.x * u.z * c1 + u.y * s, 0,
		u.y * u.x * c1 + u.z * s, c + u.y * u.y * c1, u.y * u.z * c1 - u.x * s, 0,
		u.z * u.x * c1 - u.y * s, u.z * u.y * c1 + u.x * s, c + u.z * u.z * c1, 0,
		0, 0, 0, 1
	);
}

mat4 perspective (float FOV, float aspectRatio, float near, float far){
	float f = 1.0 / tan(FOV / 2.0);
	float nf = near - far;
	
	return mat4(f / aspectRatio, 0, 0, 0,
	0, f, 0, 0,
	0, 0, (far + near) / nf, -1,
	0, 0, (2.0 * far * near) / nf, 0);
}

//bounds are top, bottom, left, right
mat4 ortho (vec4 b, float near, float far){
	float x = 1.0 / (b.z - b.w);
	float y = 1.0 / (b.y - b.x);
	float z = 1.0 / (near - far);
	
	return mat4(
		-2.0 * x, 0, 0, 0,
		0, -2.0 * y, 0, 0,
		0, 0, 2.0 * z, 0,
		(b.z + b.w) * x, (b.x + b.y) * y, (far + near) * z, 1
	);
}

vec3 setView (mat4 view, vec3 pos){
	vec4 c = view * vec4(pos, 1);
	
	return c.xyz / c.w;
}

void tri(vec3 color, vec3 p0, vec3 p1, vec3 p2){
	vec3 vp0 = setView(pMatrix *view, p0);
	vec3 vp1 = setView(pMatrix *view, p1);
	vec3 vp2 = setView(pMatrix *view, p2);
	      
	vec2 v0 = vp2.xy - vp0.xy;
	vec2 v1 = vp1.xy - vp0.xy;
	vec2 v2 = clipspace - vp0.xy;
	
	float dot00 = dot(v0, v0);
	float dot01 = dot(v0, v1);
	float dot02 = dot(v0, v2);
	float dot11 = dot(v1, v1);
	float dot12 = dot(v1, v2);

	float f = 1. / (dot00 * dot11 - dot01 * dot01);
	float u = (dot11 * dot02 - dot01 * dot12) * f;
	float v = (dot00 * dot12 - dot01 * dot02) * f;
	float w = 1. - u - v;

	if (u >= 0. && v >= 0. && w >= 0.){
		p0 = setView(view, p0);
		p1 = setView(view, p1);
		p2 = setView(view, p2);
		
		float depth = vp0.z * w + vp1.z * v + vp2.z * u;
		vec3 pos = p0 * w + p1 * v + p2 * u;
		
		//LEQUAL OpenGL Equivelent
		if (depthBuffer >= depth && depth >= -1.0){
			//lighting
			vec3 normal = normalize(cross (p1 - p0, p0 - p2));
			vec3 npos = normalize(pos);
			color *= max(0., dot(npos, normal)) + pow(max(0., dot(reflect(-npos, normal), npos)), 10.);
			
			depthBuffer = depth;
			colorBuffer = color;
		}
	}
}

void quad(vec3 color, vec3 p1, vec3 p2, vec3 p3, vec3 p4){
	tri(color, p1, p2, p3);
	tri(color, p2, p4, p3);
}

void drawScene(){
	translate(vec3(0, 0, -6));
	
	rotate(vec3(0, 1, 0), time);
	
	{
		mat4 cache = view;
		view = mat4(view);
		
		translate(vec3(-2, 0, 0));
		rotate(vec3(1, 1, 1), time);
		
		quad(vec3(1, 0, 1), vec3(-1, -1, 1), vec3(1, -1, 1), vec3(-1, 1, 1), vec3(1, 1, 1));
		quad(vec3(1, 1, 0), vec3(-1, -1, -1), vec3(-1, 1, -1), vec3(1, -1, -1), vec3(1, 1, -1));
		
		quad(vec3(0, 0, 1), vec3(1, -1, -1), vec3(1, 1, -1), vec3(1, -1, 1), vec3(1, 1, 1));
		quad(vec3(0, 1, 0), vec3(-1, -1, -1), vec3(-1, -1, 1), vec3(-1, 1, -1), vec3(-1, 1, 1));
		
		quad(vec3(1, 0, 0), vec3(-1, 1, -1), vec3(-1, 1, 1), vec3(1, 1, -1), vec3(1, 1, 1));
		quad(vec3(0, 1, 1), vec3(-1, -1, -1), vec3(1, -1, -1), vec3(-1, -1, 1), vec3(1, -1, 1));
		
		view = cache;
	}
	
	{
		mat4 cache = view;
		view = mat4(view);
		
		translate(vec3(2, 0, 0));
		rotate(vec3(1, -1, -0.5), -time);
		
		quad(vec3(1, 0, 1), vec3(-1, -1, 1), vec3(1, -1, 1), vec3(-1, 1, 1), vec3(1, 1, 1));
		quad(vec3(1, 1, 0), vec3(-1, -1, -1), vec3(-1, 1, -1), vec3(1, -1, -1), vec3(1, 1, -1));
		
		quad(vec3(0, 0, 1), vec3(1, -1, -1), vec3(1, 1, -1), vec3(1, -1, 1), vec3(1, 1, 1));
		quad(vec3(0, 1, 0), vec3(-1, -1, -1), vec3(-1, -1, 1), vec3(-1, 1, -1), vec3(-1, 1, 1));
		
		quad(vec3(1, 0, 0), vec3(-1, 1, -1), vec3(-1, 1, 1), vec3(1, 1, -1), vec3(1, 1, 1));
		quad(vec3(0, 1, 1), vec3(-1, -1, -1), vec3(1, -1, -1), vec3(-1, -1, 1), vec3(1, -1, 1));

		view = cache;
	}
	
	{
		mat4 cache = view;
		view = mat4(view);
		
		translate(vec3(0, -2, 0));
		
		quad(vec3(1, 1, 1), vec3(-2, 0, -2), vec3(-2, 0, 2), vec3(2, 0, -2), vec3(2, 0, 2));
		
		view = cache;
	}
}

void _userMain( void ) {
	clipspace = gl_FragCoord.xy / resolution * 2.0 - 1.0;
	//pMatrix = ortho(vec4(10, -10, 10, -10), 0.1, 100.0);
	pMatrix = perspective(PI / 2.0, resolution.x / resolution.y, 0.1, 100.0);
	
	drawScene();
	
	gl_FragColor = vec4(colorBuffer, 1);
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