#!/usr/bin/env python3
from __future__ import annotations

import html
import subprocess
from pathlib import Path


ROOT = Path(__file__).resolve().parents[1]
PRODUCT_DIR = ROOT / "assets" / "product"
TEMP_DIR = PRODUCT_DIR / ".tmp"

WIDTH = 1600
HEIGHT = 900
FONT_STACK = "'Segoe UI', Arial, sans-serif"
MONO_STACK = "'Cascadia Mono', 'Consolas', monospace"

PALETTE = {
    "page": "#0b1015",
    "shell": "#0f141b",
    "panel": "#121923",
    "panel_alt": "#171f2a",
    "card": "#10161d",
    "card_alt": "#131b24",
    "border": "#293544",
    "border_soft": "#1d2731",
    "text": "#eef3f8",
    "muted": "#97a5b5",
    "soft": "#c8d1dc",
    "green": "#45bf78",
    "green_bg": "#12281b",
    "amber": "#d3a552",
    "amber_bg": "#302312",
    "blue": "#5aa2ff",
    "blue_bg": "#122033",
    "slate": "#667384",
    "slate_bg": "#1b232c",
    "teal": "#3db9b0",
    "teal_bg": "#102624",
    "danger": "#dd7467",
    "danger_bg": "#321917",
}


def esc(value: str) -> str:
    return html.escape(value, quote=True)


def rect(x: int, y: int, w: int, h: int, *, fill: str, stroke: str | None = None,
         stroke_width: int = 1, rx: int = 18, extra: str = "") -> str:
    stroke_attr = f' stroke="{stroke}" stroke-width="{stroke_width}"' if stroke else ""
    extra_attr = f" {extra.strip()}" if extra.strip() else ""
    return (
        f'<rect x="{x}" y="{y}" width="{w}" height="{h}" rx="{rx}"'
        f' fill="{fill}"{stroke_attr}{extra_attr}/>'
    )


def line(x1: int, y1: int, x2: int, y2: int, *, stroke: str, stroke_width: int = 1,
         extra: str = "") -> str:
    extra_attr = f" {extra.strip()}" if extra.strip() else ""
    return (
        f'<line x1="{x1}" y1="{y1}" x2="{x2}" y2="{y2}" stroke="{stroke}" '
        f'stroke-width="{stroke_width}"{extra_attr}/>'
    )


def text(x: int, y: int, value: str, *, size: int = 16, weight: int | str = 500,
         fill: str | None = None, anchor: str = "start", family: str = FONT_STACK,
         extra: str = "") -> str:
    extra_attr = f" {extra.strip()}" if extra.strip() else ""
    return (
        f'<text x="{x}" y="{y}" font-family="{family}" font-size="{size}" '
        f'font-weight="{weight}" fill="{fill or PALETTE["text"]}" '
        f'text-anchor="{anchor}"{extra_attr}>{esc(value)}</text>'
    )


def paragraph(x: int, y: int, lines: list[str], *, size: int = 15, weight: int | str = 500,
              fill: str | None = None, family: str = FONT_STACK, line_height: int = 28) -> str:
    return "".join(
        text(x, y + index * line_height, line, size=size, weight=weight, fill=fill, family=family)
        for index, line in enumerate(lines)
    )


def badge(x: int, y: int, label: str, *, fill_color: str, border_color: str,
          text_color: str | None = None, width: int | None = None) -> str:
    badge_width = width or max(118, 18 + len(label) * 8)
    return "".join(
        [
            rect(x, y, badge_width, 34, fill=fill_color, stroke=border_color, rx=17),
            text(
                x + badge_width // 2,
                y + 23,
                label,
                size=13,
                weight=600,
                fill=text_color or PALETTE["soft"],
                anchor="middle",
            ),
        ]
    )


