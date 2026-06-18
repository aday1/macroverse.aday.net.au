/*{
    "DESCRIPTION": "simpledistanceassistedray-marcher",
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
//simple distance assisted ray-marcher by chris liu

#ifdef GL_ES
precision mediump float;
#endif

vec4 global_color=vec4(0.0);
bool global_stop=false;
vec3 light_position;
vec4 ambient_light=vec4(0.25,0.25,0.25,0.0);
float total_distance=0.0;
vec3 direction_vector;
vec3 eye_point;
bool inside_object=false;
vec4 global_attenuation=vec4(1.0);
float min_distance=0.001;
float refraction_amount=0.25;
float glass_torus(vec3 point,vec3 offset){
	vec3 point_offset=point+offset;
	vec2 torus_dimensions=vec2(0.5,0.25);
	vec4 color=vec4(0.0);
	float dist=length(vec2(length(point_offset.xy)-torus_dimensions.x,point_offset.z))-torus_dimensions.y;
	if(dist<0.0){
		if(inside_object==false){
			vec2 temp=vec2(point_offset.xy-torus_dimensions.x*normalize(point_offset.xy));
			vec3 normal=normalize(vec3(temp.x,temp.y,point_offset.z));
			inside_object=true;
			direction_vector=normalize(direction_vector-refraction_amount*normal);
			float attenuation=abs(dot(normal,direction_vector));
			global_attenuation*=vec4(attenuation,(1.0-attenuation*0.5),attenuation,1.0);
		}
	}else{
		if(inside_object==true){
			vec2 temp=vec2(point_offset.xy-torus_dimensions.x*normalize(point_offset.xy));
			vec3 normal=normalize(vec3(temp.x,temp.y,point_offset.z));
			inside_object=false;
			direction_vector=normalize(direction_vector+refraction_amount*normal);
			float attenuation=abs(dot(direction_vector,normal));
			global_attenuation*=vec4(attenuation,(1.0-attenuation*0.5),attenuation,1.0);
		}		
	}
	global_color=color;
	return abs(dist);
}
float torus(vec3 point,vec3 offset){
	vec3 point_offset=point+offset;
	vec2 torus_dimensions=vec2(0.5,0.25);
	float dist=length(vec2(length(point_offset.xy)-torus_dimensions.x,point_offset.z))-torus_dimensions.y;
	if(dist<0.0){
		vec4 color=vec4(0.75,0.25,0.0,1.0);
		vec2 temp=vec2(point_offset.xy-torus_dimensions.x*normalize(point_offset.xy));
		vec3 normal=normalize(vec3(temp.x,temp.y,point_offset.z));
		float diffuse=dot(normal,normalize(light_position-point))/length(light_position-point);
		global_stop=true;
		global_color=vec4(clamp(color*diffuse,0.0,1.0))+color*ambient_light;
	}
	return dist;
}
float plane(vec3 point){
	float dist=dot(point,vec3(0.0,0.0,1.0))+0.5;
	if(dist<0.0){
		vec4 color;
		vec4 color_1=vec4(1.0,1.0,1.0,1.0);
		vec4 color_2=vec4(0.25,0.75,1.0,1.0);
		if(abs(fract(point.x))<0.5){
			if(abs(fract(point.y))<0.5){
				color=color_1;
			}else{
				color=color_2;
			}
		}else{
			if(abs(fract(point.y))<0.5){
				color=color_2;
			}else{
				color=color_1;
			}
		}
		vec3 normal=vec3(0.0,0.0,1.0);
		float diffuse=dot(normal,normalize(light_position-point))/length(light_position-point);
		global_stop=true;
		global_color=vec4(clamp(color*diffuse,0.0,1.0))+color*ambient_light;
	}	
	return dist;
}
float glass_sphere(vec3 point,vec3 offset,float radius){
	vec3 point_offset=point+offset;
	vec4 color=vec4(0.0);
	float dist=length(point_offset)-radius;
	vec3 normal=0.5*normalize(point_offset);
	if(dist<0.0){
		if(inside_object==false){
			inside_object=true;
			direction_vector=normalize(direction_vector-normal);
		}
	}else{
		if(inside_object==true){
			inside_object=false;
			direction_vector=normalize(direction_vector+normal);
		}		
	}
	global_color=color;
	return abs(dist);
}
float reflective_sphere(vec3 point,vec3 offset){
	float radius=0.5;
	vec3 point_offset=point+offset;
	float dist=length(point_offset)-radius;
	if(dist<0.0){
		//vec4 color=vec4(0.1,0.1,0.1,1.0);
		vec3 normal=normalize(point_offset);
		//float diffuse=dot(normal,normalize(light_position-point))/length(light_position-point);
		//global_color=vec4(clamp(color*diffuse,0.0,1.0))+color*ambient_light;
		direction_vector=normalize(normal-direction_vector);
	}
	return abs(dist);
}
float sphere(vec3 point,vec3 offset){
	float radius=0.5;
	vec3 point_offset=point+offset;
	float dist=length(point_offset)-radius;
	if(dist<0.0){
		vec4 color=vec4(0.0,1.0,0.25,1.0);
		vec3 normal=normalize(point_offset);
		float diffuse=dot(normal,normalize(light_position-point))/length(light_position-point);
		global_stop=true;
		global_color=vec4(clamp(color*diffuse,0.0,1.0))+color*ambient_light;
	}
	return dist;
}
float cube(vec3 point,vec3 offset){
	float radius=0.5;
	vec3 point_offset=point+offset;
	float dist=max(abs(point_offset.x),max(abs(point_offset.y),abs(point_offset.z)))-radius;
	if(dist<0.0){
		vec4 color=vec4(1.0,1.0,0.0,1.0);
		vec3 normal;
		float dot_1=dot(point_offset,vec3(1.0,0.0,0.0));
		float dot_2=dot(point_offset,vec3(0.0,1.0,0.0));
		float dot_3=dot(point_offset,vec3(0.0,0.0,1.0));
		if(abs(dot_1)>abs(dot_2)&&abs(dot_1)>abs(dot_3)){
			normal=normalize(vec3(dot_1,0.0,0.0));
		}else if(abs(dot_2)>abs(dot_3)){
			normal=normalize(vec3(0.0,dot_2,0.0));
		}else{
			normal=normalize(vec3(0.0,0.0,dot_3));
		}
		float diffuse=dot(normal,normalize(light_position-point))/length(light_position-point);
		global_stop=true;
		global_color=vec4(clamp(color*diffuse,0.0,1.0))+color*ambient_light;
	}
	return dist;
}
float dist(vec3 point){
	float min_dist=100.0;
	float dist;
	global_color=vec4(0.0);
	vec4 color=vec4(0.0);
	dist=reflective_sphere(point,vec3(-0.5,0.0,0.0));
	if(dist<min_dist){
		min_dist=dist;
		color=global_color;
	}
	dist=reflective_sphere(point,vec3(0.5,-0.0,0.0));
	if(dist<min_dist){
		min_dist=dist;
		color=global_color;
	}
	dist=reflective_sphere(point,vec3(0.0,0.0,-0.866));
	if(dist<min_dist){
		min_dist=dist;
		color=global_color;
	}
	dist=plane(point);
	if(dist<min_dist){
		if(global_stop)return dist;
		color=global_color;
		min_dist=dist;
	}
	dist=cube(point,vec3(-2.0,0.0,0.0));
	if(dist<min_dist){
		if(global_stop)return dist;
		color=global_color;
		min_dist=dist;
	}
	dist=glass_torus(point,vec3(2.0,0.0,0.0));
	if(dist<min_dist){
		min_dist=dist;
	}
	global_color=color;
	return min_dist;
}
void main(void){
	float ratio=resolution.x/resolution.y;
	float _t =time/10.0;
	light_position=vec3(0.0,0.0,2.0);
	vec2 mpos =mouse.xy;
	vec2 screen_position=vec2((gl_FragCoord.x-resolution.x/2.0)/resolution.x*ratio,(gl_FragCoord.y-resolution.y/2.0)/resolution.y);
	eye_point=vec3(3.0*sin(-mpos.x*3.14*2.0),3.0*cos(-mpos.x*3.14*2.0),-5.0*mpos.y+5.0);
	vec3 look_at=vec3(0.0,0.0,0.0);
	vec3 up_vector=vec3(0.0,0.0,1.0);
	direction_vector=normalize(look_at-eye_point);
	vec3 horizontal_vector=normalize(cross(up_vector,direction_vector));
	vec3 vertical_vector=normalize(cross(direction_vector,horizontal_vector));
	direction_vector=normalize(direction_vector+screen_position.x*horizontal_vector+screen_position.y*vertical_vector);
	vec4 color=vec4(0.0);
	vec3 point=eye_point;
	float nearest_object_distance;
	global_color=vec4(0.0);
	for(int i=0;i<256;i++){
		nearest_object_distance=dist(point);
		total_distance+=nearest_object_distance;
		color+=global_color;
		if(global_stop)break;
		point+=direction_vector*max(nearest_object_distance,min_distance);
	}
	gl_FragColor=color*global_attenuation;
}
