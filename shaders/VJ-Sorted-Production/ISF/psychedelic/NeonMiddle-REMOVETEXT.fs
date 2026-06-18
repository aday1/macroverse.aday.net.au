/*{
    "DESCRIPTION": "NeonMiddle-REMOVETEXT",
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
        "psychedelic"
    ]
}*/



#define time ((useFrameIndex ? (float(FRAMEINDEX) / fps) : TIME) * timeScale)
#define mouse vec2(mouseX, mouseY)
#define resolution RENDERSIZE
// http://glslsandbox.com/e#27069.2
// NEON 80s FUTURE

// GLSLGridWalk v2.0 by I.G.P.

#ifdef GL_ES
precision mediump float;
#endif

//Testing letters made from arcs and quads.

float dfSemiArc(float rma, float rmi, vec2 uv)
{
	return max(abs(length(uv) - rma) - rmi, uv.x);
}

float dfQuad(vec2 p0, vec2 p1, vec2 p2, vec2 p3, vec2 uv)
{
	vec2 s0n = normalize((p1 - p0).yx * vec2(-1,1));
	vec2 s1n = normalize((p2 - p1).yx * vec2(-1,1));
	vec2 s2n = normalize((p3 - p2).yx * vec2(-1,1));
	vec2 s3n = normalize((p0 - p3).yx * vec2(-1,1));
	
	return max(max(dot(uv-p0,s0n),dot(uv-p1,s1n)), max(dot(uv-p2,s2n),dot(uv-p3,s3n)));
}

float dfRect(vec2 size, vec2 uv)
{
	return max(max(-uv.x,uv.x - size.x),max(-uv.y,uv.y - size.t));
}

//--- Letters ---
void G(inout float df, vec2 uv)
{
	df = min(df, dfSemiArc(0.5, 0.125, uv));
	df = min(df, dfQuad(vec2(0.000, 0.375), vec2(0.000, 0.625), vec2(0.250, 0.625), vec2(0.125, 0.375), uv));
	df = min(df, dfRect(vec2(0.250, 0.50), uv - vec2(0.0,-0.625)));
	df = min(df, dfQuad(vec2(-0.250,-0.125), vec2(-0.125,0.125), vec2(0.250,0.125), vec2(0.250,-0.125), uv));	
}

void L(inout float df, vec2 uv)
{
	df = min(df, dfRect(vec2(0.250, 1.25), uv - vec2(-0.625,-0.625)));
	df = min(df, dfQuad(vec2(-0.375,-0.625), vec2(-0.375,-0.375), vec2(0.250,-0.375), vec2(0.125,-0.625), uv));	
}

void S(inout float df, vec2 uv)
{
	df = min(df, dfSemiArc(0.25, 0.125, uv - vec2(-0.250,0.250)));
	df = min(df, dfSemiArc(0.25, 0.125, (uv - vec2(-0.125,-0.25)) * vec2(-1)));
	df = min(df, dfRect(vec2(0.125, 0.250), uv - vec2(-0.250,-0.125)));
	df = min(df, dfQuad(vec2(-0.625,-0.625), vec2(-0.500,-0.375), vec2(-0.125,-0.375), vec2(-0.125,-0.625), uv));	
	df = min(df, dfQuad(vec2(-0.250,0.375), vec2(-0.250,0.625), vec2(0.250,0.625), vec2(0.125,0.375), uv));
}
//----------------------
float linstep(float x0, float x1, float xn)
{
	return (xn - x0) / (x1 - x0);
}
//----------------------
vec3 retrograd(float x0, float x1, vec2 uv)
{
	float mid = -0.2;// + sin(uv.x*24.0)*0.01;

	vec3 grad1 = mix(vec3(0.60, 0.90, 1.00), vec3(0.05, 0.05, 0.40), linstep(mid, x1, uv.y));
	vec3 grad2 = mix(vec3(1.90, 1.30, 1.00), vec3(0.10, 0.10, 0.00), linstep(x0, mid, uv.y));

	return mix(grad2, grad1, smoothstep(mid, mid + 0.008, uv.y));
}
//----------------------
// improved version of gridColor
// thanx to all glsl artists
//----------------------
const vec3 color1 = vec3(-1., 0., .9);
const vec3 color2 = vec3(0.9, 0., .9);

vec3 gridColor() 
{
  vec2 p = gl_FragCoord.xy / resolution.y;
  float py = p.y;
  p -= resolution.xy / resolution.y / 2.;
  p = abs(.5-mod(vec2(p.x * abs(1./p.y)+.5*sin(time), abs(1./p.y)+time),1.));
  float grid = 2. * max(p.x, p.y);
  return (mix(color1, color2, py*2.) + 1.2) 
    * max(pow(grid,sin(time*10.)+5.), smoothstep(.93,.96,grid) * 2.) * (abs(py-.5)-.1)/.9;
}
//----------------------
void main( void ) 
{
	vec2 uv = gl_FragCoord.xy / resolution.y;
	vec2 aspect = resolution.xy / resolution.y;
	uv = (uv - aspect/2.0)*3.5;

	float dist = 1e6;
	float charSpace = 1.125;
 	uv.x += charSpace * 1.5;
	G(dist, uv); uv.x -= charSpace;
	L(dist, uv); uv.x -= charSpace;
	S(dist, uv); uv.x -= charSpace;
	L(dist, uv); uv.x -= charSpace;
	
	vec3 backcol = gridColor() * smoothstep(0.02,0.025,dist);
	vec3 textcol = retrograd(-0.75,0.50,uv + vec2(0.0,-pow(max(0.0,-dist*0.1),0.5)*0.2));
	vec3 color = mix(backcol, textcol, smoothstep(4.0 / resolution.y, 0.0, dist));
	gl_FragColor = vec4( color, 1.0 );
}

// go 27068.2 for non text


