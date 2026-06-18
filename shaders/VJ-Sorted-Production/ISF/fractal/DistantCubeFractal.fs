/*{
    "DESCRIPTION": "DistantCubeFractal",
    "CREDIT": "GLSL Sandbox / various",
    "ISFVSN": "2.0",
    "CATEGORIES": [
        "fractal"
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
        "fractal"
    ]
}*/



#define time ((useFrameIndex ? (float(FRAMEINDEX) / fps) : TIME) * timeScale)
#define mouse vec2(mouseX, mouseY)
#define resolution RENDERSIZE
#ifdef GL_ES
precision mediump float;
#endif

#extension GL_OES_standard_derivatives : enable

//https://www.shadertoy.com/view/ltGSWm  [Rusty_Baller]
float hash( float n ) 
{
    return fract(sin(n)*43758.5453123); 
}

// Noise
// x : Point in space
float noise( in vec3 x )
{
    vec3 p = floor(x);
    vec3 f = fract(x);
    f = f*f*(3.0-2.0*f);
	
    float n = p.x + p.y*157.0 + 113.0*p.z;
    return mix(mix(mix( hash(n+  0.0), hash(n+  1.0),f.x),
                   mix( hash(n+157.0), hash(n+158.0),f.x),f.y),
               mix(mix( hash(n+113.0), hash(n+114.0),f.x),
                   mix( hash(n+270.0), hash(n+271.0),f.x),f.y),f.z);
}
float turbulence(in vec3 p)
{ // Utiliser ces couleurs pour le métal rouillé.
    //const vec3 blue=vec3(1.52,0.87,0.23);
	//const vec3 green=vec3(0.5,0.5,0.5);

    // Add scaled noises
    float t=noise(p)*0.5;
    t+=0.25*noise(2.0*p);
    t+=0.125*noise(5.0*p);
    t+=0.025*noise(13.0*p);
    // Scale
    //t/=(0.6);//1.0+0.25+0.125+0.025
    // Modifier 0.8 pour changer la fréquence des taches. réduire 0.8 augmente la fréquence
    if(t>0.0 && t<= 0.4){
        return 0.6*0.46;
    }else{
        return t;
    }

}

const vec3 blue=vec3(1.0,0.30,0.0);
const vec3 green =vec3(0.5,0.5,0.5);

vec3 marble(in vec3 p)
{
    // Add scaled noises
    float t=turbulence (3.0*p);
    //t=1.0-pow((1.0-t*t),4.0);
    //t=0.5+abs(t-0.5);
    //t=1.0-pow((1.0-t*t),2.0);

	//return t*vec3(1.);
    if(t <= 0.8 && t> 0.3){
    	 return blue*0.8*t; 
    }
    if(t<=0.3){
       return blue*0.45*0.+vec3(1.);   
    } 

    return mix(blue,green,t);
}

         //反復回数(constで書く方も多い)
         #define ITE_MAX      80

         //t更新時の適当な係数。通常1で大丈夫です。
         //複雑な形状だったりレイ突き抜けるような小さいオブジェクトは値を0.25位にすると良いです
         #define DIST_COEFF   .86

         //打ち切り係数
         #define DIST_MIN     0.001

         //t最大
         #define DIST_MAX     10000.0

	#define inf 100000
	#define PI 3.14159276

	#define UnitWindow_Size 50.0

mat3 rotM(vec3 axis, float angle)
{
   
    axis = normalize(axis);
    float s = sin(angle);
    float c = cos(angle);
    float oc = 1.0 - c;
    
    return mat3(oc * axis.x * axis.x + c,           oc * axis.x * axis.y - axis.z * s,  oc * axis.z * axis.x + axis.y * s,  
                oc * axis.x * axis.y + axis.z * s,  oc * axis.y * axis.y + c,           oc * axis.y * axis.z - axis.x * s,  
                oc * axis.z * axis.x - axis.y * s,  oc * axis.y * axis.z + axis.x * s,  oc * axis.z * axis.z + c );
}

