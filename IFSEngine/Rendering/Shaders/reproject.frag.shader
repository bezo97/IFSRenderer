#version 420

layout(location = 0) out vec4 color;

uniform sampler2D new_frame_tex;//current frame
layout(rgba32f, binding = 0) uniform image2D last_frame_image;//last frame, r/w access

uniform int width = 1920;
uniform int height = 1080;
uniform mat4 prev_frame_mat;
uniform mat4 current_frame_mat;

void main(void)
{
	vec2 uv = gl_FragCoord.xy / vec2(float(width), float(height));

    vec4 current_frame_color = texture(new_frame_tex, uv);
    vec4 last_frame_color = imageLoad(last_frame_image, ivec2(uv));

    //TODO: reprojection..

    imageStore(last_frame_image, ivec2(uv), vec4(1.0));

    //TODO: write output to 'color' vec4
    color = current_frame_color;
}