/*{
    "DESCRIPTION": "MirrorSurface-TextGlyph-2",
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
        "geometric"
    ]
}*/


#define time TIME




#define mouse vec2(0.0)
#define resolution RENDERSIZE
precision highp float;
// ToDo :  logo.. Need to mix it all or some kind of time control switch !
//Writer done..-Do Fix color- Smaller font?..... Harley / Mirror 
// Like dat? --novalis

#define slashN cpos = vec2(1,cpos.y-6.);
#define CPOS cpos.x += 4.;if(cpos.x > 61.) slashN
#define A c += txColor*Sprite3x5(31725.,floor(p-cpos));CPOS
#define B c += txColor*Sprite3x5(31663.,floor(p-cpos));CPOS
#define C c += txColor*Sprite3x5(31015.,floor(p-cpos));CPOS
#define D c += txColor*Sprite3x5(27502.,floor(p-cpos));CPOS
#define E c += txColor*Sprite3x5(31143.,floor(p-cpos));CPOS
#define F c += txColor*Sprite3x5(31140.,floor(p-cpos));CPOS
#define G c += txColor*Sprite3x5(31087.,floor(p-cpos));CPOS
#define H c += txColor*Sprite3x5(23533.,floor(p-cpos));CPOS
#define I c += txColor*Sprite3x5(29847.,floor(p-cpos));CPOS
#define J c += txColor*Sprite3x5(47196.,floor(p-cpos));CPOS
#define K c += txColor*Sprite3x5(23469.,floor(p-cpos));CPOS
#define L c += txColor*Sprite3x5(18727.,floor(p-cpos));CPOS
#define M c += txColor*Sprite3x5(24429.,floor(p-cpos));CPOS
#define N c += txColor*Sprite3x5(7148.,floor(p-cpos));CPOS
#define O c += txColor*Sprite3x5(31599.,floor(p-cpos));CPOS
#define P c += txColor*Sprite3x5(31716.,floor(p-cpos));CPOS
#define Q c += txColor*Sprite3x5(31609.,floor(p-cpos));CPOS
#define R c += txColor*Sprite3x5(27565.,floor(p-cpos));CPOS
#define S c += txColor*Sprite3x5(31183.,floor(p-cpos));CPOS
#define T c += txColor*Sprite3x5(29842.,floor(p-cpos));CPOS
#define U c += txColor*Sprite3x5(23407.,floor(p-cpos));CPOS
#define V c += txColor*Sprite3x5(23402.,floor(p-cpos));CPOS
#define W c += txColor*Sprite3x5(23421.,floor(p-cpos));CPOS
#define X c += txColor*Sprite3x5(23213.,floor(p-cpos));CPOS
#define Y c += txColor*Sprite3x5(23186.,floor(p-cpos));CPOS
#define Z c += txColor*Sprite3x5(29351.,floor(p-cpos));CPOS
#define n0 c += txColor*Sprite3x5(31599.,floor(p-cpos));CPOS
#define n1 c += txColor*Sprite3x5(9362.,floor(p-cpos));CPOS
#define n2 c += txColor*Sprite3x5(29671.,floor(p-cpos));CPOS
#define n3 c += txColor*Sprite3x5(29391.,floor(p-cpos));CPOS
#define n4 c += txColor*Sprite3x5(23497.,floor(p-cpos));CPOS
#define n5 c += txColor*Sprite3x5(31183.,floor(p-cpos));CPOS
#define n6 c += txColor*Sprite3x5(31215.,floor(p-cpos));CPOS
#define n7 c += txColor*Sprite3x5(29257.,floor(p-cpos));CPOS
#define n8 c += txColor*Sprite3x5(31727.,floor(p-cpos));CPOS
#define n9 c += txColor*Sprite3x5(31695.,floor(p-cpos));CPOS
#define DOT c += txColor*Sprite3x5(2.,floor(p-cpos));CPOS
#define COLON c += txColor*Sprite3x5(1040.,floor(p-cpos));CPOS
#define PLUS c += txColor*Sprite3x5(1488.,floor(p-cpos));CPOS
#define DASH c += txColor*Sprite3x5(448.,floor(p-cpos));CPOS
#define LPAREN c += txColor*Sprite3x5(10530.,floor(p-cpos));CPOS
#define RPAREN c += txColor*Sprite3x5(8778.,floor(p-cpos));CPOS

#define _ cpos.x += 4.;if(cpos.x > 61.) slashN

#define BLOCK c += txColor*Sprite3x5(32767.,floor(p-cpos));CPOS
#define QMARK c += txColor*Sprite3x5(25218.,floor(p-cpos));CPOS
#define EXCLAM c += txColor*Sprite3x5(9346.,floor(p-cpos));CPOS

///////////////////////////////
float tau = atan(1.0)*8.0;

float linstep(float x0, float x1, float xn)
{
	return (xn - x0) / (x1 - x0);
}

float cdist(vec2 v0, vec2 v1)
{
	v0 = abs(v0-v1);
	return max(v0.x,v0.y);
}

