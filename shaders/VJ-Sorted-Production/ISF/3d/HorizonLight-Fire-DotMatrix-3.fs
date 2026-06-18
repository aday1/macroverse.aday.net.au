/*{
    "DESCRIPTION": "HorizonLight-Fire-DotMatrix-3",
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
/*
Author: Leon Miura
Email: (Author's full name without spaces)@gmail.com
Title: Pac-Man tribute shader
*/

#ifdef GL_ES
precision mediump float;
#endif

void main( void ) {

	//--- DRAW BACKGROUND---
	vec2 position = gl_FragCoord.xy;
	gl_FragColor = vec4(cos(position.x*0.2 * time * 0.01) * 0.04,cos(position.x*0.2 * time * 0.01) * 0.04,0.0,1.0);

	//---PAC-DOTS---
	float rpd = 3.0;		//pac-dot radius
	vec4 colorpd = vec4( 1.0,1.0,sin(time * 20.0), 1.0);
	float radio_brillo = 5.0;	//pac-dot glow radius
	float decaimiento;		//pac-dot glow decay
	const int NUM_PAC_DOTS = 7;	//how many pac-dots
	vec2 pac_dots_pos[NUM_PAC_DOTS];//pac-dot position array
	vec2 posicion_p1;		//first pac-dot's position
	posicion_p1.x = resolution.x / 4.0;
	posicion_p1.y = resolution.y/2.0;
	float dist_puntos = 80.0;	//horizontal distance between consecutive pac-dots

	//---GHOST---
	vec2 pos_fantasma;
	pos_fantasma.x = resolution.x * 0.75;
	pos_fantasma.y = resolution.y/2.0 + sin(time * 1.5) * 100.0;
	float radio_fantasma = 20.0;
	float entre_ojos_fantasma = 7.0;
	float radio_ojos_fantasma = 5.0;
	float radio_pupilas_fantasma = 2.0;
	vec4 color_blanco = vec4(1.0,1.0,1.0,1.0);
	vec4 color_fantasma = vec4(1.0,0.0,0.0,1.0);
	vec4 color_pupilas_fantasma = vec4(0.0,0.0,1.0,1.0);

	//---TRACK---
	float ancho_calle = 60.0;
	float ancho_pared = 2.0;
	vec4 color_pared = vec4(cos(position.x*0.01) * 3.0,1.0,cos(position.x*0.01) * 3.0,1.0);
	vec4 color_calle = vec4(0.0,0.0,sin(position.x*time*0.006),1.0);
	float calle_horizontal_y = resolution.y/2.0;
	float calle_vertical_x   = resolution.x * 0.75;

	//---PAC-MAN---
	vec4 color_pacman = vec4(1.0,0.8,0.0,1.0);
	float radio_pacman = 20.0;
	float angulo;			//mouth opening angle
	float variacion_angulo = 0.30 * sin(time * 12.0) + 0.3;	//base angle + oscillation + offset
	vec2 centro;			//pac-man's center point
	vec2 horizontal = vec2(1.0,0.0);//horizontal vector (1,0)
	int direccion = 0;		//pac-man's movement direction (0: right, 1:left)
	float oscilacion = time * 1.5; 	//horizontal movement oscillation

	centro.x = resolution.x/2.0 + sin(oscilacion) * 300.0;	//place pac-man in the horizontal center of the screen and add sinusoidal oscillation
	centro.y = resolution.y/2.0;				//place pac-man in the vertical center of the screen
	
	vec2 orientacion = normalize(position - centro);	//points the mouth left or right

	//---DEFINE PAC-MAN'S ORIENTATION AND MOVEMENT DIRECTION---
	//the derivative of pac-man's x-coordinate tells us whether the horizontal position is increasing or decreasing
	//if it increases, the direction is "right" (0); if it decreases, direction is "left" (1)
	//(derivtive of sin(x)) = cos(x)
	if(cos(oscilacion) < 0.0)
	{
		direccion = 1;
	}
	
	if(direccion == 1)
	{
		orientacion.xy *= -1.0;
	}

	angulo = acos(dot(horizontal, orientacion));	//mouth opening angle

	//---DRAW TRACK'S "TARMAC"---
	//horizontal road
	if((position.y > calle_horizontal_y - ancho_calle/2.0) &&
	   (position.y < calle_horizontal_y + ancho_calle/2.0))
	{
		gl_FragColor = color_calle;
	}

	//vertical road
	if((position.x > calle_vertical_x - ancho_calle/2.0 + ancho_pared) && 
	   (position.x < calle_vertical_x + ancho_calle/2.0))
	{
		gl_FragColor = color_calle;
	}

	//---DRAW TRACK WALLS---
	//horizontal walls
	if(((position.y - ancho_calle/2.0 > calle_horizontal_y && position.y - ancho_calle/2.0  < calle_horizontal_y + ancho_pared) ||
	   (position.y + ancho_calle/2.0 > calle_horizontal_y && position.y + ancho_calle/2.0  < calle_horizontal_y + ancho_pared)) &&
	   ((position.x < calle_vertical_x - ancho_calle/2.0) || (position.x > calle_vertical_x + ancho_calle/2.0)))
	{
		gl_FragColor = color_pared;	
	}
	
	//vertical walls
	if(((position.x > calle_vertical_x - ancho_calle/2.0  && position.x < calle_vertical_x - ancho_calle/2.0 + ancho_pared) ||
	   (position.x > calle_vertical_x + ancho_calle/2.0  && position.x < calle_vertical_x + ancho_calle/2.0 + ancho_pared)) && 
	   ((position.y > calle_horizontal_y + ancho_calle/2.0) || (position.y < calle_horizontal_y - ancho_calle/2.0 + ancho_pared)))
	{
		gl_FragColor = color_pared;	
	}

	//---DRAW PAC-MAN---
	if((distance(position,centro) < radio_pacman) && angulo > variacion_angulo)
	{
		gl_FragColor = color_pacman;
	}

	//DRAW PAC-DOTS
	for(int i = 0; i < NUM_PAC_DOTS; i++)
	{
		//set ith pac-dot position
		pac_dots_pos[i].x = posicion_p1.x + float(i) * dist_puntos;
		pac_dots_pos[i].y = resolution.y / 2.0;

		//draw ith pac-dot
		if((direccion == 0) && (centro.x < pac_dots_pos[i].x - radio_pacman/2.0) && (distance(position,pac_dots_pos[i]) < rpd + radio_brillo))
		{
			//draw ith pac-dot's nucleus
			if(distance(position,pac_dots_pos[i]) < rpd)
			{
				gl_FragColor = colorpd;//vec4( 1.0,1.0,sin(time * 20.0), 1.0);
			}
			
			//draw ith pac-dot's glow
			decaimiento = exp(distance(position,pac_dots_pos[i])/1.5);	//exponential glow decay
			gl_FragColor += vec4( 10.0/decaimiento,10.0/decaimiento,sin(time * 20.0)/decaimiento, 10.0/decaimiento);		
		}
	}

	//---DIBUJAR EL FANTASMA---
	//draw ghost's body
	if(((distance(position,pos_fantasma) < radio_fantasma) || 
	   (position.y < pos_fantasma.y && position.x > pos_fantasma.x - radio_fantasma && (position.x < pos_fantasma.x + radio_fantasma))) &&
	   (position.y > pos_fantasma.y - radio_fantasma - sin(position.x * 0.75) * 2.0 ))
	{
		gl_FragColor = color_fantasma;
	}
	
	//draw white part of ghost's eyes
	vec2 pos_ojo_izq;
	pos_ojo_izq.x = pos_fantasma.x - entre_ojos_fantasma;
	pos_ojo_izq.y = pos_fantasma.y + 6.0;
	
	vec2 pos_ojo_der;
	pos_ojo_der.x = pos_fantasma.x + entre_ojos_fantasma;
	pos_ojo_der.y = pos_fantasma.y + 6.0;

	if(distance(position,pos_ojo_izq) < radio_ojos_fantasma)
	{
		gl_FragColor = color_blanco;
	}
	
	if(distance(position,pos_ojo_der) < radio_ojos_fantasma)
	{
		gl_FragColor = color_blanco;
	}
	
	//draw ghost's pupils
	vec2 pos_pup_izq;
	pos_pup_izq.x = pos_fantasma.x - entre_ojos_fantasma;
	pos_pup_izq.y = pos_fantasma.y + 9.0;
	
	vec2 pos_pup_der;
	pos_pup_der.x = pos_fantasma.x + entre_ojos_fantasma;
	pos_pup_der.y = pos_fantasma.y + 9.0;
	
	if(distance(position,pos_pup_izq) < radio_pupilas_fantasma)
	{
		gl_FragColor = color_pupilas_fantasma;
	}
	
	if(distance(position,pos_pup_der) < radio_pupilas_fantasma)
	{
		gl_FragColor = color_pupilas_fantasma;
	}	

}
