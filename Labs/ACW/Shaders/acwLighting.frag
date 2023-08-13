#version 330 

uniform vec4 uEyePosition;
uniform sampler2D uTextureSampler1; 
uniform sampler2D uTextureSampler2; 
uniform int uTextureNum;

in vec2 oTexCoords; 
in vec4 oNormal; 
in vec4 oSurfacePosition; 
 
out vec4 FragColour; 

struct LightProperties { 
 vec4 Position; 
 vec3 AmbientLight; 
 vec3 DiffuseLight; 
 vec3 SpecularLight; 
}; 
 
uniform LightProperties uLight[3];
 
struct MaterialProperties { 
 vec3 AmbientReflectivity; 
 vec3 DiffuseReflectivity; 
 vec3 SpecularReflectivity; 
 float Shininess; 
}; 
 
uniform MaterialProperties uMaterial; 

void main()  
{  
	for(int i = 0; i <3; ++i){
		vec4 lightDir = normalize(uLight[i].Position - oSurfacePosition); 
		vec4 eyeDirection = normalize(uEyePosition - oSurfacePosition); 
		vec4 reflectedVector = reflect(-lightDir, oNormal); 
		float specularFactor = pow(max(dot( reflectedVector, eyeDirection), 0.0), uMaterial.Shininess); 

		float diffuseFactor = max(dot(oNormal, lightDir), 0); 

		vec4 texCol1 = texture2D(uTextureSampler1, oTexCoords);
		vec4 texCol2 = texture2D(uTextureSampler2, oTexCoords);

		FragColour = FragColour + vec4(uLight[i].AmbientLight * uMaterial.AmbientReflectivity +  uLight[i].DiffuseLight * uMaterial.DiffuseReflectivity * diffuseFactor +  uLight[i].SpecularLight * uMaterial.SpecularReflectivity * specularFactor,  1);
		if(uTextureNum == 0){
			FragColour = FragColour * texture(uTextureSampler1, oTexCoords);
		}
		if(uTextureNum == 1){
			FragColour = FragColour * texture(uTextureSampler2, oTexCoords);
		}
	}
} 