vec3 GenRay(vec2 p,vec3 dir,vec3 up,float angle){
	vec3 u = normalize(cross(dir,up));
	vec3 v = normalize(cross(u,dir));
	
	float fov = angle * PI * 0.5 / 180.;
return  normalize(sin(fov) * u * p.x + sin(fov) * v * p.y + cos(fov) * dir);
}

vec3 GenRay(vec3 dir,vec3 up,float angle)
{
 vec2 p = (gl_FragCoord.xy * 2.0 - resolution) / min(resolution.x, resolution.y);
vec3 u = normalize(cross(dir,up));
vec3 v = normalize(cross(u,dir));
	
float fov = angle * PI * 0.5 / 180.;

return  normalize(sin(fov) * u * p.x + sin(fov) * v * p.y + cos(fov) * dir);
}

float sdBox( vec3 p, vec3 b )
{
  vec3 d = abs(p) - b;
  return min(max(d.x,max(d.y,d.z)),0.0) +
         length(max(d,0.0));
}

float sdCross( in vec3 p )
{
  float da = sdBox(p.xyz,vec3(inf,1.0,1.0));
  float db = sdBox(p.yzx,vec3(1.0,inf,1.0));
  float dc = sdBox(p.zxy,vec3(1.0,1.0,inf));
  return min(da,min(db,dc));
}

float sdunit( in vec3 p )
{
  float da = sdBox(p.xyz,vec3(inf,.1,.1));
  float db = sdBox(p.yzx,vec3(.1,inf,.1));
  float dc = sdBox(p.zxy,vec3(.1,.1,inf));
  return min(da,min(db,dc));
}

float sphere(vec3 p,vec3 loc,float r){
    return length(p-loc) - r;
}

float map4( in vec3 p_ )
{
   vec3 p;
	p = rotM(normalize(vec3(1. + 3.*cos(time*3.),2.,3.)),time) * p_;
	p = p_;
   float d = sdBox(p,vec3(1.0));

   float s = 1.0;
   for( int m=0; m<3; m++ )
   {
      vec3 a = mod( p*s, 2.0 )-1.0;
      s *= 3.0;
      vec3 r = abs(1.0 - 3.0*abs(a));

      float da = max(r.x,r.y);
      float db = max(r.y,r.z);
      float dc = max(r.z,r.x);
      float c = (min(da,min(db,dc))-1.0)/s;

      d = max(d,c);
   }

   return d;
}
vec3 getnormal(vec3 p){
float d = 0.01;
return normalize(vec3(
	map4(p + vec3(d, 0.0, 0.0)) - map4(p + vec3(-d, 0.0, 0.0)),
	map4(p + vec3(0.0, d, 0.0)) - map4(p + vec3(0.0, -d, 0.0)),
	map4(p + vec3(0.0, 0.0, d)) - map4(p + vec3(0.0, 0.0, -d))
));
}

