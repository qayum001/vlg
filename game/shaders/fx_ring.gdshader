// Soft glowing ring on a 2x2 quad (radius 1 in UV space). Tint and alpha from COLOR (MultiMesh instance color).
// Used for shockwaves, speaker pulses, impact flashes, strike markers.
shader_type spatial;
render_mode unshaded, blend_add, cull_disabled, depth_draw_never, shadows_disabled;

uniform float thickness = 0.1;
uniform float halo = 0.3;
uniform float fill = 0.0;

varying vec4 tint;

void vertex() {
	tint = COLOR;
}

void fragment() {
	vec2 p = UV * 2.0 - 1.0;
	float d = length(p);
	float edge = 1.0 - thickness;
	float band = 1.0 - smoothstep(0.0, thickness, abs(d - edge));
	float glow = (1.0 - smoothstep(0.0, halo, abs(d - edge))) * 0.4;
	float inner = fill * smoothstep(1.0, 0.0, d) * 0.3;
	float a = (band + glow + inner) * (1.0 - step(1.0, d));
	ALBEDO = tint.rgb * (1.0 + band * 1.5);
	ALPHA = clamp(a, 0.0, 1.0) * tint.a;
}
