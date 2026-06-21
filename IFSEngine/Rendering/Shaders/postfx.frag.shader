#version 430

#define PI			3.14159265358f
#define TWOPI		6.28318530718f
#define DEGTORAD	0.0174532925f

#define MAX_PALETTE_COLORS 150

layout(location = 0) out vec4 output_color;

uniform sampler2D input_tex;//previous pass result
uniform int width = 1920;
uniform int height = 1080;

uniform float postfx_real_params[64];
uniform vec3 postfx_vec3_params[64];

layout(std140, binding = 5) uniform palette_ubo
{
    vec4 palette[MAX_PALETTE_COLORS];
};

uniform int palette_colors_count = 2;

//Helper: sample palette with linear interpolation, pos in [0,1]
vec3 getPaletteColor(float pos)
{
    float palettepos = clamp(pos, 0.0, 1.0) * float(palette_colors_count - 1);
    int index = int(floor(palettepos));
    vec3 c1 = palette[index].xyz;
    if (index + 1 >= palette_colors_count)
        return c1;
    vec3 c2 = palette[index + 1].xyz;
    float a = fract(palettepos);
    return mix(c1, c2, a);
}

@includes

void main(void)
{
	vec2 uv = vec2(gl_FragCoord.x / float(width), gl_FragCoord.y / float(height));
    vec4 input_color = texture(input_tex, uv);

    @PostFxSnippet

}
