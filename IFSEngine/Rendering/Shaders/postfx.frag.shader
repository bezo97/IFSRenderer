#version 430

layout(location = 0) out vec4 output_color;

uniform sampler2D input_tex;//previous pass result
uniform int width = 1920;
uniform int height = 1080;

uniform float postfx_real_params[64];
uniform vec3 postfx_vec3_params[64];

void main(void)
{
	vec2 uv = vec2(gl_FragCoord.x / float(width), gl_FragCoord.y / float(height));
    vec4 input_color = texture(input_tex, uv);

    @PostFxSnippet

}