vec3 retrograd(float x0, float x1, vec2 uv)
{
	float mid = 0.4 + sin(uv.x*48.0)*0.005;

	vec3 grad1 = mix(vec3(0.60, 0.90, 1.00), vec3(0.05, 0.05, 0.40), linstep(mid, x1, uv.y));
	vec3 grad2 = mix(vec3(1.90, 1.30, 1.00), vec3(0.10, 0.10, 0.00), linstep(x0, mid, uv.y));

	return mix(grad2, grad1, smoothstep(mid, mid + 0.008, uv.y));
}

float dline(vec2 p0,vec2 p1,vec2 uv)
{
	vec2 dir = normalize(p1-p0);
	uv = (uv-p0) * mat2(dir.x,dir.y,-dir.y,dir.x);
	return distance(uv,clamp(uv,vec2(0),vec2(distance(p0,p1),0)));
}

float getBit(float num,float bit){
    num = floor(num);
    bit = floor(bit);
    return float(mod(floor(num/pow(2.,bit)),2.) == 1.0);
}

float Sprite3x5(float sprite,vec2 p){
    float bounds = float(all(lessThan(p,vec2(3,5))) && all(greaterThanEqual(p,vec2(0,0))));
    return getBit(sprite,(2. - p.x) + 3. * p.y) * bounds;
}

void main( void ) {
	float time = mod(time, 3.14*6.);
	vec2 p = ((gl_FragCoord.xy/resolution.xy + vec2(-.4,-.6) + vec2(cos(time)*.1,sin(time)*.2)) * vec2(64,32))*(3.+sin(time+1.6));
	vec3 c = vec3(0);
	vec2 cpos = vec2(1,1);
	vec3 txColor = vec3(1.,.6,.4);
	slashN
	
	if (time > 2.) {DASH}
	if (time > 5.2) {M}
	if (time > 5.4) {I}
	if (time > 5.6) {R}
	if (time > 6.14) {R}
	if (time > 6.26) {O}
	if (time > 6.38) {R}
	if (time > 7.0) {DASH}
	if (time > 11.) {slashN}
	if (time > 11.8) {S}
	if (time > 12.) {A}
	if (time > 12.2) {L}
	if (time > 12.4) {U}
	if (time > 12.6) {T}
	if (time > 12.8) {slashN}
	if (time > 12.18) {T}
	if (time > 12.20) {R}
	if (time > 12.31) {S}
	if (time > 12.43) {I}
	if (time > 13.2) {DASH}
	if (time > 13.18) {S}
	if (time > 13.20) {C}
	if (time > 13.26) {E}
	if (time > 13.34) {N}
	if (time > 13.46) {E}
	if (time > 13.53) {S}
	if (time > 13.63) {A}
	if (time > 13.73) {T}
	if (time > 13.82) {slashN}
	if (time > 14.) {slashN}
	if (time > 14.5) {G R E E Z COLON N O V A L I S}
	if (time > 15.) {slashN}
	BLOCK slashN
	
	float textmix = smoothstep(0.020, 0.025, length(c));
		
	vec2 aspect = resolution.xy / resolution.y;
	vec2 uv = gl_FragCoord.xy / resolution.y;
	vec2 tuv = uv / vec2(sin(time),1.) + vec2(0.,sin(time*3.)*.05-.1);
	vec2 cen = aspect/2.0;
	vec2 tcen = cen / vec2(sin(time),1.);

	vec3 color = vec3(0);

	vec3 tricol = retrograd(0.1, 0.8, uv + vec2(0.,sin(time*3.)*.05 - .1));

	float tri = 1e4;
	for(int i = 0; i < 3; i++) {
		float a0 = tau * (float(i + 0) / 3.0) + (tau/12.0);
		float a1 = tau * (float(i - 1) / 3.0) + (tau/12.0);

		tri = min(tri, dline(0.4*vec2(cos(a0), sin(a0)), 0.4*vec2(cos(a1), sin(a1)), tuv - tcen));
	}

	tri = min(tri, abs(distance(tuv, tcen) - 0.13));

	tricol *= smoothstep(0.015,0.013, tri);

	float trimix = smoothstep(0.020,0.015,tri);

	vec2 gruv = uv-cen;
	gruv = vec2(gruv.x * abs(1.0/gruv.y), abs(1./gruv.y));
	gruv.y = clamp(gruv.y,-1e2,1e2);
	gruv.y += time;
	gruv.x += sin(time);

	float grid = 2. * cdist(vec2(.5), mod(gruv,vec2(1.)));
		
	float gridmix = max(pow(grid,6.) * 1.2, smoothstep(0.93,0.98,grid) * 3.);

	vec3 gridcol = (mix(vec3(0., 0., 0.9), vec3(0.9, 0., 0.9), uv.y*2.) + 1.2) * gridmix;
	gridcol *= linstep(0.1,1.5,abs(uv.y - cen.y));

	color = mix(gridcol, mix(c, tricol, trimix), trimix+textmix);
	
	gl_FragColor = vec4(color, 1.0);

}
