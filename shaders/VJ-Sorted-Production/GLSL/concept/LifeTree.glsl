
  Original shader from httpswww.shadertoy.comviewwslGz7
 

#ifdef GL_ES
precision mediump float;
#endif

 glslsandbox uniforms
uniform float time;
uniform vec2 resolution;

 shadertoy globals
float iTime = 0.0;
vec3  iResolution = vec3(0.0);

 --------[ Original ShaderToy begins here ]---------- 
vec2 po (vec2 v) {
	return vec2(length(v),atan(v.y,v.x));
}
vec2 ca (vec2 u) {
	return u.xvec2(cos(u.y),sin(u.y));
}
float ln (vec2 p, vec2 a, vec2 b) { 
    float r = dot(p-a,b-a)dot(b-a,b-a);
    r = clamp(r,0.,1.);
    p.x+=(0.7+0.5sin(0.1iTime))0.2smoothstep(1.,0.,abs(r2.-1.))sin(3.14159(r-4.iTime));
    return (1.+0.5r)length(p-a-(b-a)r);
}
void mainImage( out vec4 Q, in vec2 U )
{   vec2 R = iResolution.xy;
 	float r = 1e9;
 	U = 4.(U-0.5R)R.y;
 	U.y += 1.5;
 	Q = vec4(0);
 	for (int i = 1; i  20; i++) {
        U = ca(po(U)+0.3(sin(2.iTime)+0.5sin(4.53iTime)+0.1cos(12.2iTime))vec2(0,1));
        r = min(r,ln(U,vec2(0),vec2(0,1.)));
        U.y-=1.;
        
        U.x=abs(U.x);
        U=1.4+0.1sin(iTime)+0.05sin(0.2455iTime)(float(i));
        U = po(U);
        U.y += 1.+0.5sin(0.553iTime)sin(sin(iTime)float(i))+0.1sin(0.4iTime)+0.05sin(0.554iTime);
        U = ca(U);
        
        
        Q+=sin(1.5exp(-1e2rr)1.4vec4(1,-1.8,1.9,4)+iTime);
        
 		
 	}
 	Q=18.;
}
 --------[ Original ShaderToy ends here ]---------- 

void main(void)
{
    iTime = time;
    iResolution = vec3(resolution, 0.0);

    mainImage(gl_FragColor, gl_FragCoord.xy);
    gl_FragColor.a = 1.0;
}