/*{
    "DESCRIPTION": "CityScapeXY",
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
        "3d"
    ]
}*/



#define time ((useFrameIndex ? (float(FRAMEINDEX) / fps) : TIME) * timeScale)
#define mouse vec2(mouseX, mouseY)
#define resolution RENDERSIZE
#ifdef GL_ES
precision highp float;
#endif

const float coiff = 1.0;

vec3 getAtmosphericScattering(vec2 uv){
	
	float zenith = (0.1 / uv.y * coiff); //Density of the atmosphere
	
	vec3 color = vec3(0.26,0.5,1.0) * zenith; //The color of the sky multiplied by the density
	color = pow(color, 1.0 - vec3(zenith));  //Make it so higher values gets scattered more than lower values based on density
	
	return max(color, 0.0); //Limit the final color to not go under 0.0
	
}

float random(float p) {
  return fract(sin(p)*10000.);
}
float noise(vec2 p) {
  return random(p.x + p.y*100.);
}

float city(vec2 uv){
	return 1.0 - step(random(floor(uv.x * 30.0)) * mouse.y + 0.05, uv.y);
}

void main( void ) {

	vec2 position = gl_FragCoord.xy / resolution.y;
	vec2 mou = vec2(mouse.x,0.);
	
	//position.x += mouse.x;
	//position.x += time*0.1;
	vec3 color = getAtmosphericScattering(position);
	     color /= 1.0 + color; //Fix color banding
	
	color += noise(position) * noise(position+1.0) * noise(position+2.0) * noise(position+3.0)
		* noise(position+4.0) * noise(position+5.0) * noise(position+6.0) ;

	color = mix(color, vec3(0.0), city(position + mou*0.1)*0.7 );

	color = mix(color, vec3(0.0), city(position*0.7 + mou));
	
	gl_FragColor = vec4(color, 1.0);

}
