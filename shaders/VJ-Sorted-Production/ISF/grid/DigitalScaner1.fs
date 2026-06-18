/*{
    "DESCRIPTION": "DigitalScaner1",
    "CREDIT": "GLSL Sandbox / various",
    "ISFVSN": "2.0",
    "CATEGORIES": [
        "grid"
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
        "grid"
    ]
}*/



#define time ((useFrameIndex ? (float(FRAMEINDEX) / fps) : TIME) * timeScale)
#define mouse vec2(mouseX, mouseY)
#define resolution RENDERSIZE
#ifdef GL_ES
precision mediump float;
#endif

#extension GL_OES_standard_derivatives : enable

const float PI = 3.14159265358979323844;

const vec2 text_size = vec2(16., 80.); // pixels

float wobble(vec2 pos)
{
	return .5 + .5*sin(3.*time - 3.5*length(pos));
}

float in_hexagon(vec2 pos, vec2 center, float r)
{
	vec2 d = pos - center;
	float l = r*wobble(center)*cos(PI/6.)/cos(mod(atan(d.x, d.y) + PI, PI/3.) - PI/6.);
	return smoothstep(l + .001, l, length(d));

}

const float hexagon_radius = .5;

#define IN_ROW(y, l) { vec2 center = vec2(-.5*(l - 1.)*hexagon_radius, y); for (float c = 0.; c < l; c++) { v += in_hexagon(pos, center, .5*hexagon_radius); center += vec2(hexagon_radius, 0.); } }

float in_cluster(vec2 pos)
{
	float dy = pos.x*sqrt(3.)/2.;
	
	float v = 0.;

	IN_ROW(3.*dy, 4.)
	IN_ROW(2.*dy, 5.)
	IN_ROW(dy, 6.)
	IN_ROW(0., 7.)
	IN_ROW(-dy, 6.)
	IN_ROW(-2.*dy, 5.)
	IN_ROW(-3.*dy, 4.)
		
	return v;
}

float in_text(vec2 pos)
{
	vec2 p = floor(vec2(pos.x, -pos.y)*80. + .5*text_size);

	if (any(greaterThanEqual(p, text_size)) || any(lessThan(p, vec2(0.))))
		return 0.;

	float uv = p.y*text_size.x + p.x;

	float idx = floor(uv/24.);

	float v = 4.;
	v = mix(v, 32768., step(1., idx));
	v = mix(v, 0., step(2., idx));
	v = mix(v, 4064257., step(3., idx));
	v = mix(v, 8421312., step(4., idx));
	v = mix(v, 0., step(5., idx));
	v = mix(v, 262400., step(6., idx));
	v = mix(v, 4480802., step(7., idx));
	v = mix(v, 16646272., step(8., idx));
	v = mix(v, 65599., step(9., idx));
	v = mix(v, 4465276., step(10., idx));
	v = mix(v, 32836., step(11., idx));
	v = mix(v, 16713216., step(12., idx));
	v = mix(v, 2245887., step(13., idx));
	v = mix(v, 16678852., step(14., idx));
	v = mix(v, 10682495., step(15., idx));
	v = mix(v, 4456704., step(16., idx));
	v = mix(v, 4473890., step(17., idx));
	v = mix(v, 8405122., step(18., idx));
	v = mix(v, 65697., step(19., idx));
	v = mix(v, 6234726., step(20., idx));
	v = mix(v, 4227652., step(21., idx));
	v = mix(v, 14680192., step(22., idx));
	v = mix(v, 2238977., step(23., idx));
	v = mix(v, 8552388., step(24., idx));
	v = mix(v, 49216., step(25., idx));
	v = mix(v, 2818864., step(26., idx));
	v = mix(v, 263202., step(27., idx));
	v = mix(v, 4210818., step(28., idx));
	v = mix(v, 135168., step(29., idx));
	v = mix(v, 270904., step(30., idx));
	v = mix(v, 4227588., step(31., idx));
	v = mix(v, 1048640., step(32., idx));
	v = mix(v, 10686466., step(33., idx));
	v = mix(v, 16678852., step(34., idx));
	v = mix(v, 16511., step(35., idx));
	v = mix(v, 3670576., step(36., idx));
	v = mix(v, 269473., step(37., idx));
	v = mix(v, 12583040., step(38., idx));
	v = mix(v, 253952., step(39., idx));
	v = mix(v, 508328., step(40., idx));
	v = mix(v, 32772., step(41., idx));
	v = mix(v, 384., step(42., idx));
	v = mix(v, 27649., step(43., idx));
	v = mix(v, 8389632., step(44., idx));
	v = mix(v, 458752., step(45., idx));
	v = mix(v, 12976512., step(46., idx));
	v = mix(v, 16769024., step(47., idx));
	v = mix(v, 128., step(48., idx));
	v = mix(v, 49180., step(49., idx));
	v = mix(v, 65411., step(50., idx));
	v = mix(v, 32768., step(51., idx));
	v = mix(v, 7340032., step(52., idx));
	v = mix(v, 16776960., step(53., idx));

	return mod(floor(v/pow(2., mod(uv, 24.))), 2.);
}

void main()
{
        vec2 pos = (gl_FragCoord.xy*2. - resolution)/min(resolution.x, resolution.y);

	float s = .5 + .5*cos(gl_FragCoord.y*2.*PI/4.);

	vec4 bg = mix(vec4(.5, .5, .5, 1.), vec4(0., 0., 0., 0.), .75*length(pos));

	float v0 = in_cluster(pos)*mix(1., .25, step(pos.y, .2)*step(-.2, pos.y))*s;
	float v1 = in_text(pos)*(s + .25);

	gl_FragColor = mix(bg, vec4(.5, 2., .5, 1.), v0) + mix(vec4(0., 0., 0., 1.), vec4(.75, 1., .75, 1.), v1);
}

