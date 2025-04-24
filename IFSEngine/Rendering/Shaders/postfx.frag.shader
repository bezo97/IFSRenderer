#version 430

layout(location = 0) out vec4 color;

uniform sampler2D tex;//previous postfx result
uniform int width = 1920;
uniform int height = 1080;

void main(void)
{
	vec2 uv = vec2(gl_FragCoord.x / float(width), gl_FragCoord.y / float(height));

    @PostFxSnippet

}