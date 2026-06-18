/*{
    "DESCRIPTION": "DotMatrix-OrganicForm-4",
    "CREDIT": "GLSL Sandbox / various",
    "ISFVSN": "2.0",
    "CATEGORIES": [
        "tunnel"
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
            "NAME": "speed",
            "TYPE": "float",
            "DEFAULT": 1.0,
            "MIN": 0.0,
            "MAX": 5.0,
            "LABEL": "Speed"
        },
        {
            "NAME": "mouseX",
            "TYPE": "float",
            "DEFAULT": 0.0,
            "MIN": -1.0,
            "MAX": 1.0,
            "LABEL": "Mouse X"
        },
        {
            "NAME": "mouseY",
            "TYPE": "float",
            "DEFAULT": 0.0,
            "MIN": -1.0,
            "MAX": 1.0,
            "LABEL": "Mouse Y"
        },
        {
            "NAME": "zoom",
            "TYPE": "float",
            "DEFAULT": 1.0,
            "MIN": 0.1,
            "MAX": 4.0,
            "LABEL": "Zoom"
        },
        {
            "NAME": "colorR",
            "TYPE": "float",
            "DEFAULT": 1.0,
            "MIN": 0.0,
            "MAX": 2.0,
            "LABEL": "Color Red"
        },
        {
            "NAME": "colorG",
            "TYPE": "float",
            "DEFAULT": 1.0,
            "MIN": 0.0,
            "MAX": 2.0,
            "LABEL": "Color Green"
        },
        {
            "NAME": "colorB",
            "TYPE": "float",
            "DEFAULT": 1.0,
            "MIN": 0.0,
            "MAX": 2.0,
            "LABEL": "Color Blue"
        },
        {
            "NAME": "brightness",
            "TYPE": "float",
            "DEFAULT": 0.0,
            "MIN": -1.0,
            "MAX": 1.0,
            "LABEL": "Brightness"
        },
        {
            "NAME": "saturation",
            "TYPE": "float",
            "DEFAULT": 1.0,
            "MIN": 0.0,
            "MAX": 3.0,
            "LABEL": "Saturation"
        },
        {
            "NAME": "contrast",
            "TYPE": "float",
            "DEFAULT": 1.0,
            "MIN": 0.0,
            "MAX": 3.0,
            "LABEL": "Contrast"
        },
        {
            "NAME": "hueShift",
            "TYPE": "float",
            "DEFAULT": 0.0,
            "MIN": 0.0,
            "MAX": 1.0,
            "LABEL": "Hue Shift"
        },
        {
            "NAME": "invert",
            "TYPE": "bool",
            "DEFAULT": 0,
            "LABEL": "Invert Colors"
        }
    ],
    "TAGS": [
        "tunnel",
        "texture-input"
    ]
}*/




#define time ((useFrameIndex ? (float(FRAMEINDEX) / fps) : TIME) * speed)
#define mouse vec2(mouseX, mouseY)
#define resolution (RENDERSIZE / zoom)


#ifdef GL_ES
precision mediump float;
#endif

// "Snake" by Kabuto

// Make sure your browser window is not too narrow.
// With standard quality (2) selected minimum width is about 520 pixels.

// The snake tries to follow your mouse pointer. Eat red bubbles to grow. Eating yourself ends the game.

// Eat 16 bubbles to advance one level. What do you expect will happen when you reach level 2? :-) There are 16 levels total.

uniform sampler2D backbuffer;

