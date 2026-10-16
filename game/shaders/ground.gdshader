shader_type spatial;
// Procedural 2 m checker + grid lines so movement and camera orbit are readable without textures.

uniform vec3 color_a : source_color = vec3(0.20, 0.24, 0.30);
uniform vec3 color_b : source_color = vec3(0.16, 0.19, 0.25);
uniform vec3 line_color : source_color = vec3(0.35, 0.42, 0.55);

varying vec3 world_pos;

void vertex() {
	world_pos = (MODEL_MATRIX * vec4(VERTEX, 1.0)).xyz;
}

void fragment() {
	vec2 cell = floor(world_pos.xz / 2.0);
	float checker = mod(cell.x + cell.y, 2.0);
	vec2 g = abs(fract(world_pos.xz / 2.0) - 0.5);
	float line = smoothstep(0.47, 0.49, max(g.x, g.y));
	ALBEDO = mix(mix(color_a, color_b, checker), line_color, line * 0.6);
	ROUGHNESS = 0.95;
}
