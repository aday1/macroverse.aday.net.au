/*{
    "DESCRIPTION": "DotMatrix-Emerald-LitSurface-19",
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

#extension GL_OES_standard_derivatives : enable

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
    float h = clamp( 0.5+0.5*(b-a)/k, 0.0, 1.0 );
    return mix( b, a, h ) - k*h*(1.0-h);
}

float opU( float d1, float d2 )
{
	return (d1<d2) ? d1 : d2;
}
//	距離関数（DistanceFunction）
float distanceFunc(vec3 p){
	vec3 obj = vec3( cos(time), 0.5+cos(time),-5.4+2.*cos(time*2.) );
	vec3 obj_plane = vec3(0.0, 0.0, 5.0 );

	const int x = 6;
	float d_set=0.0;
	float d_result = 0.0;
	
	float d2 = distSphere(p-obj, 0.86);
	float d5 = distPlane(p-obj_plane);

	return smin(d2, d5, 0.9);
}

//	ノーマルマップ生成
vec3 getNormal(vec3 p){
    float d = 0.0001;
    return normalize(vec3(
        distanceFunc(p + vec3(  d, 0.0, 0.0)) - distanceFunc(p + vec3( -d, 0.0, 0.0)),
        distanceFunc(p + vec3(0.0,   d, 0.0)) - distanceFunc(p + vec3(0.0,  -d, 0.0)),
        distanceFunc(p + vec3(0.0, 0.0,   d)) - distanceFunc(p + vec3(0.0, 0.0,  -d))
    ));
}

//	影生成関数
float getShadow(vec3 ro, vec3 rd){
    float h = 0.0;
    float c = 0.0;
    float r = 1.0;
    float shadowCoef = 0.5;
    for(float t = 0.0; t < 50.0; t++){
        h = distanceFunc(ro + rd * c);
        if(h < 0.001){
            return shadowCoef;
        }
        r = min(r, h * 16.0 / c);
        c += h;
    }
    return 1.0 - shadowCoef + r * shadowCoef;
}

//	AmbientOcclusionの生成
float AO(vec3 p,vec3 n)
{
	float dlt = 0.5;
	float oc = 0.0, d = 1.0;
	for(int i = 0; i < 6; i++)
	{
		oc += (float(i) * dlt - distanceFunc(p + n * float(i) * dlt)) / d;
		d *= 2.0;
	}
	return 2.0 - oc;
}

//	適当なフィルタ
vec3 filt( vec3 col ){
	if(col.x >0.977 )
		col.x = 0.99777;
	if( col.y > 0.9987)
		col.y =0.9987;
	if( col.z > 0.9777)
		col.z = 0.9777;
	return col;
}
//	メイン関数
void main( void ) {
	vec2 p = (gl_FragCoord.xy * 2.0 - resolution) / min(resolution.x, resolution.y) + mouse / 4.0;
	
	const vec3 lightDir = vec3(1.577, 1.577, 1.577);
	// camera
	vec3 cPos = vec3(0.9, 3.57, 6.0);	//カメラの位置
	vec3 cDir = vec3(-0.1,  -0.3, -1.0);	//カメラの方向
	vec3 cUp  = vec3(0.0,  1.0,  0.0);	//カメラの仰角
	vec3 cSide = cross(cDir, cUp);
	float targetDepth = 6.0;
    
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
	if(abs(distance) < 0.001){
		//	ノーマルマップ生成。
		vec3 normal = getNormal(rPos);
		
		//	diffusion　色の変化
		float diff = clamp(dot(lightDir, normal), 0.9, 0.7);
		// generate tile pattern
		float u = 1.0 - floor(mod(rPos.y*cos(rPos.x*rPos.z), 8.0));
		float v = 1.0 - floor(mod(rPos.z, 2.0));
		if((u == 1.0 && v < 1.0) || (u < 1.0 && v == 1.0)){
		    diff *= 0.5*sin(time);	//	周期的に黒の領域を作る
		}
		
		// ライトと影の定義
		vec3 halfLE = normalize(light - ray);
		float spec = pow(clamp(dot(halfLE, normal), 0.0, 1.0), 50.0);
		shadow = getShadow(rPos + normal * 0.001, light);
		
		float a = AO( light, normal );
		//	カメラからの距離
		float camLength = rLen;
		color = (((vec3(diff,(distance+diff), 1.0)*diff + 0.8*vec3(spec) )*max(0.2, shadow)
			+vec3(camLength)*0.0195*sin(time)	//	フォグ処理（カメラからの距離が遠いほど白くする）
			))
			*a*0.66			//	AmbientoOcclusionの合成
			;
		}else{
			color = vec3(0.7);	//	オブジェクトがないところの色（白飛ばし）
		}
	color -= (mod(gl_FragCoord.y, 2.0) < 0.8 ? 0.14 : 0.0);	//	スキャンライン
	color = filt(color);			//	フィルタ
	gl_FragColor = vec4(color*0.94 , 0.6);	//最終的な出力＋画面全体の明るさを調整
}
