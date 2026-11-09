// Ground zone on a 2x2 quad (radius 1 in UV space). Pattern: INSTANCE_CUSTOM.x on MultiMesh, else the uniform
// (0 = fuzz noise: Distortion field and puddles, 1 = glowing cracks: quakes, 2 = bubbles: poison / holy water).
// Tint: COLOR (instance color) x tint uniform. Procedural, no textures.
shader_type spatial;
render_mode unshaded, blend_add, cull_disabled, depth_draw_never, shadows_disabled;

uniform vec4 tint : source_color = vec4(1.0);
uniform float pattern = 0.0;
uniform float cells = 5.0;

varying vec4 col;
varying float kind;

vec2 hash2(vec2 p) {
	p = vec2(dot(p, vec2(127.1, 311.7)), dot(p, vec2(269.5, 183.3)));
	return fract(sin(p) * 43758.5453);
}

float noise(vec2 p) {
	vec2 i = floor(p), f = fract(p);
	vec2 u = f * f * (3.0 - 2.0 * f);
	float a = hash2(i).x, b = hash2(i + vec2(1, 0)).x, c = hash2(i + vec2(0, 1)).x, d = hash2(i + vec2(1, 1)).x;
	return mix(mix(a, b, u.x), mix(c, d, u.x), u.y);
}

// Distance to the nearest Voronoi edge: small along cracks.
float crack(vec2 p) {
	vec2 i = floor(p), f = fract(p);
	float d1 = 8.0, d2 = 8.0;
	for (int y = -1; y <= 1; y++)
	for (int x = -1; x <= 1; x++) {
		vec2 g = vec2(float(x), float(y));
		vec2 o = hash2(i + g);
		float d = length(g + o - f);
		if (d < d1) { d2 = d1; d1 = d; } else if (d < d2) { d2 = d; }
	}
	return d2 - d1;
}

void vertex() {
	col = COLOR * tint;
	kind = INSTANCE_CUSTOM.x > 0.5 ? INSTANCE_CUSTOM.x : pattern;
}

void fragment() {
	vec2 p = UV * 2.0 - 1.0;
	float d = length(p);
	float mask = 1.0 - smoothstep(0.85, 1.0, d);
	float rim = (1.0 - smoothstep(0.0, 0.08, abs(d - 0.93))) * 0.8;
	float a;
	if (kind > 1.5) {
		float b = noise(p * cells + vec2(0.0, TIME * 0.8));
		a = 0.25 + smoothstep(0.65, 0.9, b) * 0.9;
	} else if (kind > 0.5) {
		float c = crack(p * cells);
		a = 0.12 + (1.0 - smoothstep(0.0, 0.08, c)) * 1.2;
	} else {
		float n = noise(p * cells + TIME * 1.5) * 0.6 + noise(p * cells * 2.3 - TIME * 2.1) * 0.4;
		a = 0.2 + n * 0.6;
	}
	ALBEDO = col.rgb * (1.0 + rim);
	ALPHA = clamp(a * mask + rim * mask, 0.0, 1.0) * col.a;
}
