/*{
    "DESCRIPTION": "HelloWorldItsTime",
    "CREDIT": "GLSL Sandbox / various",
    "ISFVSN": "2.0",
    "CATEGORIES": [
        "particles"
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
        "particles"
    ]
}*/



#define time ((useFrameIndex ? (float(FRAMEINDEX) / fps) : TIME) * timeScale)
#define mouse vec2(mouseX, mouseY)
#define resolution RENDERSIZE

// HelloWorldItsTime

#ifdef GL_ES
precision mediump float;
#endif

vec2 SPR_SIZE = vec2(6, 8);
vec2 start = vec2(0, 0);
vec2 tuv = vec2(0, 0);

vec2 c_a = vec2(0x7228BE, 0x8A2000);
vec2 c_b = vec2(0xF22F22, 0x8BC000);
vec2 c_c = vec2(0x722820, 0x89C000);
vec2 c_d = vec2(0xE248A2, 0x938000);
vec2 c_e = vec2(0xFA0E20, 0x83E000);
vec2 c_f = vec2(0xFA0E20, 0x820000);
vec2 c_g = vec2(0x72282E, 0x89C000);
vec2 c_h = vec2(0x8A2FA2, 0x8A2000);
vec2 c_i = vec2(0xF88208, 0x23E000);
vec2 c_j = vec2(0xF84104, 0x918000);
vec2 c_k = vec2(0x8A4A34, 0x8A2000);
vec2 c_l = vec2(0x820820, 0x83E000);
vec2 c_m = vec2(0x8B6AA2, 0x8A2000);
vec2 c_n = vec2(0x8B2AA6, 0x8A2000);
vec2 c_o = vec2(0x7228A2, 0x89C000);
vec2 c_p = vec2(0xF228BC, 0x820000);
vec2 c_q = vec2(0x7228AA, 0x99E000);
vec2 c_r = vec2(0xF228BC, 0x8A2000);
vec2 c_s = vec2(0x7A0702, 0x0BC000);
vec2 c_t = vec2(0xF88208, 0x208000);
vec2 c_u = vec2(0x8A28A2, 0x89C000);
vec2 c_v = vec2(0x8A28A2, 0x508000);
vec2 c_w = vec2(0x8A28AA, 0xDA2000);
vec2 c_x = vec2(0x8A2722, 0x8A2000);
vec2 c_y = vec2(0x8A2782, 0x89C000);
vec2 c_z = vec2(0xF84210, 0x83E000);
vec2 c_0 = vec2(0x732AA6, 0x89C000);
vec2 c_1 = vec2(0x218208, 0x23E000);
vec2 c_2 = vec2(0x722108, 0x43E000);
vec2 c_3 = vec2(0x722302, 0x89C000);
vec2 c_4 = vec2(0x92491E, 0x104000);
vec2 c_5 = vec2(0xFA0F02, 0x89C000);
vec2 c_6 = vec2(0x72283C, 0x89C000);
vec2 c_7 = vec2(0xF82108, 0x420000);
vec2 c_8 = vec2(0x722722, 0x89C000);
vec2 c_9 = vec2(0x722782, 0x89C000);
vec2 c_per = vec2(0x000000, 0x008000);
vec2 c_exc = vec2(0x208208, 0x008000);
vec2 c_com = vec2(0x000000, 0x008400);
vec2 c_col = vec2(0x008000, 0x008000);
vec2 c_sol = vec2(0x008000, 0x008400);
vec2 c_pls = vec2(0x00823E, 0x208000);
vec2 c_dsh = vec2(0x00003E, 0x000000);
vec2 c_div = vec2(0x002108, 0x420000);
vec2 c_ast = vec2(0x000508, 0x500000);
vec2 c_lbr = vec2(0x084104, 0x102000);
vec2 c_rbr = vec2(0x810410, 0x420000);
vec2 c_lsb = vec2(0x184104, 0x106000);
vec2 c_rsb = vec2(0xC10410, 0x430000);
vec2 c_lcb = vec2(0x184208, 0x106000);
vec2 c_rcb = vec2(0xC10208, 0x430000);
vec2 c_les = vec2(0x084208, 0x102000);
vec2 c_grt = vec2(0x408104, 0x210000);
vec2 c_sqo = vec2(0x208000, 0x000000);
vec2 c_dqo = vec2(0x514000, 0x000000);
vec2 c_que = vec2(0x72208C, 0x008000);
vec2 c_pct = vec2(0x022108, 0x422000);
vec2 c_dol = vec2(0x21EA1C, 0x2BC200);
vec2 c_num = vec2(0x53E514, 0xF94000);
vec2 c_ats = vec2(0x722BAA, 0xA9C000);
vec2 c_equ = vec2(0x000F80, 0xF80000);
vec2 c_tdl = vec2(0x42A100, 0x000000);
vec2 c_rsl = vec2(0x020408, 0x102000);
vec2 c_crt = vec2(0x214880, 0x000000);
vec2 c_amp = vec2(0x42842C, 0x99C000);
vec2 c_bar = vec2(0x208208, 0x208208);
vec2 c_blk = vec2(0xFFFFFF, 0xFFFFFF);
vec2 c_trd = vec2(0xFD5FD5, 0xFD5FD5);
vec2 c_hlf = vec2(0xA95A95, 0xA95A95);
vec2 c_qrt = vec2(0xA80A80, 0xA80A80);
vec2 c_spc = vec2(0x000000, 0x000000);