vec4 drawunit(vec3 camera_dir_,vec2 screen_pos){
		vec3 camera_dir = normalize(camera_dir_);
		 
         	//eye座標
         	vec3 pos = -1.*camera_dir;
         	vec3 target = vec3(0.);
		vec3 center_dir = (target-pos);
		vec3 dir = GenRay(screen_pos,normalize(center_dir),vec3(.0,1.,0.),120.);

         	//map関数で定義した形状を反復法で解きます。ここではt初期値は0にしとく
         	float t = 0.0;
         	
		 float dist = 0.;
		vec3 nowpos = pos + dir*0.2;
         	//SphereTracing。ここintersectって名前で別に作る人も多いです
         	for(int i = 0 ; i < ITE_MAX; i++) {

         		//形状表現した陰関数を反復しながら解く
         		//0(DIST_MIN)に近くなったら解に近いので打ち切り
         		dist = sdunit((t * dir + nowpos));
         		if(dist < DIST_MIN) {
				break;
			}
         		
         		//tを更新。DIST_COEFFは複雑な形状を描画する際に小さく為につけています。
         		//ちょっとずつレイを進める事ができます。
         		t += dist * DIST_COEFF;
			
			if(t > DIST_MAX){
				break;
			}
         	}
         	
         	//option形状の近くの位置を出しておく
         	vec3 ip = pos + dir * t;
         	//色を作ります。ここでは進めたtの位置(深度)をただ出力するだけ

         	vec3 color = vec3(0.2);
		//color = vec3(1./t);
		if(dist < DIST_MIN){
		float d = 0.01;
		normalize(vec3(
			sdunit(ip + vec3(d, 0.0, 0.0)) - sdunit(ip + vec3(-d, 0.0, 0.0)),
			sdunit(ip + vec3(0.0, d, 0.0)) - sdunit(ip + vec3(0.0, -d, 0.0)),
			sdunit(ip + vec3(0.0, 0.0, d)) - sdunit(ip + vec3(0.0, 0.0, -d))));
         	vec3 n = getnormal(ip);
		color = clamp(vec3(dot(ip,vec3(1.0,0.,0.)),dot(ip,vec3(0.,1.0,0.)),dot(ip,vec3(0.,0.,1.))),0.,1.);
		 }         	
         	//最後に色をつけておしまいです
         	return  vec4(color, 1.0);

}
	void main( void ) {
		vec3 l = normalize(vec3(.5,.99,.99));
		//eye座標
         	vec3 pos = vec3(2.1*sin(time*0.1) ,2.1*sin(time*0.3), + 2.1*cos(time*0.2));
         	vec3 target = vec3(0.);
		vec3 center_dir = (target-pos);
		vec3 dir;
		dir = GenRay(normalize(center_dir),vec3(.0,1.,0.),120.);
		
		//左上のやつを描画
		if(gl_FragCoord.x < UnitWindow_Size  && resolution.y - gl_FragCoord.y < UnitWindow_Size){
			vec2 reso = vec2(UnitWindow_Size);
			vec2 coord =vec2(gl_FragCoord.x ,gl_FragCoord.y-(resolution.y - UnitWindow_Size));
			vec2 p = (coord.xy * 2.0 - reso) / min(reso.x, reso.y);
			vec4 col = drawunit(center_dir,p);
			gl_FragColor = col;
			return ;
		}

         	//map関数で定義した形状を反復法で解きます。ここではt初期値は0にしとく
         	float t = 0.0;
         	
		 float dist = 0.;
         	//SphereTracing。ここintersectって名前で別に作る人も多いです
         	for(int i = 0 ; i < ITE_MAX; i++) {

         		//形状表現した陰関数を反復しながら解く
         		//0(DIST_MIN)に近くなったら解に近いので打ち切り
         		dist = map4((t * dir + pos));
         		if(dist < DIST_MIN) {
				break;
			}
         		
         		//tを更新。DIST_COEFFは複雑な形状を描画する際に小さく為につけています。
         		//ちょっとずつレイを進める事ができます。
         		t += dist * DIST_COEFF;
			
			if(t > DIST_MAX){
				break;
			}
         	}
         	
         	//option形状の近くの位置を出しておく
         	vec3 ip = pos + dir * t;
         	//色を作ります。ここでは進めたtの位置(深度)をただ出力するだけ

         	vec3 color = vec3(0.,0.,0.);
		if(dist < DIST_MIN){
         	vec3 n = getnormal(ip);
		float diff = dot(l,n);
		diff += dot(vec3(.0)-ip,n);
		color = vec3(0.3,0.4,0.4) * clamp(diff,0.1,1.);
		color += clamp(vec3(1.)*dot(normalize(dir+l),n)*0.5,0.,1.);
		color = color*marble(ip*10.);
		
		 }         	
		color += vec3(.5,.6,.9)*(1.-1./t);
         	gl_FragColor = vec4(color, 1.0);
}

void main2( void ) {

	vec2 p= ( gl_FragCoord.xy / resolution.xy ) + mouse / 4.0;

	vec3 color = vec3(0.0);
	color += marble(vec3(p.xy*10.,0.));

	gl_FragColor = vec4(vec3(1.0)*color,0.);

}
