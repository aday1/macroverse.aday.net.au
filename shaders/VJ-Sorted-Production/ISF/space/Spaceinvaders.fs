/*{
    "DESCRIPTION": "Spaceinvaders",
    "CREDIT": "GLSL Sandbox / various",
    "ISFVSN": "2.0",
    "CATEGORIES": [
        "space"
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
        "space"
    ]
}*/



#define time ((useFrameIndex ? (float(FRAMEINDEX) / fps) : TIME) * timeScale)
#define mouse vec2(mouseX, mouseY)
#define resolution RENDERSIZE
#ifdef GL_ES
precision mediump float;
#endif

//Testing a sprite packed into the bits of 9 numbers

float row0 = 1728.0;
float row1 = 10280.0;
float row2 = 12264.0;
float row3 = 16376.0;
float row4 = 7088.0;
float row5 = 4064.0;
float row6 = 1088.0;
float row7 = 2080.0;
float row8 = 0.0;

float getBit(float num,float bit)// Retrieves a given bit from a number
{
	float rshift = pow(2.0,floor(bit));
	
	num = floor(num / rshift);
	
	return ((num / 2.0) == floor(num/2.0)) ? 0.0 : 1.0;
}

float getSprite(vec2 p)
{
	p = mod(floor(p),vec2(16,9));
	
	if(p.y == 0.0){return getBit(row0,p.x);}
	if(p.y == 1.0){return getBit(row1,p.x);}
	if(p.y == 2.0){return getBit(row2,p.x);}
	if(p.y == 3.0){return getBit(row3,p.x);}
	if(p.y == 4.0){return getBit(row4,p.x);}
	if(p.y == 5.0){return getBit(row5,p.x);}
	if(p.y == 6.0){return getBit(row6,p.x);}
	if(p.y == 7.0){return getBit(row7,p.x);}
	if(p.y == 8.0){return getBit(row8,p.x);}
	return 0.0;
}

void main( void ) {

	vec2 p = ( gl_FragCoord.xy / resolution.xy );
	p = vec2(1.-p.x,p.y);
	
	p *= vec2(32,18);

	float color = 0.0;	

	color = getSprite(p);
	
	gl_FragColor = vec4( vec3( color ), 1.0 );

}