def button(x: int, y: int, label: str, *, variant: str = "primary", width: int = 120) -> str:
    if variant == "primary":
        fill_color = PALETTE["blue"]
        border_color = PALETTE["blue"]
        text_color = "#f8fbff"
    else:
        fill_color = PALETTE["panel_alt"]
        border_color = PALETTE["border"]
        text_color = PALETTE["soft"]

    return "".join(
        [
            rect(x, y, width, 42, fill=fill_color, stroke=border_color, rx=14),
            text(x + width // 2, y + 27, label, size=15, weight=600, fill=text_color, anchor="middle"),
        ]
    )


def nav_item(x: int, y: int, label: str, *, active: bool = False) -> str:
    fill_color = PALETTE["panel_alt"] if active else "transparent"
    stroke_color = PALETTE["border"] if active else "transparent"
    text_color = PALETTE["text"] if active else PALETTE["muted"]
    parts = [rect(x, y, 220, 44, fill=fill_color, stroke=stroke_color, rx=16)]
    if active:
        parts.append(rect(x + 12, y + 12, 8, 20, fill=PALETTE["teal"], stroke=None, rx=4))
    parts.append(text(x + 34, y + 28, label, size=16, weight=600, fill=text_color))
    return "".join(parts)


def shell(title: str, subtitle: str, body: str, *, active_nav: str = "Tweaks") -> str:
    return f"""<svg xmlns="http://www.w3.org/2000/svg" width="{WIDTH}" height="{HEIGHT}" viewBox="0 0 {WIDTH} {HEIGHT}">
  <defs>
    <linearGradient id="pageGlow" x1="0%" y1="0%" x2="100%" y2="100%">
      <stop offset="0%" stop-color="#14202b"/>
      <stop offset="100%" stop-color="#0b1015"/>
    </linearGradient>
  </defs>
  {rect(0, 0, WIDTH, HEIGHT, fill='url(#pageGlow)', stroke=None, rx=0)}
  {rect(36, 36, WIDTH - 72, HEIGHT - 72, fill=PALETTE["shell"], stroke=PALETTE["border"], rx=28)}
  {rect(36, 36, 270, HEIGHT - 72, fill="#0d1319", stroke=PALETTE["border_soft"], rx=28)}
  {line(306, 56, 306, HEIGHT - 56, stroke=PALETTE["border_soft"])}
  {text(82, 106, "RegProbe", size=28, weight=700)}
  {text(82, 134, "Preview-first and reversible.", size=15, weight=500, fill=PALETTE["muted"])}
  {nav_item(64, 190, "Tweaks", active=active_nav == "Tweaks")}
  {nav_item(64, 244, "Recovery", active=active_nav == "Recovery")}
  {nav_item(64, 298, "About & Diagnostics", active=active_nav == "About & Diagnostics")}
  {text(356, 106, title, size=31, weight=700)}
  {text(356, 138, subtitle, size=16, weight=500, fill=PALETTE["muted"])}
  {body}
</svg>
"""


def render_configuration_verdict_card() -> str:
    body = []
    body.append(rect(348, 176, 1180, 96, fill=PALETTE["panel"], stroke=PALETTE["border"], rx=22))
    body.append(text(382, 214, "System readiness", size=15, weight=600, fill=PALETTE["soft"]))
    body.append(text(382, 242, "0 pending changes  ·  Rollback available  ·  Elevated host isolated", size=16, fill=PALETTE["muted"]))
    body.append(badge(1232, 202, "Verified", fill_color=PALETTE["green_bg"], border_color=PALETTE["green"], width=126))
    body.append(badge(1368, 202, "Low risk", fill_color=PALETTE["blue_bg"], border_color=PALETTE["blue"], width=128))

    body.append(rect(348, 304, 1180, 500, fill=PALETTE["panel"], stroke=PALETTE["border"], rx=26))
    body.append(text(382, 350, "Tweaks", size=20, weight=700))
    body.append(text(382, 378, "The main workspace keeps plain-English effects, trust signals, and rollback close together.", size=15, fill=PALETTE["muted"]))

    body.append(rect(382, 418, 1112, 336, fill=PALETTE["card"], stroke=PALETTE["border"], rx=22))
    body.append(rect(382, 418, 10, 336, fill=PALETTE["teal"], stroke=None, rx=5))
    body.append(text(416, 458, "GameDVR capture policy", size=23, weight=700))
    body.append(badge(1100, 433, "A", fill_color="#241941", border_color="#6f55d8", width=54))
    body.append(badge(1164, 433, "SAFE", fill_color=PALETTE["green_bg"], border_color=PALETTE["green"], width=90))
    body.append(text(416, 494, "Disables a documented capture-related surface through a validated control path.", size=17, fill=PALETTE["soft"]))

    body.append(rect(416, 528, 650, 170, fill=PALETTE["card_alt"], stroke=PALETTE["border"], rx=18))
    body.append(text(440, 559, "Verdict", size=14, weight=700, fill=PALETTE["muted"]))
    body.append(badge(904, 536, "Apply allowed", fill_color=PALETTE["green_bg"], border_color=PALETTE["green"], width=138))
    body.append(text(440, 596, "Proof and rollback signals are strong enough for the normal apply flow.", size=16, fill=PALETTE["soft"]))
    body.append(badge(440, 622, "Docs ready", fill_color=PALETTE["green_bg"], border_color=PALETTE["green"], width=108))
    body.append(badge(560, 622, "Runtime ready", fill_color=PALETTE["green_bg"], border_color=PALETTE["green"], width=128))
    body.append(badge(702, 622, "Source ready", fill_color=PALETTE["green_bg"], border_color=PALETTE["green"], width=118))
    body.append(badge(834, 622, "Rollback ready", fill_color=PALETTE["green_bg"], border_color=PALETTE["green"], width=132))
    body.append(text(440, 662, "Risk: Low-risk surface with the standard preview and verify flow.", size=14, fill=PALETTE["muted"]))
    body.append(text(440, 688, "Rollback: Verified via vm-safety-bench.", size=14, fill=PALETTE["muted"]))

    body.append(badge(416, 716, "Confirmed", fill_color=PALETTE["green_bg"], border_color=PALETTE["green"], width=112))
    body.append(text(540, 739, "Registry setting  ·  Current: Enabled  ·  Preferred: Disabled", size=14, family=MONO_STACK, fill=PALETTE["muted"]))
    body.append(button(1346, 438, "Preview", variant="secondary"))
    body.append(button(1346, 492, "Apply", variant="primary"))
    body.append(button(1346, 546, "Restore", variant="secondary"))

    return shell(
        "Tweaks with visible trust signals",
        "The verdict card turns the repo's evidence model into something a user can scan in one glance.",
        "".join(body),
    )


def render_evidence_detail_drawer() -> str:
    body = []
    body.append(rect(348, 176, 740, 618, fill=PALETTE["panel"], stroke=PALETTE["border"], rx=26))
    body.append(text(382, 220, "Tweaks", size=20, weight=700))
    body.append(text(382, 248, "Rows stay calm. Evidence gets detail only when the user asks for it.", size=15, fill=PALETTE["muted"]))
    body.append(rect(382, 286, 672, 128, fill=PALETTE["card"], stroke=PALETTE["border"], rx=20))
    body.append(text(414, 324, "NetworkThrottlingIndex", size=22, weight=700))
    body.append(text(414, 356, "Research-only until runtime behavior is proven on supported builds.", size=16, fill=PALETTE["soft"]))
    body.append(badge(860, 300, "Blocked", fill_color=PALETTE["amber_bg"], border_color=PALETTE["amber"], width=104))
    body.append(badge(972, 300, "Medium risk", fill_color=PALETTE["blue_bg"], border_color=PALETTE["blue"], width=132))

    body.append(rect(1118, 176, 410, 618, fill=PALETTE["panel_alt"], stroke=PALETTE["border"], rx=26))
    body.append(text(1150, 220, "Evidence detail", size=21, weight=700))
    body.append(paragraph(1150, 248, ["Plain-English first.", "Technical justification follows."], size=15, fill=PALETTE["muted"]))
    body.append(badge(1150, 278, "Blocked from Apply", fill_color=PALETTE["amber_bg"], border_color=PALETTE["amber"], width=162))
    body.append(text(1150, 334, "Proof snapshot", size=14, weight=700, fill=PALETTE["muted"]))
    body.append(badge(1150, 352, "Docs partial", fill_color=PALETTE["blue_bg"], border_color=PALETTE["blue"], width=116))
    body.append(badge(1278, 352, "Policy none", fill_color=PALETTE["slate_bg"], border_color=PALETTE["slate"], width=112))
    body.append(badge(1150, 396, "VM partial", fill_color=PALETTE["blue_bg"], border_color=PALETTE["blue"], width=104))
    body.append(badge(1268, 396, "Trace pending", fill_color=PALETTE["slate_bg"], border_color=PALETTE["slate"], width=122))
    body.append(badge(1402, 396, "RE partial", fill_color=PALETTE["blue_bg"], border_color=PALETTE["blue"], width=104))
    body.append(text(1150, 458, "What we know", size=14, weight=700, fill=PALETTE["muted"]))
    body.append(paragraph(1150, 486, ["The value is writable,", "reversible, and visible."], size=15, fill=PALETTE["soft"]))
    body.append(text(1150, 530, "What we do not claim", size=14, weight=700, fill=PALETTE["muted"]))
    body.append(paragraph(1150, 558, ["Current captures still do not prove", "stable runtime behavior on current builds."], size=14, fill=PALETTE["soft"]))
    body.append(text(1150, 612, "Rollback story", size=14, weight=700, fill=PALETTE["muted"]))
    body.append(paragraph(1150, 640, ["Previous value can be restored.", "Promotion stays blocked until", "stronger runtime proof lands."], size=14, fill=PALETTE["soft"]))
    body.append(text(1150, 706, "Why it matters", size=14, weight=700, fill=PALETTE["muted"]))
    body.append(paragraph(1150, 734, ["The decision and uncertainty", "stay in the same place."], size=15, fill=PALETTE["soft"]))

    return shell(
        "Evidence drawer that teaches the model",
        "The detail view mirrors the repo language: verdict, proof snapshot, what we know, and what we still need.",
        "".join(body),
    )


def render_recovery_surface() -> str:
    body = []
    body.append(rect(348, 176, 1180, 618, fill=PALETTE["panel"], stroke=PALETTE["border"], rx=26))
    body.append(text(382, 220, "Recovery and rollback", size=20, weight=700))
    body.append(text(382, 248, "A calmer recovery surface makes the safety story feel real instead of implied.", size=15, fill=PALETTE["muted"]))

    body.append(rect(382, 286, 440, 474, fill=PALETTE["card"], stroke=PALETTE["border"], rx=22))
    body.append(text(416, 328, "Last applied change", size=20, weight=700))
    body.append(text(416, 360, "GameDVR capture policy", size=18, weight=600, fill=PALETTE["soft"]))
    body.append(badge(416, 388, "Rollback tested", fill_color=PALETTE["green_bg"], border_color=PALETTE["green"], width=138))
    body.append(text(416, 448, "Snapshot captured before apply.", size=15, fill=PALETTE["muted"]))
    body.append(text(416, 478, "Verification complete after mutation.", size=15, fill=PALETTE["muted"]))
    body.append(text(416, 508, "Restore path available without leaving the surface.", size=15, fill=PALETTE["muted"]))
    body.append(button(416, 570, "Restore previous", variant="primary", width=180))
    body.append(button(610, 570, "Open details", variant="secondary", width=150))

    body.append(rect(852, 286, 642, 216, fill=PALETTE["card"], stroke=PALETTE["border"], rx=22))
    body.append(text(886, 328, "Recovery checklist", size=20, weight=700))
    body.append(text(886, 368, "1. Preview the restore action before you run it.", size=16, fill=PALETTE["soft"]))
    body.append(text(886, 402, "2. Run rollback through the isolated elevated host.", size=16, fill=PALETTE["soft"]))
    body.append(text(886, 436, "3. Verify the value and keep the audit trail.", size=16, fill=PALETTE["soft"]))

    body.append(rect(852, 528, 642, 232, fill=PALETTE["card"], stroke=PALETTE["border"], rx=22))
    body.append(text(886, 570, "Rollback history", size=20, weight=700))
    body.append(text(886, 610, "2026-04-15 07:42  ·  Restore verified  ·  vm-safety-bench", size=15, family=MONO_STACK, fill=PALETTE["soft"]))
    body.append(text(886, 644, "2026-04-15 07:18  ·  Apply verified    ·  configuration flow", size=15, family=MONO_STACK, fill=PALETTE["soft"]))
    body.append(text(886, 678, "2026-04-15 07:16  ·  Snapshot captured  ·  pre-change export", size=15, family=MONO_STACK, fill=PALETTE["soft"]))
    body.append(badge(1288, 556, "Recovery ready", fill_color=PALETTE["green_bg"], border_color=PALETTE["green"], width=150))

    return shell(
        "Recovery surface that matches the trust model",
        "The repo already tracks rollback deeply; the product surface should make that visible and reassuring.",
        "".join(body),
        active_nav="Recovery",
    )


def render_flow_frame(step_title: str, step_index: int, callout: str) -> str:
    steps = [
        ("Preview", PALETTE["blue"], PALETTE["blue_bg"]),
        ("Apply", PALETTE["amber"], PALETTE["amber_bg"]),
        ("Verify", PALETTE["green"], PALETTE["green_bg"]),
        ("Rollback ready", PALETTE["teal"], PALETTE["teal_bg"]),
    ]

    body = []
    body.append(rect(348, 176, 1180, 618, fill=PALETTE["panel"], stroke=PALETTE["border"], rx=26))
    body.append(text(382, 220, step_title, size=24, weight=700))
    body.append(text(382, 252, callout, size=16, fill=PALETTE["muted"]))
    body.append(rect(382, 298, 1112, 250, fill=PALETTE["card"], stroke=PALETTE["border"], rx=22))
    body.append(text(416, 340, "GameDVR capture policy", size=22, weight=700))
    body.append(text(416, 372, "Verdict is visible before the user commits to anything.", size=16, fill=PALETTE["soft"]))
    body.append(badge(416, 402, "Docs ready", fill_color=PALETTE["green_bg"], border_color=PALETTE["green"], width=108))
    body.append(badge(536, 402, "Runtime ready", fill_color=PALETTE["green_bg"], border_color=PALETTE["green"], width=128))
    body.append(badge(678, 402, "Source ready", fill_color=PALETTE["green_bg"], border_color=PALETTE["green"], width=118))
    body.append(badge(810, 402, "Rollback ready", fill_color=PALETTE["green_bg"], border_color=PALETTE["green"], width=132))
    body.append(text(416, 458, "Risk: Low-risk surface with the standard preview and verify flow.", size=14, fill=PALETTE["muted"]))
    body.append(text(416, 488, "Rollback: Verified via vm-safety-bench.", size=14, fill=PALETTE["muted"]))
    body.append(button(1310, 332, "Preview", variant="secondary"))
    body.append(button(1310, 386, "Apply", variant="primary"))
    body.append(button(1310, 440, "Restore", variant="secondary"))

    step_x = 432
    for index, (label, accent, accent_bg) in enumerate(steps):
        active = index == step_index
        fill_color = accent_bg if active else PALETTE["slate_bg"]
        border_color = accent if active else PALETTE["slate"]
        body.append(rect(step_x + index * 250, 620, 210, 82, fill=fill_color, stroke=border_color, rx=18))
        body.append(text(step_x + 105 + index * 250, 654, label, size=18, weight=700, anchor="middle"))
        state_text = "Current focus" if active else "Queued"
        body.append(text(step_x + 105 + index * 250, 684, state_text, size=13, fill=PALETTE["muted"], anchor="middle"))

    return shell("Preview -> Apply -> Verify -> Rollback", "A short storyboard GIF teaches the app's safest path before a user scrolls.", "".join(body))


def write_svg(name: str, content: str) -> Path:
    path = PRODUCT_DIR / name
    path.write_text(content, encoding="utf-8")
    return path


def render_gif_from_frames(frame_paths: list[Path], output_path: Path) -> None:
    TEMP_DIR.mkdir(parents=True, exist_ok=True)
    png_paths = []

    try:
        for frame_path in frame_paths:
            png_path = TEMP_DIR / f"{frame_path.stem}.png"
            subprocess.run(
                ["magick", str(frame_path), "-resize", "1200x675", str(png_path)],
                check=True,
                cwd=ROOT,
            )
            png_paths.append(png_path)

        subprocess.run(
            [
                "magick",
                *[str(path) for path in png_paths],
                "-delay",
                "110",
                "-loop",
                "0",
                str(output_path),
            ],
            check=True,
            cwd=ROOT,
        )
    finally:
        if TEMP_DIR.exists():
            for child in TEMP_DIR.iterdir():
                child.unlink()
            TEMP_DIR.rmdir()


def main() -> None:
    PRODUCT_DIR.mkdir(parents=True, exist_ok=True)

    write_svg("configuration-verdict-card.svg", render_configuration_verdict_card())
    write_svg("evidence-detail-drawer.svg", render_evidence_detail_drawer())
    write_svg("recovery-surface.svg", render_recovery_surface())

    frame_paths = [
        write_svg(
            "preview-flow-step-01-preview.svg",
            render_flow_frame(
                "Preview before apply",
                0,
                "The user sees the verdict, proof snapshot, and risk story before the action becomes real.",
            ),
        ),
        write_svg(
            "preview-flow-step-02-apply.svg",
            render_flow_frame(
                "Apply through the isolated host",
                1,
                "Mutation happens deliberately through the elevated host instead of inside the browsing shell.",
            ),
        ),
        write_svg(
            "preview-flow-step-03-verify.svg",
            render_flow_frame(
                "Verify the result immediately",
                2,
                "The safe path ends with a verification pass, not with a hope that the registry write meant what it claimed.",
            ),
        ),
        write_svg(
            "preview-flow-step-04-rollback.svg",
            render_flow_frame(
                "Keep rollback visible",
                3,
                "Recovery stays close so the trust model feels operational, not theoretical.",
            ),
        ),
    ]

    render_gif_from_frames(frame_paths, PRODUCT_DIR / "preview-apply-verify-rollback.gif")


if __name__ == "__main__":
    main()
