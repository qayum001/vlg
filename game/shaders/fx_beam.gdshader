// Beam on a 1x1 quad: UV.x across (soft edges, hot core), UV.y along (scrolling sound stripes).
// Tint and alpha from COLOR (MultiMesh instance color).
shader_type spatial;
render_mode unshaded, blend_add, cull_disabled, depth_draw_never, shadows_disabled;

uniform float stripes = 9.0;
uniform float scroll = 6.0;

varying vec4 tint;

void vertex() {
	tint = COLOR;
}

void fragment() {
	float across = abs(UV.x * 2.0 - 1.0);
	float core = 1.0 - smoothstep(0.0, 0.25, across);
	float body = 1.0 - smoothstep(0.2, 1.0, across);
	float wave = 0.65 + 0.35 * sin((UV.y * stripes - TIME * scroll) * 6.2831);
	float tip = smoothstep(0.0, 0.05, UV.y) * smoothstep(1.0, 0.9, UV.y);
	ALBEDO = mix(tint.rgb, vec3(1.0), core * 0.7);
	ALPHA = clamp((body * wave + core) * tip, 0.0, 1.0) * tint.a;
}
