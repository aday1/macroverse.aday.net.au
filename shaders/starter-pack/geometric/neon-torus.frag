precision mediump float;

uniform float val_n0_001; // @expose -0.999 1.001
uniform float val_n1_5; // @expose 0 2.5
uniform float val_n0_5; // @expose -0.5 1.5
uniform float rotSpeed; // @expose 0.1 2
uniform float torusR; // @expose 0.3 1.5
uniform float tubeR; // @expose 0.1 0.6
uniform float twist; // @expose 0 5
uniform float colorHue; // @expose 0 1
uniform float glowIntensity; // @expose 0.1 3.0
uniform float saturation; // @expose 0.5 2.5
uniform vec2 resolution;
uniform float time;

float sdTorus(vec3 p, vec2 t) { vec2 q = vec2(length(p.xz)-t.x, p.y); return length(q)-t.y; }
mat3 rotY(float a){float c=cos(a),s=sin(a);return mat3(c,0,s,0,1,0,-s,0,c);}
mat3 rotX(float a){float c=cos(a),s=sin(a);return mat3(1,0,0,0,c,-s,0,s,c);}

float scene(vec3 p) {
    p = rotY(time*rotSpeed)*rotX(time*rotSpeed*0.5)*p;
    float a = atan(p.z, p.x) * twist;
    p.xz = mat2(cos(a),-sin(a),sin(a),cos(a)) * p.xz;
    return sdTorus(p, vec2(torusR, tubeR));
}

vec3 calcNormal(vec3 p){vec2 e=vec2(0.001,0);return normalize(vec3(scene(p+e.xyy)-scene(p-e.xyy),scene(p+e.yxy)-scene(p-e.yxy),scene(p+e.yyx)-scene(p-e.yyx)));}
vec3 hue2rgb(float h){return clamp(abs(mod(h*6.0+vec3(0,4,2),6.0)-3.0)-1.0,0.0,1.0);}

void main() {
    vec2 uv = (gl_FragCoord.xy-val_n0_5*resolution)/min(resolution.x,resolution.y);
    vec3 ro=vec3(0,0,4), rd=normalize(vec3(uv,-val_n1_5));
    float t=0.0;
    for(int i=0;i<96;i++){float d=scene(ro+rd*t);if(d<val_n0_001||t>20.0)break;t+=d;}
    vec3 col=vec3(0.02);
    float minD=1e9;
    for(int i=0;i<96;i++){float sd=scene(ro+rd*(float(i)*0.05));minD=min(minD,abs(sd));}
    if(t<20.0){
        vec3 p=ro+rd*t;
        vec3 n=calcNormal(p);
        vec3 l=normalize(vec3(1,1,2));
        float diff=max(dot(n,l),0.0);
        float spec=pow(max(dot(reflect(-l,n),-rd),0.0),48.0);
        float rim=pow(1.0-max(dot(n,-rd),0.0),3.0);
        float hShift=atan(n.z,n.x)*0.08;
        vec3 baseCol=hue2rgb(colorHue+hShift);
        vec3 rimCol=hue2rgb(colorHue+0.15);
        col=baseCol*(0.15+diff*1.0)+vec3(1)*spec*0.7+rimCol*rim*0.9;
    }
    vec3 glowCol=hue2rgb(colorHue+0.05);
    float glow=exp(-minD*6.0)*glowIntensity;
    col+=glowCol*glow*0.5;
    float luma=dot(col,vec3(0.299,0.587,0.114));
    col=mix(vec3(luma),col,saturation);
    col=col/(1.0+col*0.25);
    col=pow(col,vec3(0.92));
    gl_FragColor=vec4(col,1.0);
}
