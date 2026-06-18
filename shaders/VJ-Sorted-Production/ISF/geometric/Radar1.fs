/*{
    "DESCRIPTION": "Radar1",
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
        }
    ],
    "TAGS": [
        "psychedelic",
        "geometric"
    ]
}*/



#define time ((useFrameIndex ? (float(FRAMEINDEX) / fps) : TIME))
#define mouse vec2(0.0)
#define resolution RENDERSIZE
#ifdef GL_ES
precision mediump float;
#endif

float pi = atan(1.0)*8.0;

float lines = pi/12.0;
float rings = 1.0 / 6.0;
float rad = (5.0/12.0);
float lw = 2.0/resolution.y;

float dfLine(vec2 start, vec2 end, vec2 uv)
{   
	vec2 line = end - start;
	float frac = dot(uv - start,line) / dot(line,line);
	return distance(start + line * clamp(frac, 0.0, 1.0), uv);
}

float dGrid(vec2 uv)
{
	float d = 0.0;
	
	d = abs((mod(atan(uv.y, uv.x) + lines/2.0, lines) - lines/2.0 ));
	d *= length(uv);	
	d = min(d,abs(mod(length(uv) + rings/4.0, rings/2.0) - rings/4.0));
	d = max(d,length(uv) - rad);
	
	return d;
}

void main( void ) 
{
	vec2 res = resolution / resolution.y;
	vec2 uv = gl_FragCoord.xy / resolution.y - res/2.0;
	
	vec3 col = vec3(0.05);
	
	float a = -time;
	
	float scan = dfLine(vec2(0), vec2(cos(a),sin(a)) * rad, uv);
	
	col += clamp(mix(vec3(0,0,0), vec3(0.02,1,0.02), 0.001/(scan*scan*32.0)), 0.0,1.0);
	col.g += max(0.0, 0.2/(1.0+10.8*mod((atan(uv.y,uv.x)-a),pi)/pi)) * smoothstep(lw,0.0,length(uv) - rad);
	
	col *= smoothstep(0.0,lw,dGrid(uv)) * 0.7 + 0.3;
	
	col *= smoothstep(lw,0.0,length(uv) - rad*1.05);
	
	gl_FragColor = vec4( col, 1.0 );

}
