#extension GL_OES_standard_derivatives : enable

precision highp float;

uniform float time;
uniform vec2 mouse;
uniform vec2 resolution;

// Custom uniform parameters that will be exposed in Resolume
uniform float timeScale;         // Controls animation speed (0.0 to 1.0)
uniform float mouseInfluence;    // Controls mouse interaction strength (0.0 to 1.0)
uniform float waveIntensity;     // Controls wave intensity (0.0 to 1.0)
uniform float colorIntensity;    // Controls color intensity (0.0 to 1.0)
uniform float redChannel;        // Controls red channel intensity (0.0 to 1.0)
uniform float greenChannel;      // Controls green channel intensity (0.0 to 1.0)
uniform float blueChannel;       // Controls blue channel intensity (0.0 to 1.0)

void main( void ) {

	// Apply custom parameters with safe defaults
	float scaledTime = time * (timeScale > 0.0 ? timeScale : 1.0);
	float mouseStrength = mouseInfluence > 0.0 ? mouseInfluence : 1.0;
	float waveMult = waveIntensity > 0.0 ? waveIntensity : 1.0;
	float colorMult = colorIntensity > 0.0 ? colorIntensity : 1.0;
	float redMult = redChannel > 0.0 ? redChannel : 1.0;
	float greenMult = greenChannel > 0.0 ? greenChannel : 1.0;
	float blueMult = blueChannel > 0.0 ? blueChannel : 1.0;

	vec2 position = ( gl_FragCoord.xy / resolution.xy ) + mouse / (4.0 / mouseStrength);

	float color = 0.0;
	color += sin( position.x * cos( scaledTime / 15.0 ) * 80.0 ) + cos( position.y * cos( scaledTime / 15.0 ) * 10.0 );
	color += sin( position.y * sin( scaledTime / 10.0 ) * 40.0 ) + cos( position.x * sin( scaledTime / 25.0 ) * 40.0 );
	color += sin( position.x * sin( scaledTime / 5.0 ) * 10.0 ) + sin( position.y * sin( scaledTime / 35.0 ) * 80.0 );
	color *= sin( scaledTime / 10.0 ) * 0.5 * waveMult;

	float red = color * redMult * colorMult;
	float green = color * 6.4 * greenMult * colorMult;
	float blue = sin( color + scaledTime / 3.0 ) * 0.75 * blueMult * colorMult;

	gl_FragColor = vec4( vec3( red, green, blue ), 1.0 );

}