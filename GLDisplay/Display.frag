#version 450
#extension GL_ARB_explicit_attrib_location : enable

layout(location = 0) out vec3 color;
uniform sampler2D renderedTexture;

uniform int width=300;//TODO: ezt beallitani GLControl meret alapjan
uniform int height=300;

void main(void)
{
	int px = int(gl_FragCoord.x);
	int py = int(gl_FragCoord.y);
	vec2 uv = vec2(gl_FragCoord.x/float(width),gl_FragCoord.y/float(height));

	//color = vec3(1.0,uv.y,0.0);
	color = texture(renderedTexture, uv ,1).xyz;
}