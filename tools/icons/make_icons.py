"""Generate 256x256 skill and item icons through a local ComfyUI (SDXL base). Stdlib only.

Usage: python tools/icons/make_icons.py [id ...]   (no ids = all; existing files are skipped unless named)
Needs ComfyUI running on http://127.0.0.1:8188 with models/checkpoints/sd_xl_base_1.0.safetensors.
Writes game/assets/icons/skills/<id>.png and game/assets/icons/items/<id>.png. Fixed seed per id: rerunning an
id gives the same image; change SEED_SALT or the prompt to get a new one. Frees ComfyUI's VRAM at the end.
"""
import json
import os
import sys
import time
import urllib.parse
import urllib.request
import zlib

URL = "http://127.0.0.1:8188"
CHECKPOINT = "sd_xl_base_1.0.safetensors"
SEED_SALT = 7
# Per-id extra salt for redraws whose first seed came out wrong (pattern, wrong background).
REDRAW = {"plague_flask": 11, "hex_needle": 5, "copper_coil": 3, "smoke_bomb": 2, "knockoff_sneakers": 4, "gold_tooth": 6,
          "vampire_tooth": 8}
ROOT = os.path.join(os.path.dirname(__file__), "..", "..", "game", "assets", "icons")

STYLE = ("game item icon, one single object centered, stylized hand-painted digital art, bold clean shapes, "
         "vibrant saturated colors, soft rim light, isolated on a solid pure black background, crisp, high detail")
SKILL_STYLE = STYLE + ", neon purple hot pink and cyan glow, synthwave night"
NEGATIVE = ("text, letters, words, watermark, signature, logo, brand, swoosh, trademark, person, people, face, mouth, "
            "hands, body, blurry, cropped, "
            "frame, border, multiple objects, collage, photo, photorealistic, noisy background, white background, "
            "light background, colored background, gradient background")

SKILLS = {
    "cowbell": "a single metal cowbell, trapezoid shaped percussion bell with a top handle, one drumstick",
    "low_end": "glowing concentric bass sound wave rings radiating from a speaker cone",
    "speaker_stack": "a stack of two loudspeaker cabinets with glowing cones",
    "distortion": "a guitar distortion stomp pedal with a glowing knob and fuzzy electric aura",
    "slap": "an electric bass guitar headstock with a bright pink lightning beam of sound",
    "vinyl": "a black vinyl record with a hot pink label and motion trails",
    "sub_quake": "a huge subwoofer cone smashing cracked ground with a shockwave",
    "encore": "a glowing stage microphone with spotlights and confetti",
    "bass_cannon": "a futuristic loudspeaker cannon firing a massive beam",
    "the_drop": "a neon audio waveform with a big downward spike and an arrow pointing down",
}
ITEMS = {
    "bell_shard": "a broken shard of an old bronze church bell",
    "hourglass_sand": "an ornate hourglass with glowing golden sand",
    "watchtower_lens": "an old brass spyglass lens",
    "split_arrowhead": "an elven arrowhead splitting into three arrowheads",
    "hex_needle": "one single long dark knitting needle with a glowing purple rune at its tip, a little dark yarn wrapped on it",
    "overclock_chip": "a glowing computer microchip with sparks",
    "copper_coil": "one single tightly wound copper wire coil spring with blue electric sparks",
    "plague_flask": "a single round green poison potion bottle with a cork and a small skull label",
    "haunted_lantern": "an old iron lantern with a glowing ghost wisp inside",
    "toy_drone": "a small plastic toy quadcopter drone",
    "old_mortar": "a small rusty mortar cannon",
    "tesla_backpack": "a steampunk tesla coil backpack with lightning",
    "energy_drink": "an energy drink can with a lightning bolt emblem",
    "smoke_bomb": "one single round black cartoon bomb with a lit sparking fuse and a small puff of grey smoke",
    "holy_water": "a glass bottle of glowing holy water with a small cross",
    "pocket_singularity": "a tiny black hole orb with a swirling purple accretion disk",
    "whetstone": "a sharpening whetstone with sparks",
    "knockoff_sneakers": "one pair of cheap chunky colorful sneakers with plain stripes and no logos",
    "herbal_tea": "a steaming cup of herbal tea with green leaves",
    "horseshoe_magnet": "a red horseshoe magnet pulling golden coins",
    "vampire_tooth": "one single long white pointed fang tooth with a drop of red blood at the tip",
    "knights_pauldron": "a steel knight shoulder armor pauldron",
    "gold_tooth": "one single shiny golden molar tooth with roots",
    "clockwork_heart": "a mechanical clockwork heart made of brass gears",
    "grim_ledger": "a dark leather ledger book with a skull clasp and tally marks",
}


