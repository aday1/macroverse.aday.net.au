/*{
    "DESCRIPTION": "bubble field",
    "CREDIT": "GLSL Sandbox / various",
    "ISFVSN": "2.0",
    "CATEGORIES": ["Misc"],
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
    ]
}*/
uniform float BUBBLE_COUNT; // @expose
uniform float BG_BASE; // @expose
uniform float BG_GRADIENT; // @expose
uniform float SEED_SIZ_POWER; // @expose
uniform float RAD_BASE; // @expose
uniform float RAD_SCALE; // @expose
uniform float RISE_SPEED; // @expose
uniform float SPEED_MIN; // @expose
uniform float SPEED_MAX; // @expose
uniform float COLOR_FREQ; // @expose
uniform float COLOR_OFFSET; // @expose
uniform float EDGE_SOFTNESS; // @expose
uniform float VIGNETTE_BASE; // @expose
uniform float VIGNETTE_STRENGTH; // @expose


#define time (useFrameIndex ? (float(FRAMEINDEX) / fps) : TIME)
#define mouse vec2(0.0)
#define resolution RENDERSIZE

// --- Tunable constants ---
/* #define BUBBLE_COUNT overridden by param */
/* uniform BG_BASE from params */
/* uniform BG_GRADIENT from params */

const float SEED_PHA_FREQ   = 546.13;
const float SEED_PHA_OFFSET = 1.0;
const float SEED_SIZ_FREQ   = 651.74;
const float SEED_SIZ_OFFSET = 5.0;
/* uniform SEED_SIZ_POWER from params */

const float SEED_POX_FREQ   = 321.55;
const float SEED_POX_OFFSET = 4.1;
/* uniform RAD_BASE from params */
/* uniform RAD_SCALE from params */
/* uniform RISE_SPEED from params */
/* uniform SPEED_MIN from params */
/* uniform SPEED_MAX from params */

const vec3 COLOR_A = vec3(0.94, 0.3, 0.0); // @expose
const vec3 COLOR_B = vec3(0.1, 0.4, 0.8);  // @expose
/* uniform COLOR_FREQ from params */
/* uniform COLOR_OFFSET from params */
/* uniform EDGE_SOFTNESS from params */
/* uniform VIGNETTE_BASE from params */
/* uniform VIGNETTE_STRENGTH from params */

//
// Bubbles
//
// https://www.shadertoy.com/view/4dl3zn
//
// Created by inigo quilez - iq/2013
//
// License Creative Commons Attribution-NonCommercial-ShareAlike 3.0 Unported License.

void main(void)
{
	vec2 uv = -1.0 + 2.0*gl_FragCoord.xy / resolution.xy;
	uv.x *=  resolution.x / resolution.y;

    // background
	vec3 color = vec3(BG_BASE + BG_GRADIENT*uv.y);

    // bubbles
	for (int i=0; i < int(BUBBLE_COUNT); i++ )
	{
        // bubble seeds
		float pha =      sin(float(i)*SEED_PHA_FREQ+SEED_PHA_OFFSET)*0.5 + 0.5;
		float siz = pow( sin(float(i)*SEED_SIZ_FREQ+SEED_SIZ_OFFSET)*0.5 + 0.5, SEED_SIZ_POWER );
		float pox =      sin(float(i)*SEED_POX_FREQ+SEED_POX_OFFSET) * resolution.x / resolution.y;

        // bubble size, position and color
		float rad = RAD_BASE + RAD_SCALE*siz;
		vec2  pos = vec2( pox, -1.0-rad + (2.0+2.0*rad)*mod(pha+RISE_SPEED*time*(SPEED_MIN+SPEED_MAX*siz),1.0));
		float dis = length( uv - pos );
		vec3  col = mix( COLOR_A, COLOR_B, 0.5+0.5*sin(float(i)*COLOR_FREQ+COLOR_OFFSET));
		//    col+= 8.0*smoothstep( rad*EDGE_SOFTNESS, rad, dis );
		
        // render
		float f = length(uv-pos)/rad;
		f = sqrt(clamp(1.0-f*f,0.0,1.0));
		color -= col.zyx *(1.0-smoothstep( rad*EDGE_SOFTNESS, rad, dis )) * f;
	}

    // vignetting
	color *= sqrt(VIGNETTE_BASE-VIGNETTE_STRENGTH*length(uv));

	gl_FragColor = vec4(color,1.0);
}