void _userMain( void ) {
	vec4 cPrev = texture2D(backbuffer, gl_FragCoord.xy / resolution);
	vec4 t0 = texture2D(backbuffer, vec2(0.5)/resolution.xy);
	vec4 t1 = texture2D(backbuffer, vec2(1.5,0.5)/resolution.xy);
	float t = floor(t0.w*255.+0.5);
	float s = floor(t1.z*255.+0.5);
	float s2 = fract(s/16.);
	float s3 = floor(s/16.)/16.;
	// gl_FragCoord seems to be pixel + .5
	// (0,0) contains the interpolated mouse coord in (x,y), game state in z and internal time in w.
	// (1,0) contains the edible object.
	if (gl_FragCoord.y < 1.) {
		float tt = fract(t/256.*23.)*256.;
		vec4 t2 = texture2D(backbuffer, vec2(4.+tt,0) / (resolution-1.));
		float eatself = step(length(t0.xy-t2.xy),0.09)*step(.1,fract((t-tt-1.)/256.))*step(fract((t-tt-1.)/256.), s2-.05);
		if (gl_FragCoord.x < 1.) {
			float propdir = fract(cPrev.z+clamp(fract(atan(mouse.y-cPrev.y,mouse.x-cPrev.x)/6.283-cPrev.z+.5),.49,.51)-.5);
			float speed = .004;
			gl_FragColor = mix(vec4(cPrev.x+cos(propdir*6.283)*speed, cPrev.y+sin(propdir*6.283)*speed,propdir,fract((t+1.5)/256.)), mix(t0,vec4(0), step(.9,t1.w)), eatself);
		} else if (gl_FragCoord.x < 2.) {
			float eaten = step(length(t0.xy-t1.xy),0.09);
			vec2 diff = t1.xy-t0.xy;
			vec2 fr = fract(t1.xy-.5)-.5;
			vec2 move = normalize((cos(time*vec2(1.215,1.7125)+t0.xy)+cos(time*vec2(1.8612,1.37341)))*.01 + normalize(diff)*.01 / (.03 + dot(diff,diff)) + fr*.02/(.01+fr*fr))*.008;
			gl_FragColor = mix(mix(vec4(t1.xy+move*step(fract(time*41.752654)+.0001,s3), t1.zw), fract(vec4(time*17.34312532,time*31.634541,fract((s+1.5)/256.),0)), eaten), mix(vec4(t1.xyz,t1.w+.01),vec4(.2,.2,0,0),step(.9,t1.w)), eatself);
		} else if (floor(gl_FragCoord.x) == t+4.) {
			gl_FragColor = t0;
		} else {
			gl_FragColor = cPrev;
		}
	} else {
		vec2 position = ( gl_FragCoord.xy / resolution.xy );
		float y = position.y*2.-1.0;
		y = abs(y)*2.5-y-.1;
		float x = mix(mix(mix(mix(489303.1,283988.1,step(y,.4)), mix(481126.1,87380.1,step(y,.2)), step(y,.3)), mix(480599.1,0.,step(y,.0)), step(y,.1)),0.,step(.5,y));
		
		float color0 = floor(fract(x/pow(2.,floor(20.5-position.x*20.)))*2.);
		color0 = mix(0.1,0.15,step(position.x+position.y-1., sin(position.x*200.)+sin(position.y*200.) + color0*2. - .8));
		vec4 color = vec4(color0, color0*5.5, color0*5.5, 0.01);

		for (int i = 0; i < 16; i++) {
			float num = fract((t-1.)/256. - float(i)/16.)*256.;
			vec4 oldpix = texture2D(backbuffer, vec2(4.+num,0) / (resolution-1.));
			vec2 p = (gl_FragCoord.xy/resolution.xy - oldpix.xy)*vec2(20.,14.);
			float det = max(0.,1.-p.x*p.x-p.y*p.y);
			float q = (sqrt(det)+p.y)*sign(det);
			float e = step(color.w,q)*step(float(i),s2*16.);
			color = mix(color,vec4(q,2.*p.x*q,2.*p.y*q,q),e);
		}
		vec2 p = (gl_FragCoord.xy/resolution.xy - t1.xy)*vec2(20.,14.);
		float det = max(0.,1.-p.x*p.x-p.y*p.y);
		float q = (sqrt(det)+p.y)*sign(det);
		float w = (sqrt(det)+p.y)*sign(det);
		float edible = step(color.w,q)*q*w;
		 color = mix(color,vec4(.0),step(color.w,q));
		
		gl_FragColor = vec4(edible + color.x+color.y*.3,color.x+color.z*.3,color.x*4.,1.);
		
	}

}

void main() {
    _userMain();
    vec3 c = gl_FragColor.rgb;
    float a = gl_FragColor.a;
    float luma = dot(c, vec3(0.299, 0.587, 0.114));
    c = mix(vec3(luma), c, saturation);
    c = (c - 0.5) * contrast + 0.5;
    c *= vec3(colorR, colorG, colorB);
    c += brightness;
    if (hueShift > 0.001) {
        float cosH = cos(hueShift * 6.28318);
        float sinH = sin(hueShift * 6.28318);
        c = vec3(
            c.r * (0.299 + 0.701*cosH + 0.168*sinH) + c.g * (0.587 - 0.587*cosH + 0.330*sinH) + c.b * (0.114 - 0.114*cosH - 0.497*sinH),
            c.r * (0.299 - 0.299*cosH - 0.328*sinH) + c.g * (0.587 + 0.413*cosH + 0.035*sinH) + c.b * (0.114 - 0.114*cosH + 0.292*sinH),
            c.r * (0.299 - 0.300*cosH + 1.250*sinH) + c.g * (0.587 - 0.588*cosH - 1.050*sinH) + c.b * (0.114 + 0.886*cosH - 0.203*sinH)
        );
    }
    if (invert) c = 1.0 - c;
    gl_FragColor = vec4(clamp(c, 0.0, 1.0), a);
}