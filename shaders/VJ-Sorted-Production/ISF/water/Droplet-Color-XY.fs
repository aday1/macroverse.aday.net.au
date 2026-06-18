/*{
    "DESCRIPTION": "Droplet-Color-XY",
    "CREDIT": "GLSL Sandbox / various",
    "ISFVSN": "2.0",
    "CATEGORIES": [
        "water"
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
        "water",
        "texture-input"
    ]
}*/



#define time ((useFrameIndex ? (float(FRAMEINDEX) / fps) : TIME) * timeScale)
#define mouse vec2(mouseX, mouseY)
#define resolution RENDERSIZE

#ifdef GL_ES
precision mediump float;
#endif

uniform sampler2D buffer;

vec3 hsv2rgbNorm(vec3 c)
{
	vec4 K = vec4(1.0, 2.0 / 3.0, 1.0 / 3.0, 3.0);
	vec3 p = abs(fract(c.xxx + K.xyz) * 6.0 - K.www);
	return c.z * normalize(mix(K.xxx, clamp(p - K.xxx, 0.0, 1.0), c.y));
}

vec3 hsv2rgb(vec3 c)
{
	vec4 K = vec4(1.0, 2.0 / 3.0, 1.0 / 3.0, 3.0);
	vec3 p = abs(fract(c.xxx + K.xyz) * 6.0 - K.www);
	return c.z * mix(K.xxx, clamp(p - K.xxx, 0.0, 1.0), c.y);
}

vec3 some_gradient(float pos)
{
	float theta = pos * 6.2831853;
	return vec3(sin(theta - 2.0943951),
		    sin(theta),
		    sin(theta + 2.0943951)) * 0.5 + 0.5;
}

float watersim(sampler2D bb, vec2 uv)
{
	// controls
	const float exciter_size = 0.01; // hotspot diameter
	const float exciter_freq = 0.0; // hotspot frobnication, in Hz
	
	const float C = 1.68; // ripple speed
	const float D = 0.12; // distance
	const float U = 0.15; // viscosity - aka damping
	const float T = 0.05; // time passed between frames

	float aspect = resolution.y / resolution.x;
	vec2 ms = mouse * vec2(1., aspect);
	vec2 uva = uv * vec2(2.0, aspect);
	float excitement = cos(time * exciter_freq * 6.2831853);
	float exciter = excitement * (1.0 - step(exciter_size, length(uva - ms)));

	vec2 uvold = uv.x < 0.5 ? uv : uv - vec2(0.5,0.0);
	float old = texture2D(bb, clamp(uvold,vec2(0.0),vec2(0.49999,0.99999))).a;
	float older = texture2D(bb, clamp(uvold+vec2(0.5,0.0),vec2(0.5,0.0),vec2(1.0))).a;
	vec3 d = vec3(1.0 / resolution, 0.0);
        float oldneighbors =
		texture2D(bb, clamp(uvold - d.xz,vec2(0.0001),vec2(0.49999,.99999))).a +
		texture2D(bb, clamp(uvold + d.xz,vec2(0.0001),vec2(0.49999,.99999))).a +
		texture2D(bb, clamp(uvold - d.zy,vec2(0.0001),vec2(0.49999,.99999))).a +
		texture2D(bb, clamp(uvold + d.zy,vec2(0.0001),vec2(0.49999,.99999))).a
		- 2.0; // subtract 0.5 per sample to recover effective sign of each
	float amplitude = ((4.0 - 8.0 * C * C * T * T / (D * D)) / (U * T + 2.0) * (old - 0.5) * 2.0 +
                            (U * T - 2.0) / (U * T + 2.0) * (older - 0.5) * 2.0 +
                            (2.0 * C * C * T * T / (D * D)) / (U * T + 2.0) * oldneighbors * 2.0 + exciter)
				* 0.999; // dithering hack? noisier but nicer since ripples stay visible longer
	float fresh = (clamp(amplitude, -1.0, 1.0) * 0.5) + 0.5; //the interesting value
	return uv.x < 0.5 ? fresh : old;
}

vec4 texture2D_bicubic(sampler2D tex, vec2 uv)
{
	// only scaled here on x axis so y bits disabled
	vec2 ps = 1./resolution;
	vec2 uva = uv+ps*.5;
	vec2 f = fract(uva*resolution);
	vec2 texel = uv-f*ps;
#define bcfilt(a) (a<2.?a<1.?((3.*a-6.)*a*a+4.)/6.:(((6.-a)*a-12.)*a+8.)/6.:0.) 
	vec4 fxs = vec4(bcfilt(abs(1.+f.x)), bcfilt(abs(f.x)),
			bcfilt(abs(1.-f.x)), bcfilt(abs(2.-f.x)));
	//vec4 fys = vec4(bcfilt(abs(1.+f.y)), bcfilt(abs(f.y)),
	//		bcfilt(abs(1.-f.y)), bcfilt(abs(2.-f.y)));
#undef bcfilt
	//vec4 result = vec4(0);
	//for (int r = -1; r <= 2; ++r)
	//{
		vec4 tmp = vec4(0);
		for (int t = -1; t <= 2; ++t)
			tmp += texture2D(tex, texel+vec2(t,0)*ps) * fxs[t+1];
		//result += tmp * fys[r+1];
	//}
	//return result;
	return tmp;
}

void main(void)
{
	vec2 uv = gl_FragCoord.xy / resolution;
	float water = watersim(buffer,uv);
	float old = texture2D_bicubic(buffer,uv*(vec2(0.5,1.0))).a;
	//use previous buffer for mapping to whole screen and stash results
	//gl_FragColor = vec4(hsv2rgb(vec3(old + 0.35, 1.0,1.0)), water);
	//gl_FragColor = vec4(hsv2rgbNorm(vec3(old + 0.35, 1.0,1.0)), water);
	gl_FragColor = vec4(some_gradient(old + 0.5), water);

	// fake cross-eye stereo debug visual:
	//gl_FragColor = vec4(hsv2rgb(vec3(water)), water);

	if (resolution == vec2(200,100))
		gl_FragColor = vec4(some_gradient(length(gl_FragCoord)),1.0);
}

