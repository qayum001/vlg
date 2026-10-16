shader_type spatial;
// Full-screen additive layer for the overdraw ramp: 1 layer = every pixel blended once,
// with one texture fetch like a particle. Dim tint so dozens of layers don't saturate.

render_mode blend_add, unshaded, cull_disabled, depth_draw_never, depth_test_disabled, shadows_disabled, fog_disabled;

uniform sampler2D tex : source_color, filter_linear, repeat_enable;
uniform vec3 tint : source_color = vec3(0.02, 0.012, 0.03);

void fragment() {
	vec4 t = texture(tex, UV * 3.0 + vec2(TIME * 0.05, 0.0));
	ALBEDO = tint * t.rgb;
	ALPHA = t.a;
}
