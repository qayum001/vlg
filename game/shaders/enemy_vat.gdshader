shader_type spatial;
// Baked vertex animation (VAT). anim_tex: column = VERTEX_ID, rows [0, frame_count) positions,
// rows [frame_count, 2 * frame_count) normals. Built by PlaceholderMeshes.BakeWalk.
// INSTANCE_CUSTOM: x = anim phase [0, 1), y = hit flash, z = variant index (0..3).
// Vertex COLOR.a: 1 = tinted skin, 0.5 = fixed color, 0 = emissive.

uniform sampler2D anim_tex : filter_nearest, repeat_disable;
uniform int frame_count = 24;
uniform vec3 tint_0 : source_color = vec3(0.85, 0.3, 0.35);
uniform vec3 tint_1 : source_color = vec3(0.4, 0.75, 0.35);
uniform vec3 tint_2 : source_color = vec3(0.55, 0.4, 0.85);
uniform vec3 tint_3 : source_color = vec3(0.9, 0.65, 0.25);

varying vec3 v_inst;

void vertex() {
	float f = INSTANCE_CUSTOM.x * float(frame_count);
	int f0 = int(f) % frame_count;
	int f1 = (f0 + 1) % frame_count;
	float t = fract(f);
	vec3 p0 = texelFetch(anim_tex, ivec2(VERTEX_ID, f0), 0).xyz;
	vec3 p1 = texelFetch(anim_tex, ivec2(VERTEX_ID, f1), 0).xyz;
	vec3 n0 = texelFetch(anim_tex, ivec2(VERTEX_ID, f0 + frame_count), 0).xyz;
	vec3 n1 = texelFetch(anim_tex, ivec2(VERTEX_ID, f1 + frame_count), 0).xyz;
	VERTEX = mix(p0, p1, t);
	NORMAL = normalize(mix(n0, n1, t));
	v_inst = INSTANCE_CUSTOM.xyz;
}

void fragment() {
	int variant = int(v_inst.z + 0.5);
	vec3 tint = variant == 0 ? tint_0 : (variant == 1 ? tint_1 : (variant == 2 ? tint_2 : tint_3));
	ALBEDO = COLOR.a > 0.75 ? COLOR.rgb * tint : COLOR.rgb;
	ROUGHNESS = 0.75;
	EMISSION = (COLOR.a < 0.25 ? COLOR.rgb * 4.0 : vec3(0.0)) + vec3(1.0, 0.85, 0.7) * v_inst.y * 2.0;
}
