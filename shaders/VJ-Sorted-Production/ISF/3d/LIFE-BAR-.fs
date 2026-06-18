/*{
    "DESCRIPTION": "LIFE-BAR-",
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
precision mediump float;
#endif

vec2 position;
vec2 mousePos;

float heartSize = 10.0;
//vec2 heart0 = vec2(40.0, resolution.y - 40.0);

float heartFillPct;
float magicFillPct;

void drawBackground( void )
{
	float color = 0.0;
	color += sin( position.x * cos( time / 15.0 ) * 80.0 ) + cos( position.y * cos( time / 15.0 ) * 10.0 );
	//color += sin( position.y * sin( time / 10.0 ) * 40.0 ) + cos( position.x * sin( time / 25.0 ) * 40.0 );
	//color += sin( position.x * sin( time / 5.0 ) * 10.0 ) + sin( position.y * sin( time / 35.0 ) * 80.0 );
	color *= sin( time / 00.0 ) * 0.5; // 00.0 = NOTHING

	gl_FragColor = vec4( vec3( color, color * 0.5, sin( color + time / 2.0) * 0.75 ), 1.0 );
}

void drawCircle( void )
{
	float distanceX, distanceY, dist;
	
	distanceX = abs(gl_FragCoord.x - mousePos.x);
	distanceY = abs(gl_FragCoord.y - mousePos.y);
	
	dist = sqrt((distanceX * distanceX) + (distanceY * distanceY));
	
	float radius = resolution.y / 10.0;
	
	if (dist < radius)
	{
		gl_FragColor = vec4(0.0, 1.0, 0.0, 1.0);	
	}
}

void drawHeart( vec2 heartPos, bool pulse, float fill)
{
	float distanceX, distanceY, dist;
	
	distanceX = abs(gl_FragCoord.x - heartPos.x);
	distanceY = abs(gl_FragCoord.y - heartPos.y);
	
	dist = sqrt((distanceX * distanceX) + (distanceY * distanceY));
	
	float radius = heartSize;
	
	if (pulse)
	{
		radius *= sin( (time * 2.0)) + ((heartSize / 6.0));
		if (radius < heartSize * 0.75)
			radius = heartSize * 0.75;
		if (radius > heartSize * 1.25)
			radius = heartSize * 1.25;
	}
	
	if (dist < radius)
	{
		if (dist > radius * 0.8)
		{
			gl_FragColor = vec4(1.0, 1.0, 1.0, 1.0);
		}
		else
		{
			if (fill >= 1.0)
			{
				gl_FragColor = vec4(1.0, 0.0, 0.0, 1.0);
			}
			else if (fill > 0.0)
			{
				if (heartPos.x > gl_FragCoord.x)
				{
					gl_FragColor = vec4(1.0, 0.0, 0.0, 1.0);
				}
				else
				{
					gl_FragColor = vec4(0.5, 0.5, 0.5, 1.0);	
				}
			}
			else
			{
				gl_FragColor = vec4(0.5, 0.5, 0.5, 1.0);	
			}
		}
	}
}

// Move the mouse cursor vertically to change the hearts.
void drawHearts( void )
{	
	heartFillPct = mousePos.y / resolution.y * 20.0;
	
	float heartIndex = 1.0;
	for (float row = 0.0; row <= 1.0; row++)
	{
		for (float col = 1.0; col <= 10.0; col++)
		{
			vec2 heartPos = vec2(40.0, resolution.y - 40.0);
			heartPos.x *= col / 1.8;
			heartPos.y -= row * 40.0 / 1.8;
			
			bool pulse = (heartIndex == ceil(heartFillPct));
			
			float fill = 1.0;
			if (pulse && heartFillPct / heartIndex < 0.5)
			{
				fill = 0.5;
			}
			else if (heartIndex > ceil(heartFillPct))
			{
				fill = 0.0;
			}
			
			drawHeart(heartPos, pulse, fill);
			
			heartIndex++;
		}
	}
}

// Move the mouse cursor horizontally to change the magic
void drawMagicBar( void )
{
	vec2 barPos = vec2(10.0, resolution.y - 100.0);
	vec2 barSize = vec2(220.0, 20.0);
	
	magicFillPct = mousePos.x / resolution.x * barSize.x;
	
	vec2 pos = gl_FragCoord.xy;
	
	if (pos.x >= barPos.x && pos.x <= barPos.x + barSize.x &&
	    pos.y >= barPos.y && pos.y <= barPos.y + barSize.y)
	{
		if (pos.x >= barPos.x + 5.0 && pos.x <= barPos.x + barSize.x - 5.0 &&
		    pos.y >= barPos.y + 5.0 && pos.y <= barPos.y + barSize.y - 5.0)
		{
			if (((pos.x - 5.0) / barSize.x) * (barSize.x) > magicFillPct)
			{
				gl_FragColor = vec4(0.0, 0.0, 0.0, 1.0);	
			}
			else
			{
				gl_FragColor = vec4(0.0, 1.0, 0.0, 1.0);
			}
		}
		else
		{
			gl_FragColor = vec4(1.0, 1.0, 1.0, 1.0);	
		}
	}
}

void main( void )
{
	position = ( gl_FragCoord.xy / resolution.xy ) + mouse / 8.0;
	mousePos = mouse * resolution;
	
	drawBackground();
	
	//drawCircle();
	
	//drawHeart(heart0, true);
	drawHearts();
	
	drawMagicBar();
}
