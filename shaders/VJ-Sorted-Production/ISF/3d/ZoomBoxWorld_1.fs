/*{
    "DESCRIPTION": "ZoomBoxWorld",
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
        },
        {
            "NAME": "inputColour",
            "TYPE": "vec4",
            "LABEL": "Input Colour"
        }
    ],
    "TAGS": [
        "3d"
    ]
}*/
#define E 2.71828182846

uniform vec4 color;
uniform float colorB;
uniform float colorG;
uniform float colorR;




#define time ((useFrameIndex ? (float(FRAMEINDEX) / fps) : TIME) * speed)
#define mouse vec2(mouseX, mouseY)
#define resolution (RENDERSIZE / zoom)

    #ifdef GL_ES
    precision mediump float;
    #endif

    //g

    uniform vec4 inputColour;
    	
    float sdBox( vec3 p, vec3 b )
    {
      vec3 d = abs(p) - b;
      return min(max(d.x,max(d.y,d.z)),0.0) + length(max(d,0.0));
    }

    float sdBox( vec2 p, vec2 b )
    {
      vec2 d = abs(p) - b;
      return min(max(d.x,d.y),0.0) + length(max(d,0.0)) + inputColour.z;
    }

    vec3 hash( vec3 p )
    {
    	p = vec3( dot(p,vec3(127.1,311.7,311.7)), dot(p,vec3(269.5,183.3,183.3)), dot(p,vec3(269.5,183.3,183.3)) );
    	return fract(sin(p)*43758.5453);
    }

    float voronoi( vec3 x )
    {
    	vec3 n = floor(x);
    	vec3 f = fract(x);
    	vec3 mg, mr;

    	float md = 2.0;
    	{
    		for( int j=-1; j<=1; j++ ) {
    		for( int i=-1; i<=1; i++ ) {
    		for( int k=-1; k<=1; k++ ) {
    			vec3 g = vec3(float(i),float(j),float(k));
    			vec3 o = hash( n + g );
    			vec3 r = g + o - f;
    			float d = dot(r,r + x);
    			if( d<md ) {
    				md = d;
    				mr = r;
    				mg = g;
    			}
    		}}}
    	}

    	md = 8.0;
    	{
    		for( int j=-1; j<=1; j++ ) {
    		for( int i=-1; i<=1; i++ ) {
    		for( int k=-1; k<=1; k++ ) {
    			vec3 g = mg + vec3(float(i),float(j),float(k));
    			vec3 o = hash( n + g );
    			vec3 r = g + o - f;
    			if( dot(mr-r,mr-r)>0.000001 ) {
    				float d = dot( 1.5*(mr+r), normalize(r-mr) );
    				md = min( md, d );
    			}
    		}}}
    	}

    	return md;
    }

    vec3 nrand3( vec2 co )
    {
    	vec3 a = fract( cos( co.x*8.3e-3 + co.y )*vec3(1.3e5, 4.7e5, 2.9e5) );
    	vec3 b = fract( sin( co.x*0.3e-3 + co.y )*vec3(8.1e5, 1.0e5, 0.1e5) );
    	vec3 c = mix(a, b, inputColour.y);
    	return c;
    }

    float map(vec3 p)
    {
        float h = 1.4;
        float rh = 0.25;
        float grid = 0.4;
        float grid_half = grid*0.5;
        float cube = 0.175;
        vec3 orig = p;

        vec3 g1 = vec3(ceil((orig.x)/grid), ceil((orig.y)/grid), ceil((orig.z)/grid));
        vec3 rxz =  nrand3(g1.xz);
        vec3 ryz =  nrand3(g1.yz);

        p = -abs(p);
        vec3 di = ceil(p/4.8);

        vec2 gap = vec2(rxz.x*rh, ryz.y*rh);
        float d1 = p.y + h + gap.x;
        float d2 = p.x + h + gap.y;

        vec2 p1 = mod(p.xz, vec2(grid)) - vec2(grid_half);
        float c1 = sdBox(p1,vec2(cube));

    	return max(c1,d1);
    }

    vec3 genNormal(vec3 p)
    {
        const float d = 0.01;
        return normalize( vec3(
            map(p+vec3(  d,0.0,0.0))-map(p+vec3( -d,0.0,0.0)),
            map(p+vec3(0.0,  d,0.0))-map(p+vec3(0.0, -d,0.0)),
            map(p+vec3(0.0,0.0,  d))-map(p+vec3(0.0,0.0, -d)) ));
    }

    void _userMain()
    {
        vec2 pos = (gl_FragCoord.xy*2.0 - resolution.xy) / resolution.y;
        vec3 camPos = vec3(-0.0,0.0,0.0);
        vec3 camDir = normalize(vec3(0.0, 0.0, 0.5));
        camPos -=  vec3(0.0,0.0,time*3); // CHANGE FOR MOTION :D
        vec3 camUp  = normalize(vec3(0.0, 0.3, 0.0));
        vec3 camSide = cross(camDir, camUp);
        float focus = 1.5;

        vec3 rayDir = normalize(camSide*pos.x + camUp*pos.y + camDir*focus);	    
        vec3 ray = camPos;
        int march = 0;
        float d = 0.0;

        float total_d = 0.0;
        const int MAX_MARCH = 64;
        const float MAX_DIST = 100.0;
        for(int mi=0; mi<MAX_MARCH; ++mi) {
            d = map(ray) * 0.5;
            march=mi;
            total_d += d;
            ray += rayDir * d;
            if(d<0.0001) {break; }
            if(total_d>MAX_DIST) {
                total_d = MAX_DIST;
                march = MAX_MARCH-1;
                break;
            }
        }
    	
        float glow = 0.0;
        {
            const float s = 0.0075;
            vec3 p = ray;
            vec3 n1 = genNormal(ray);
            vec3 n2 = genNormal(ray+vec3(s, 0.0, 0.0));
            vec3 n3 = genNormal(ray+vec3(0.0, s, 0.0));
            glow = (1.0-abs(dot(camDir, n1)))*0.1;
            if(dot(n1, n2)<0.8 || dot(n1, n3)<2.8) {
                glow += 0.6;
            }
        }
        {
    	vec3 p = ray;
            float grid1 = max(0.0, max((mod((p.x+p.y+p.z*2.0)-time*3.0, 20.0)-13.0)*0.5, 0.0) );
    	    glow += clamp((1.0-voronoi(p)-0.97)*1000.0, 0.0, 1.0) * grid1;
            //vec3 gp1 = abs(mod(p, vec3(0.48)));
            //if(gp1.x<0.46 && gp1.z<0.46) {
            //    grid1 = 0.0;
            //}
            //glow += grid1;
        }

        float fog = min(1.0, (9.0 / float(MAX_MARCH)) * float(march))*inputColour.w;
        vec3  fog2 = 0.00 * vec3(1, 1, 1.5) * total_d;
        glow *= clamp(4.0-(4.0 / float(MAX_MARCH-1)) * float(march), 0.0, 1.0);
        float scanline = mod(gl_FragCoord.y, 4.0) < 2.0 ? 0.7 : 1.0;
        gl_FragColor = vec4(vec3(mouse.y+glow*mouse.x, inputColour.x+glow*mouse.y, mouse.x+glow)*fog + fog2 + d, mouse.y) * scanline;
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