/*{
    "DESCRIPTION": "DotMatrix-Emerald-TextGlyph-2",
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
        "geometric",
        "texture-input"
    ]
}*/






#define time ((useFrameIndex ? (float(FRAMEINDEX) / fps) : TIME) * timeScale)
#define mouse vec2(mouseX, mouseY)
#define resolution RENDERSIZE
#ifdef GL_ES
precision mediump float;
#endif

#extension GL_OES_standard_derivatives : enable

uniform sampler2D backbuffer;

//	ボールの距離関数。
float point(vec3 p,vec3 ro, vec3 rd, float r)
{
	vec3 a = ro-p - rd * dot(ro-p, rd);
	return r/resolution.y - length(a);
}

//	地面の距離関数。
float ground( vec3 ro, vec3 rd){
	return rd.y;
}

//	gl_FragColorに1.0以上の値を入れないようフィルタをかける。
//	別に不要とは思うけど、元々想定が0.0～1.0という認識なので。
//	RGBごとにフィルタ処理するため、不思議な色の変化をしている。
//	todo:DSP処理で用いるフィルタ・EQ等のカーブを導入
vec4 filterVal( vec4 g ){
	vec4 r;
	r.x = (g.x>1.0 ) ? 1.0 : g.x;
	r.y = (g.y>1.0 ) ? 1.0 : g.y;
	r.z = (g.z>1.0 ) ? 1.0 : g.z;
	r.w = (g.w>1.0 ) ? 1.0 : g.w;	
	return r;
}

void main( void ) {

	//	お決まりの処理。
	vec2 uv = gl_FragCoord.xy / resolution.xy;
	vec2 p = (-1.0 + 2.0 * uv) * vec2(resolution.x/resolution.y, 1.0);
	vec2 m = (-1.0 + 2.0 * mouse.xy/resolution.xy) * vec2(resolution.x/resolution.y, 1.0);	
	vec3 ro = 2.8 * vec3(cos(0.2 * time), 1.0, sin(0.2 * time));
	
	//	正規化。
	vec3 ww = normalize(vec3(0.0, 0.0, 0.0) - ro);
	vec3 uu = normalize(cross(vec3(0.0, 1.0, 0.0), ww));
	vec3 vv = normalize(cross(ww, uu));
	
	//	レイの方向。
	vec3 rd = normalize(p.x * uu + p.y * vv + 3.6 * ww);
	vec3 col = vec3(1.0);
	
	//	事前に基本色を定義しておく。
	
	//	フラグメントシェーダによる最終的なテクスチャの色定義。
	vec4 total;
	
	//	ボールの色を格納する変数。（ボールがなかった時の色についてもこちらで定義）
	vec3 cl_ball=vec3( 0.05+uv.y,0.001+uv.y,0.01*uv.y)*0.446;
	
	//	地面の色を格納する変数。
	vec3 cl_ground;

	//	ボール・地面との距離を計測。
	//	ノーマルマップはなし
	for ( float j=1.0; j<10.; j+=1.0){
		for (float i=1.0; i<10.0; i+=1.0)
		{
			//	ボクセル的に分割したはずがそこから動きを調整したので、
			//	「とりあえず球を出してみた」的な意味合いの式と思っていただければ。
			float p = point(vec3(cos(time*4.0+i*j+.2*sin(time))/4.0+0.002*j, sin(6.0+i*sin(time)+j*5.)/0.8, i*j/3.0- 2.5), ro, rd, 59.0-j*cos(time*2.0+i*j));
		
			//	地面とカメラの距離。
			//	フェードしてわかりづらいけど、一応設置してるのです。
			float g = ground( ro, rd );
			
			//	ボールとカメラの距離チェック。
			if ( p> 0.0)
			{
				//	ボールの色決定。
				float cr = sin(time+i)/4.0;
				float cg = sin(time+i+1.0)/4.0;
				float cb = sin(time+i+2.0)/4.0;
			
				//	ボールの色を割当
				cl_ball = vec3(0.46+cr, 0.08+cg, 0.8+cb);
			}
		    
			//	地面とのヒットチェック。
			//	本来ならもう不要のはずなんだけど、
			//	色を混ぜまくってしまったので今更削除もできず・・・
			//	主にUV座標と組み合わせて使用。
			if( g <0.)
				cl_ground = vec3( 0.9,.5*uv.y*3.,0.9*uv);
			}
		}
		//	最小値を描画。（カメラに映る色を知らせる）
		col = min(cl_ball,cl_ground);
		//col = cl_ball;
	
	//	バックバッファを拡大した画像を定義。
	vec4 bf1=texture2D( backbuffer, uv)*(0.37+0.2*abs((sin(time)*cos(time) ) ) ) ;
	vec4 bf2 = texture2D( backbuffer, vec2(uv.x,uv.y+0.5)*0.65 )*0.17;
	vec4 bf3 = (texture2D( backbuffer, vec2( uv.x+0.5,uv.y+0.55)*0.6 ) )*0.1;
	
	//	最終的な色を決定。
	//	「計算した色・バックバッファ3枚」を合成している。
	total = vec4(col,1.0)*0.8 + bf1 + bf2 + bf3;
	
	//	フィルタをかける。
	gl_FragColor = filterVal( total);
}
