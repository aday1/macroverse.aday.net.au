/*{
    "DESCRIPTION": "pattern5",
    "CREDIT": "GLSL Sandbox / various",
    "ISFVSN": "2.0",
    "CATEGORIES": [
        "abstract"
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
        "abstract"
    ]
}*/



#define time ((useFrameIndex ? (float(FRAMEINDEX) / fps) : TIME) * timeScale)
#define mouse vec2(mouseX, mouseY)
#define resolution RENDERSIZE
#ifdef GL_ES
precision mediump float;
#endif

/* Made by Kriszti�n Szab� */
/* mod DamianPrg */
void main(){
	/* The light's positions */
	vec2 light_pos = resolution*mouse;
	/* The radius of the light */
	float radius = 1.0;
	/* Intensity range: 0.0 - 1.0 */
	float intensity = 0.2;
	
	/* Distance between the fragment and the light */
	float dist = distance(gl_FragCoord.xy, light_pos);
	
	/* Basic light color, change it to your likings */
	vec3 light_color = vec3(0.2, 1.0, 1.0);
	/* Alpha value of the fragment calculated based on intensity and distance */
	float alpha = 1.0 / (dist*intensity);
	
	/* The final color, calculated by multiplying the light color with the alpha value */
	vec4 final_color = vec4(light_color, 1.0)*vec4(alpha, alpha, alpha, alpha);
	
	final_color.rgb *= atan(dist)+sin(dist+time*50.0);
	final_color.rgb *= 2.0;
	
	//final_color.rgb *= atan(dist)+cos(dist);
	
	gl_FragColor = final_color;
	
	/* If you want to apply radius to the effect comment out the gl_FragColor line and remove comments from below: */

}