def workflow(prompt, seed, prefix):
    return {
        "1": {"class_type": "CheckpointLoaderSimple", "inputs": {"ckpt_name": CHECKPOINT}},
        "2": {"class_type": "CLIPTextEncode", "inputs": {"text": prompt, "clip": ["1", 1]}},
        "3": {"class_type": "CLIPTextEncode", "inputs": {"text": NEGATIVE, "clip": ["1", 1]}},
        "4": {"class_type": "EmptyLatentImage", "inputs": {"width": 1024, "height": 1024, "batch_size": 1}},
        "5": {"class_type": "KSampler", "inputs": {
            "model": ["1", 0], "positive": ["2", 0], "negative": ["3", 0], "latent_image": ["4", 0],
            "seed": seed, "steps": 28, "cfg": 7.0, "sampler_name": "dpmpp_2m", "scheduler": "karras", "denoise": 1.0}},
        "6": {"class_type": "VAEDecode", "inputs": {"samples": ["5", 0], "vae": ["1", 2]}},
        "7": {"class_type": "ImageScale", "inputs": {"image": ["6", 0], "upscale_method": "lanczos",
                                                     "width": 256, "height": 256, "crop": "disabled"}},
        "8": {"class_type": "SaveImage", "inputs": {"images": ["7", 0], "filename_prefix": prefix}},
    }


def post(path, body):
    req = urllib.request.Request(URL + path, data=json.dumps(body).encode(), headers={"Content-Type": "application/json"})
    with urllib.request.urlopen(req, timeout=30) as r:
        return json.loads(r.read() or b"{}")


def get(path):
    with urllib.request.urlopen(URL + path, timeout=30) as r:
        return r.read()


def generate(group, key, desc, style):
    out = os.path.join(ROOT, group, key + ".png")
    seed = zlib.crc32(key.encode()) + SEED_SALT + REDRAW.get(key, 0) * 1000
    pid = post("/prompt", {"prompt": workflow(f"{desc}, {style}", seed, f"vlg_{key}")})["prompt_id"]
    while True:
        hist = json.loads(get(f"/history/{pid}"))
        if pid in hist and hist[pid].get("outputs"):
            break
        if pid in hist and hist[pid].get("status", {}).get("status_str") == "error":
            raise RuntimeError(f"{key}: ComfyUI error {hist[pid]['status']}")
        time.sleep(1)
    img = hist[pid]["outputs"]["8"]["images"][0]
    q = urllib.parse.urlencode({"filename": img["filename"], "subfolder": img["subfolder"], "type": img["type"]})
    os.makedirs(os.path.dirname(out), exist_ok=True)
    with open(out, "wb") as f:
        f.write(get(f"/view?{q}"))
    print("wrote", out, flush=True)


def main(ids):
    jobs = [("skills", k, v, SKILL_STYLE) for k, v in SKILLS.items()] + [("items", k, v, STYLE) for k, v in ITEMS.items()]
    for group, key, desc, style in jobs:
        out = os.path.join(ROOT, group, key + ".png")
        if ids and key not in ids:
            continue
        if not ids and os.path.exists(out):
            continue
        generate(group, key, desc, style)
    post("/free", {"unload_models": True, "free_memory": True})


if __name__ == "__main__":
    main(set(sys.argv[1:]))