vec2 digit(float n)
{
  n = mod(floor(n),10.0);
  if(n == 0.0) return c_0;
  if(n == 1.0) return c_1;
  if(n == 2.0) return c_2;
  if(n == 3.0) return c_3;
  if(n == 4.0) return c_4;
  if(n == 5.0) return c_5;
  if(n == 6.0) return c_6;
  if(n == 7.0) return c_7;
  if(n == 8.0) return c_8;
  if(n == 9.0) return c_9;
  return vec2(0.0);
}

float ch(vec2 ch)
{
  vec2 b = vec2((SPR_SIZE.x - tuv.x - 1.0) + tuv.y * SPR_SIZE.x) - vec2(24,0);
  vec2 p = mod(floor(ch / exp2(clamp(b,-1.0, 25.0))), 2.0);
  float o = dot(p,vec2(1)) * float(all(bvec4(greaterThanEqual(tuv,vec2(0)), lessThan(tuv,SPR_SIZE))));
  tuv.x -= SPR_SIZE.x;
  return o;
}

vec2 str_size(vec2 cl)
{
	return SPR_SIZE * cl;
}

void start_print(vec2 uv, float x, float y)
{
	uv -= str_size(vec2(x,y)); 
	start = floor(uv);
	tuv = floor(uv);
}

void new_line()
{
  tuv.x = start.x;
  tuv.y += SPR_SIZE.y;
}

float c = 0.0;
	
void showValue (float value)
{
  for(int i = 4;i > -3;i--)
  {
    if(i == -1) c += ch(c_per);   // add dot
    c += ch(digit(value / pow(10.0,float(i))));
  }
}

void main( void ) 
{
  vec2 aspect = resolution.xy / resolution.y;
  vec2 uv = ( gl_FragCoord.xy / resolution.y );
  uv *= 60.0;
  uv = floor(uv);
  
  start_print(uv, 5., 6.);
  
  c += ch(c_spc) +ch(c_h) +ch(c_e) +ch(c_l) +ch(c_l) +ch(c_o) +ch(c_com);  // HELLO
  new_line();
  
  c += ch(c_spc) +ch(c_w) +ch(c_o) +ch(c_r) +ch(c_l) +ch(c_d) +ch(c_exc);  // WORLD
  new_line();
  
  c += ch(c_qrt);
  c += ch(c_hlf);
  c += ch(c_trd);
  c += ch(c_blk);
  c += ch(c_blk);
  c += ch(c_trd);	
  c += ch(c_hlf);
  c += ch(c_qrt);
  
  start_print(uv, 1., 2.7); 
  c += ch(c_per);
  c += ch(c_exc);
  c += ch(c_com);
  c += ch(c_col);
  c += ch(c_sol);
  c += ch(c_pls);
  c += ch(c_dsh);
  c += ch(c_div);
  c += ch(c_ast);
  c += ch(c_lbr);
  c += ch(c_rbr);
  c += ch(c_lsb);
  c += ch(c_rsb);
  c += ch(c_lcb);
  c += ch(c_rcb);
  c += ch(c_les);
  c += ch(c_grt);
     
  start_print(uv, 1., 1.7); 
  c += ch(c_sqo);
  c += ch(c_dqo);
  c += ch(c_que);
  c += ch(c_pct);
  c += ch(c_dol);
  c += ch(c_num);
  c += ch(c_ats);
  c += ch(c_equ);
  c += ch(c_tdl);
  c += ch(c_rsl);
  c += ch(c_crt);
  c += ch(c_amp);
  c += ch(c_bar);
  c += ch(c_blk);
  c += ch(c_trd);
  c += ch(c_hlf);
  c += ch(c_qrt);
   
  start_print(uv, 3., 0.2);
  c += ch(c_t);
  c += ch(c_i);
  c += ch(c_m);
  c += ch(c_e);
  c += ch(c_col);  
     
  showValue(time);	
     
  gl_FragColor = vec4( vec3( c,c,0.), 1.0 );
}

