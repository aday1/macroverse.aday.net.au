/*{
    "DESCRIPTION": "Ballpit-XYZW",
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

uniform vec4 inputColour;

// mouse.x
// mouse.y
// inputColour.x
// inputColour.y
// inputColour.z
// inputColour.w

#define MOTIONBLUR
#define DEPTHOFFIELD

#define CUBEMAPSIZE 256

#define SAMPLES 12
#define PATHDEPTH 4
#define TARGETFPS 60.

#define FOCUSDISTANCE 10 * mouse.x
#define FOCUSBLUR inputColour.z

#define RAYCASTSTEPS 20
#define RAYCASTSTEPSRECURSIVE 2

#define EPSILON 5
#define MAXDISTANCE 180.
#define GRIDSIZE 8.
#define GRIDSIZESMALL 5.9
#define MAXHEIGHT 10.
#define SPEED 0.5

float itime = time;

//
// math functions
//

float hash( const float n ) {
	return fract(sin(n)*43758.54554213);
}
vec2 hash2( const float n ) {
	return fract(sin(vec2(n,n+1.))*vec2(43758.5453123));
}
vec2 hash2( const vec2 n ) {
	return fract(sin(vec2( n.x*n.y, n.x+n.y))*vec2(25.1459123,312.3490423));
}
vec3 hash3( const vec2 n ) {
	return fract(sin(vec3(n.x, n.y, n+2.0))*vec3(36.5453123,43.1459123,11234.3490423));
}
//
// intersection functions
//

float intersectPlane( const vec3 ro, const vec3 rd, const float height) {	
	if (rd.y==0.0) return 500.;	
	float d = -(ro.y - height)/rd.y;
	if( d > 0. ) {
		return d;
	}
	return 500.;
}

float intersectUnitSphere ( const vec3 ro, const vec3 rd, const vec3 sph ) {
	vec3  ds = ro - sph;
	float bs = dot( rd, ds );
	float cs = dot( ds, ds ) - mouse.y;
	float ts = bs*bs - cs;

	if( ts > 0.0 ) {
		ts = -bs - sqrt( ts );
		if( ts > 0. ) {
			return ts;
		}
	}
	return 500.;
}

//
// Scene
//

void getSphereOffset( const vec2 grid, out vec2 center ) {
	center = (hash2( grid+vec2(43.12,1.23) ) - vec2(0.5) )*(GRIDSIZESMALL);
}
void getMovingSpherePosition( const vec2 grid, const vec2 sphereOffset, out vec3 center ) {
	// falling?
	float s = 0.1+hash( grid.x*1.23114+5.342+74.324231*grid.y );
	float t = 14.*s + time/s;
	
	float y =  s * MAXHEIGHT * abs( cos( t ) );
	vec2 offset = grid + sphereOffset;
	
	center = vec3( offset.x, y, offset.y ) + inputColour.w*vec3( GRIDSIZE, 2., GRIDSIZE );
}
void getSpherePosition( const vec2 grid, const vec2 sphereOffset, out vec3 center ) {
	vec2 offset = grid + sphereOffset;
	center = vec3( offset.x, 0., offset.y ) + 0.5*vec3( GRIDSIZE, 2., GRIDSIZE );
}
vec3 getSphereColor( const vec2 grid ) {
	return 0.8*normalize( hash3( grid+vec2(43.12*grid.y,12.23*grid.x) ) );
}

vec3 sundir = normalize( vec3(-1.0,0.8,0.2) );

vec3 getBackgroundColor( const vec3 ro, const vec3 rd ) {	
	return vec3( 1.0, inputColour.y, inputColour.z ) * (1.8 * (rd.y+inputColour.w) );
}

// code duplication because the for-loop requires a const
vec3 traceRec( const vec3 ro, const vec3 rd, out vec3 intersection, out vec3 normal, out float dist, out int material) {
	dist = MAXDISTANCE;
	float distcheck;
	
	vec3 sphereCenter, col;

	material = 0;	
	col = getBackgroundColor(ro, rd);
	
	if( (distcheck = intersectPlane( ro,  rd, 0.)) < MAXDISTANCE ) {
		dist = distcheck;
		material = 1;
		normal = vec3( 0., 1., 0. );
		col = vec3( 0.5 );
	}
	
	// trace grid
	vec3 pos = floor(ro/GRIDSIZE)*GRIDSIZE;
	vec3 ri = 1.0/rd;
	vec3 rs = sign(rd) * GRIDSIZE;
	vec3 dis = (pos-ro + 0.5  * GRIDSIZE + rs*0.5) * ri;
	vec3 mm = vec3(0.0);
	vec2 offset;
	
	for( int i=0; i<RAYCASTSTEPSRECURSIVE; i++ ) {
		if( material == 2 ) break; {		
			getSphereOffset( pos.xz, offset );			
			
			getMovingSpherePosition( pos.xz, -offset, sphereCenter );			
			if( (distcheck = intersectUnitSphere( ro, rd, sphereCenter )) < dist ) {
				dist = distcheck;
				normal = normalize((ro+rd*dist)-sphereCenter);
				col = getSphereColor(pos.xz);
				material = 2;
			}
			
			getSpherePosition( pos.xz, offset, sphereCenter );
			if( (distcheck = intersectUnitSphere( ro, rd, sphereCenter )) < dist ) {
				dist = distcheck;
				normal = normalize((ro+rd*dist)-sphereCenter);
				col = getSphereColor(pos.xz+vec2(1.,2.));
				material = 2;
			}
			
			mm = step(dis.xyz, dis.zyx);
			dis += mm * rs * ri;
			pos += mm * rs;	
		}
	}
	
	intersection = ro+rd*dist;
	
	return col;
}

vec3 trace(const vec3 ro, const vec3 rd, out vec3 intersection, out vec3 normal, out float dist, out int material) {
	dist = MAXDISTANCE;
	float distcheck;
	
	vec3 sphereCenter, col, normalcheck;
	
	material = 0;
	col = getBackgroundColor(ro, rd);
	
	if( (distcheck = intersectPlane( ro,  rd, 0.)) < MAXDISTANCE ) {
		dist = distcheck;
		material = 1;
		normal = vec3( 0., 1., 0. );
		col = vec3( inputColour.x );
	} 
	
	// trace grid
	vec3 pos = floor(ro/GRIDSIZE)*GRIDSIZE;
	vec3 ri = 1.0/rd;
	vec3 rs = sign(rd) * GRIDSIZE;
	vec3 dis = (pos-ro + 0.5  * GRIDSIZE + rs*0.5) * ri;
	vec3 mm = vec3(0.0);
	vec2 offset;
		
	for( int i=0; i<RAYCASTSTEPS; i++ )	{
		if( material == 2 ||  distance( ro.xz, pos.xz ) > dist+GRIDSIZE ) break; {
			getSphereOffset( pos.xz, offset );
			
			getMovingSpherePosition( pos.xz, -offset, sphereCenter );			
			if( (distcheck = intersectUnitSphere( ro, rd, sphereCenter )) < dist ) {
				dist = distcheck;
				normal = normalize((ro+rd*dist)-sphereCenter);
				col = getSphereColor(pos.xz);
				material = 2;
			}
			
			getSpherePosition( pos.xz, offset, sphereCenter );
			if( (distcheck = intersectUnitSphere( ro, rd, sphereCenter )) < dist ) {
				dist = distcheck;
				normal = normalize((ro+rd*dist)-sphereCenter);
				col = getSphereColor(pos.xz+vec2(1.,2.));
				material = 2;
			}		
			mm = step(dis.xyz, dis.zyx);
			dis += mm * rs * ri;
			pos += mm * rs;		
		}
	}
	
	intersection = ro+rd*dist;
	
	return col;
}

vec2 rv2;

vec3 cosWeightedRandomHemisphereDirection2( const vec3 n ) {
	vec3  uu = normalize( cross( n, vec3(0.0,1.0,1.0) ) );
	vec3  vv = cross( uu, n );
	
	float ra = sqrt(rv2.y);
	float rx = ra*cos(6.2831*rv2.x); 
	float ry = ra*sin(6.2831*rv2.x);
	float rz = sqrt( 1.0-rv2.y );
	vec3  rr = vec3( rx*uu + ry*vv + rz*n );

    return normalize( rr );
}

void _userMain() {
	vec2 q = gl_FragCoord.xy/resolution.xy;
	vec2 p = -1.0+2.0*q;
	p.x *= resolution.x/resolution.y;
	
	vec3 col = vec3( 0. );
	
	// raytrace
	int material;
	vec3 normal, intersection;
	float dist;
	float seed = time+(p.x+resolution.x*p.y)*1.51269341231;
	
	for( int j=0; j<SAMPLES; j++ ) {
		float fj = float(j);
		
#ifdef MOTIONBLUR
		itime = time + fj/(float(SAMPLES)*TARGETFPS);
#endif
		
		rv2 = hash2( 24.4316544311*fj+time+seed );
		
		vec2 pt = p+rv2/(0.5*resolution.xy);
				
		// camera	
		vec3 ro = vec3( cos( 0.232*time) * 10., 6.+3.*cos(0.3*time), GRIDSIZE*(time/SPEED) );
		vec3 ta = ro + vec3( -sin( 0.232*time) * 10., -2.0+cos(0.23*time), 10.0 );
		
		float roll = -0.15*sin(0.5*time);
		
		// camera tx
		vec3 cw = normalize( ta-ro );
		vec3 cp = vec3( sin(roll), cos(roll),mouse.x );
		vec3 cu = normalize( cross(cw,cp) );
		vec3 cv = normalize( cross(cu,cw) );
	
#ifdef DEPTHOFFIELD
    // create ray with depth of field
		const float fov = 3.0;
		
        vec3 er = normalize( vec3( pt.xy, fov ) );
        vec3 rd = er.x*cu + er.y*cv + er.z*cw;

        vec3 go = FOCUSBLUR*vec3( (rv2-vec2(0.5))*2., 0.0 );
        vec3 gd = normalize( er*FOCUSDISTANCE - go );
		
        ro += go.x*cu + go.y*cv;
        rd += gd.x*cu + gd.y*cv;
		rd = normalize(rd);
#else
		vec3 rd = normalize( pt.x*cu + pt.y*cv + 1.5*cw );		
#endif			
		vec3 colsample = vec3( 1. );
		
		// first hit
		rv2 = hash2( (rv2.x*2.4543263+rv2.y)*(time+1.) );
		colsample *= trace(ro, rd, intersection, normal, dist, material);

		// bounces
		for( int i=0; i<(PATHDEPTH-1); i++ ) {
			if( material != 0 ) {
				rd = cosWeightedRandomHemisphereDirection2( normal );
				ro = intersection + EPSILON*rd;
						
				rv2 = hash2( (rv2.x*2.4543263+rv2.y)*(time+1.)+(float(i+1)*.23) );
						
				colsample *= traceRec(ro, rd, intersection, normal, dist, material);
			}
		}	
		if( material == 0 ) {			
			col += colsample;	
		}
	}
	col  /= float(SAMPLES);
	
	col = pow( col, vec3(mouse.x) );	
	col = clamp(col, 0.0, 1.0);
	// contrast	
 //   col = clamp( col*0.7 + 0.3*col*col*(3.0-2.0*col), 0., 1.); 
	   
	// vigneting
	col *= 0.25+0.75*pow( 16.0*q.x*q.y*(1.0-q.x)*(1.0-q.y), 0.15 );
	
	gl_FragColor = vec4( col,1.0);
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