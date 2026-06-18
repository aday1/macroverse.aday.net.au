// https://www.shadertoy.com/view/Ntt3Rr
precision highp float;
uniform float time;
uniform vec2 mouse,resolution;

void main( void ) {
    vec4 col;
    float a=3.14/4.;
    vec2 uv1 = (gl_FragCoord.xy*10.-resolution)/resolution.y/60.*mat2(cos(a),-sin(a),sin(a),cos(a))-vec2(0.1,.401);
    for(int c=0;c<3;c++){
        vec2 uv = uv1;
        for(int i=0;i<6;i++)
        {
            uv = fract(uv.y+uv.x+uv-time/50.)-.5;
            uv *= (uv.y-uv.x)*3.;
            col[c] -= abs(abs(uv.x)-uv.y);
            col.yx += uv*uv.yx*col.yz/3.;
        }
    }
    gl_FragColor = abs(col-atan(col,vec4(1.5,1.5,.5,-1))*1.5);
}