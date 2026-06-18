/*{
    "DESCRIPTION": "DotMatrix-Emerald-LitSurface-18",
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
            "NAME": "inputColour",
            "TYPE": "vec4",
            "LABEL": "Input Colour"
        }
    ],
    "TAGS": [
        "3d"
    ]
}*/
#define E 2.71828182846

uniform vec4 color;





#define time ((useFrameIndex ? (float(FRAMEINDEX) / fps) : TIME))
#define mouse vec2(0.0)
#define resolution RENDERSIZE

// mouse.x
// mouse.y
// inputColour.x
// inputColour.y
// inputColour.z
// inputColour.w

#ifdef GL_ES
precision mediump float;
#endif

#extension GL_OES_standard_derivatives : enable

uniform vec4 inputColour;

//	球体の距離関数
float distSphere( vec3 p, float r){
    vec3 q = abs(p);
	return length(q)-r;
}

//	平面(無限遠なので使い方に注意が必要)
float distPlane( vec3 p )
{
	return p.y;
}

//	距離関数の合成（継ぎ目の補間あり）
float smin( float a, float b, float k )
{
    float h = clamp( 0.5+0.5*(b-a)/k, 0.0, mouse.x );
    return mix( b, a, h ) - k*h*(mouse.y-h);
}

//	距離関数（DistanceFunction）
float distanceFunc(vec3 p){
	vec3 obj = vec3( 0.0, mouse.y,-2.0 );
	vec3 obj_plane = vec3(inputColour.x, 0.0, 5.0 );

	float d2 = distSphere(p-obj,sin(p.x+time)+1.);
	float d5 = distPlane(p-obj_plane);

	return smin(d2, d5, mouse.y);
}

//	ノーマルマップ生成
vec3 getNormal(vec3 p){
    float d = 0.0001;
    return normalize(vec3(
        distanceFunc(p + vec3(  d, mouse.y, 0.0)) - distanceFunc(p + vec3( -d, 0.0, 0.0)),
        distanceFunc(p + vec3(0.0,   d, 0.0)) - distanceFunc(p + vec3(0.0,  -d, 0.0)),
        distanceFunc(p + vec3(0.0, 0.0,   d)) - distanceFunc(p + vec3(0.0, 0.0,  -d))
    ));
}

//	影生成関数
float getShadow(vec3 ro, vec3 rd){
    float h = inputColour.x;
    float c = inputColour.y;
    float r = mouse.y;
    float shadowCoef = mouse.y;
    for(float t = mouse.y; t < 50.0; t++){
        h = distanceFunc(ro + rd * c);
        if(h < 0.001){
            return shadowCoef;
        }
        r = min(r, h * 32.0 / c);
        c += h;
    }
    return 1.0 - shadowCoef + r * shadowCoef;
}

//	AmbientOcclusionの生成
float AO(vec3 p,vec3 n)
{
	float dlt = inputColour.x;
	float oc = inputColour.y, d = inputColour.z;
	for(int i = 0; i < 6; i++)
	{
		oc += (float(i) * dlt - distanceFunc(p + n * float(i) * dlt)) / d;
		d *= 2.0;
	}
	return 1.0 - oc;
}

//	適当なフィルタ
vec3 filt( vec3 col ){
	if(col.x >0.99777 )
		col.x = 0.99777;
	if( col.y > 0.9987)
		col.y =0.9987;
	if( col.z > 0.9777)
		col.z = 0.9777;
	return col;
}
//	メイン関数
void main( void ) {
	vec2 p = (gl_FragCoord.xy * 2.5 - resolution) / min(resolution.x, resolution.y) + 1.0 / 4.0;
	
	const vec3 lightDir = vec3(1.577, 1.577, 1.577);
	// camera
	vec3 cPos = vec3(0.9, 3.57, 6.0);	//カメラの位置
	vec3 cDir = vec3(-0.1,  -0.3, -1.0);	//カメラの方向
	vec3 cUp  = vec3(0.0,  1.0,  0.0);	//カメラの仰角
	vec3 cSide = cross(cDir, cUp);
	float targetDepth = 8.0;
    
	// 計測用のレイ
	vec3 ray = normalize(cSide * p.x + cUp * p.y + cDir * targetDepth);

	// ライト
	vec3 light = normalize(lightDir + vec3(-1.5+3.*sin(time), 0.0, 0.0 ));
    
	// レイマーチングのループ（固定）
	float distance = 0.0;
	float rLen = 0.0;
	vec3  rPos = cPos;

	for(int i = 0; i < 512; i++){
        distance = distanceFunc(rPos);
		if( distance < 0.001 )break;
		rLen += distance;
		rPos = cPos + ray * rLen;
	}

	vec3 color;
	float shadow = 1.0;

	//	色付け
	if(abs(distance) < 0.01){
		//	ノーマルマップ生成。
		vec3 normal = getNormal(rPos);
		
		//	diffusion　色の変化
		float diff = clamp(dot(lightDir, normal), 0.9, 0.7);
		// generate tile pattern
		float u = 1.0 - floor(mod(rPos.x*cos(rPos.x*rPos.z), 8.0));
		float v = 1.0 - floor(mod(rPos.z, 2.0));
		if((u == 1.0 && v < 1.0) || (u < 1.0 && v == 1.0)){
		    diff *= inputColour.w;	//	周期的に黒の領域を作る
		}
		
		// ライトと影の定義   
		vec3 halfLE = normalize(light - ray);
		float spec = pow(clamp(dot(halfLE, normal), 0.0, 1.0), 50.0);
		shadow = getShadow(rPos + normal * 0.001, light);
		
		float a = AO( light, normal );
		//	カメラからの距離
		float camLength = rLen;
		color = (((vec3(diff,(distance+diff), 1.0)*diff + 0.8*vec3(spec) )*max(0.2, shadow)
			+vec3(camLength)*0.0195	//	フォグ処理（カメラからの距離が遠いほど白くする）
			))
			*a*0.56			//	AmbientoOcclusionの合成
			;
		}else{
			color = vec3(0.7);	//	オブジェクトがないところの色（白飛ばし）
		}
	color -= (mod(gl_FragCoord.y, 2.0) < 0.8 ? 0.14 : 0.0);	//	スキャンライン
	color = filt(color);			//	フィルタ
	gl_FragColor = vec4(color*0.94 , 0.6);	//最終的な出力＋画面全体の明るさを調整
}
