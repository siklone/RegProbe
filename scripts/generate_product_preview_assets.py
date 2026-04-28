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
    "shell_top": "#111923",
    "shell_status": "#0d131b",
    "sidebar": "#0a0a0a",
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
    "red": "#c0392b",
    "red_bg": "#22110f",
    "silver": "#c0c0c0",
    "bronze": "#cd7f32",
    "gold": "#ffd700",
    "rail_blue": "#3498db",
    "rail_green": "#2ecc71",
    "rail_orange": "#e67e22",
    "rail_purple": "#9b59b6",
    "rail_teal": "#1abc9c",
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
        fill_color = PALETTE["red"]
        border_color = PALETTE["red"]
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


def chip(x: int, y: int, label: str, *, fill_color: str, border_color: str,
         text_color: str | None = None, width: int | None = None) -> str:
    chip_width = width or max(86, 20 + len(label) * 8)
    return "".join(
        [
            rect(x, y, chip_width, 30, fill=fill_color, stroke=border_color, rx=15),
            text(
                x + chip_width // 2,
                y + 20,
                label,
                size=12,
                weight=600,
                fill=text_color or PALETTE["soft"],
                anchor="middle",
                family=MONO_STACK,
            ),
        ]
    )


def top_tab(x: int, y: int, label: str, *, active: bool = False) -> str:
    fill_color = "#162232" if active else "transparent"
    stroke_color = "#56728f" if active else "transparent"
    text_color = PALETTE["text"] if active else "#95a4b7"
    return "".join(
        [
            rect(x, y, 128, 40, fill=fill_color, stroke=stroke_color, rx=20),
            text(x + 64, y + 25, label, size=15, weight=600, fill=text_color, anchor="middle"),
        ]
    )


def traffic_button(x: int, y: int, color: str, glyph: str) -> str:
    return "".join(
        [
            rect(x, y, 18, 18, fill=color, stroke=None, rx=9),
            text(x + 9, y + 13, glyph, size=10, weight=700, fill="#081016", anchor="middle"),
        ]
    )


def status_token(x: int, y: int, label: str, *, tone: str = "neutral") -> str:
    tone_map = {
        "ok": (PALETTE["green_bg"], PALETTE["green"], "#d9f9e6"),
        "info": (PALETTE["blue_bg"], PALETTE["blue"], "#dce9ff"),
        "warning": (PALETTE["amber_bg"], PALETTE["amber"], "#ffe5b8"),
        "neutral": ("#151c25", "#253345", "#d6e0ee"),
    }
    fill_color, border_color, text_color = tone_map[tone]
    width = max(86, 22 + len(label) * 8)
    return "".join(
        [
            rect(x, y, width, 26, fill=fill_color, stroke=border_color, rx=13),
            text(x + width // 2, y + 18, label, size=11, weight=600, fill=text_color, anchor="middle"),
        ]
    )


def rail_item(x: int, y: int, label: str, count: str, *, color: str, active: bool = False) -> str:
    parts = [rect(x, y, 206, 40, fill="#111111" if active else "transparent", stroke=PALETTE["red"] if active else "transparent", rx=12)]
    if active:
        parts.append(rect(x + 2, y + 9, 4, 22, fill=PALETTE["red"], stroke=None, rx=2))
    parts.append(rect(x + 16, y + 16, 8, 8, fill=color, stroke=None, rx=4))
    parts.append(text(x + 36, y + 26, label, size=11, weight=700, fill=PALETTE["text"] if active else PALETTE["muted"], family=MONO_STACK))
    parts.append(rect(x + 154, y + 10, 40, 20, fill="#1a1a1a", stroke="#262626", rx=10))
    parts.append(text(x + 174, y + 24, count, size=10, weight=700, fill="#6c7684", family=MONO_STACK, anchor="middle"))
    return "".join(parts)


def stat_bar(x: int, y: int, label: str, value: str, *, fill_color: str, width: int = 230, progress: int = 70) -> str:
    bar_width = int(width * progress / 100)
    return "".join(
        [
            text(x, y, label, size=10, weight=700, fill="#758293", family=MONO_STACK),
            text(x + width, y, value, size=10, weight=700, fill="#dce4ee", family=MONO_STACK, anchor="end"),
            rect(x, y + 10, width, 6, fill="#1a1a1a", stroke=None, rx=3),
            rect(x, y + 10, bar_width, 6, fill=fill_color, stroke=None, rx=3),
        ]
    )


def tweak_card(x: int, y: int, *, category: str, category_color: str, name: str, path: str,
               status: str, tier: str, active: bool = False) -> str:
    border = PALETTE["red"] if active else "#1e1e1e"
    parts = [
        rect(x, y, 286, 148, fill="#111111", stroke=border, rx=18),
        rect(x, y, 6, 148, fill=PALETTE["red"] if active else category_color, stroke=None, rx=3),
        text(x + 18, y + 26, category, size=10, weight=800, fill=category_color, family=MONO_STACK),
        badge(x + 192, y + 10, status, fill_color="#1a1500" if status == "DRAFT" else "#0a2200", border_color="#3a3000" if status == "DRAFT" else "#1a4a00", width=80, text_color="#ffc107" if status == "DRAFT" else "#4caf50"),
        text(x + 18, y + 56, name, size=16, weight=700),
        badge(x + 236, y + 42, tier, fill_color=PALETTE["silver"] if tier == "B" else PALETTE["gold"], border_color=PALETTE["silver"] if tier == "B" else PALETTE["gold"], text_color="#000000", width=36),
        paragraph(x + 18, y + 84, [path[:38], path[38:]], size=10, fill="#5f6a78", family=MONO_STACK, line_height=16),
        chip(x + 18, y + 116, "DOCS", fill_color="#131f14", border_color="#2d6a3e", width=56),
        chip(x + 82, y + 116, "RUNTIME", fill_color="#151e2f", border_color="#365f96", width=78),
        chip(x + 168, y + 116, "SOURCE", fill_color="#1a1a1a", border_color="#3a3a3a", width=72),
        chip(x + 248, y + 116, "ROLLBACK", fill_color="#151e2f", border_color="#365f96", width=84),
    ]
    return "".join(parts)


def window_shell(body: str, *, active_top: str, status_items: list[tuple[str, str]]) -> str:
    status = []
    cursor = 72
    for label, tone in status_items:
        status.append(status_token(cursor, 124, label, tone=tone))
        cursor += max(86, 22 + len(label) * 8) + 10

    tabs = [
        top_tab(594, 38, "Tweaks", active=active_top == "Tweaks"),
        top_tab(736, 38, "Recovery", active=active_top == "Recovery"),
        top_tab(878, 38, "Diagnostics", active=active_top == "Diagnostics"),
    ]

    return f"""<svg xmlns="http://www.w3.org/2000/svg" width="{WIDTH}" height="{HEIGHT}" viewBox="0 0 {WIDTH} {HEIGHT}">
  <defs>
    <linearGradient id="pageGlow" x1="0%" y1="0%" x2="100%" y2="100%">
      <stop offset="0%" stop-color="#131922"/>
      <stop offset="100%" stop-color="#0b1015"/>
    </linearGradient>
  </defs>
  {rect(0, 0, WIDTH, HEIGHT, fill='url(#pageGlow)', stroke=None, rx=0)}
  {rect(28, 28, WIDTH - 56, HEIGHT - 56, fill=PALETTE["shell"], stroke=PALETTE["border"], rx=28)}
  {rect(28, 28, WIDTH - 56, 72, fill=PALETTE["shell_top"], stroke=None, rx=28)}
  {rect(28, 100, WIDTH - 56, 36, fill=PALETTE["shell_status"], stroke=None, rx=0)}
  {text(64, 54, "RegProbe", size=20, weight=700)}
  {chip(164, 38, "Prod", fill_color="#111a25", border_color="#243446", width=64)}
  {chip(236, 38, "Staging", fill_color="#111a25", border_color="#243446", text_color="#6f8094", width=82)}
  {rect(334, 36, 190, 34, fill="#111a25", stroke="#243446", rx=17)}
  {text(429, 58, "Windows 11 25H2 • build 26200", size=12, weight=500, fill="#9ba8ba", anchor="middle")}
  {''.join(tabs)}
  {rect(1178, 36, 164, 34, fill="#121a24", stroke="#243446", rx=17)}
  {text(1224, 58, "⌕", size=12, weight=700, fill="#d7e2ee")}
  {text(1253, 58, "Search", size=12, weight=600, fill="#d7e2ee")}
  {text(1312, 58, "Ctrl+K", size=10, weight=500, fill="#6f8094")}
  {traffic_button(1450, 44, "#febc2e", "–")}
  {traffic_button(1478, 44, "#28c840", "□")}
  {traffic_button(1506, 44, "#ff5f57", "×")}
  {''.join(status)}
  {body}
</svg>
"""


def render_configuration_verdict_card() -> str:
    body = []
    body.append(rect(48, 156, 184, 700, fill=PALETTE["sidebar"], stroke="#0e0e0e", rx=22))
    body.append(text(66, 184, "WORKSPACE", size=9, weight=700, fill="#555f6d", family=MONO_STACK))
    body.append(rail_item(54, 208, "POWER", "34", color=PALETTE["red"], active=True))
    body.append(rail_item(54, 254, "KERNEL", "18", color=PALETTE["rail_blue"]))
    body.append(rail_item(54, 300, "SYSTEM", "22", color=PALETTE["rail_green"]))
    body.append(rail_item(54, 346, "NETWORK", "41", color=PALETTE["rail_purple"]))
    body.append(rail_item(54, 392, "POLICY", "17", color=PALETTE["rail_teal"]))
    body.append(rail_item(54, 438, "PRIVACY", "29", color=PALETTE["rail_orange"]))
    body.append(text(66, 812, "• 194 visible tweaks", size=10, weight=600, fill="#6a7682", family=MONO_STACK))

    body.append(text(268, 186, "Research card workspace", size=28, weight=700))
    body.append(text(268, 214, "The shipped Tweaks surface now behaves like an analysis desk: category rail, proof-forward cards, and a dedicated detail sheet.", size=15, fill=PALETTE["muted"]))
    body.append(tweak_card(268, 246, category="POWER", category_color=PALETTE["red"], name="PowerRequestOverride", path=r"HKLM\SYSTEM\CurrentControlSet\Control\Power", status="PROMOTED", tier="B", active=True))
    body.append(tweak_card(268, 404, category="KERNEL", category_color=PALETTE["rail_blue"], name="ForceBugcheckForDpcWatchdog", path=r"HKLM\SYSTEM\CurrentControlSet\Session Manager\Kernel", status="DRAFT", tier="B"))
    body.append(tweak_card(268, 562, category="SYSTEM", category_color=PALETTE["rail_green"], name="GlobalTimerResolutionRequests", path=r"HKLM\SYSTEM\CurrentControlSet\Session Manager\Kernel", status="DRAFT", tier="B"))

    body.append(rect(584, 246, 960, 528, fill="#080808", stroke="#1a1a1a", rx=24))
    body.append(text(620, 286, "POWER • Request Override", size=11, weight=800, fill=PALETTE["red"], family=MONO_STACK))
    body.append(text(620, 322, "PowerRequestOverride", size=25, weight=700))
    body.append(badge(1260, 296, "PROMOTED", fill_color="#0a2200", border_color="#1a4a00", text_color="#4caf50", width=110))
    body.append(badge(1382, 296, "TIER B", fill_color=PALETTE["silver"], border_color=PALETTE["silver"], text_color="#000000", width=80))
    body.append(text(620, 360, r"HKLM\SYSTEM\CurrentControlSet\Control\Power\PowerRequestOverride", size=11, weight=500, fill="#56606b", family=MONO_STACK))
    body.append(text(620, 406, "WHAT IT DOES", size=10, weight=800, fill="#495360", family=MONO_STACK))
    body.append(paragraph(620, 434, ["Stores per-app power request override rules.", "The surface is proven as a real registry control point,", "but consumer semantics still stay one step below full runtime certainty."], size=15, fill=PALETTE["soft"], line_height=24))
    body.append(text(620, 522, "VALUES", size=10, weight=800, fill="#495360", family=MONO_STACK))
    body.append(rect(620, 542, 430, 104, fill="#0d0d0d", stroke="#1a1a1a", rx=18))
    body.append(text(644, 572, "RuleCount", size=12, weight=700, family=MONO_STACK))
    body.append(text(946, 572, "REG_DWORD", size=11, weight=600, fill="#8995a4", family=MONO_STACK, anchor="end"))
    body.append(text(644, 604, "DISABLED / ENABLED", size=12, weight=700, family=MONO_STACK))
    body.append(text(946, 604, "Observed via exact query", size=11, weight=600, fill="#8995a4", family=MONO_STACK, anchor="end"))
    body.append(text(620, 676, "PROOF", size=10, weight=800, fill="#495360", family=MONO_STACK))
    body.append(chip(620, 694, "DOCS", fill_color="#161616", border_color="#2b2b2b", width=70))
    body.append(chip(700, 694, "RUNTIME", fill_color="#1c1210", border_color=PALETTE["red"], width=94))
    body.append(chip(804, 694, "SOURCE", fill_color="#161616", border_color="#2b2b2b", width=82))
    body.append(chip(896, 694, "ROLLBACK", fill_color="#161616", border_color="#2b2b2b", width=98))
    body.append(stat_bar(1106, 430, "RUNTIME", "72", fill_color=PALETTE["red"], progress=72))
    body.append(stat_bar(1106, 474, "STATIC", "40", fill_color="#777777", progress=40))
    body.append(stat_bar(1106, 518, "COVERAGE", "55", fill_color=PALETTE["silver"], progress=55))
    body.append(stat_bar(1106, 562, "CONFIDENCE", "60", fill_color="#aaaaaa", progress=60))
    body.append(rect(1106, 622, 392, 78, fill="#110000", stroke="#3a0000", rx=16))
    body.append(text(1130, 652, "⚠ INTENTIONAL HOLD", size=13, weight=700, fill=PALETTE["red"]))
    body.append(text(1130, 682, "Consumer semantics still need a bounded runtime trace.", size=14, weight=500, fill="#c86a62"))
    body.append(rect(584, 790, 960, 66, fill="#111111", stroke="#1a1a1a", rx=18))
    body.append(text(620, 830, "PLAN LOG  ·  Preview → Apply → Verify → Rollback surfaces stay visible below the analysis sheet.", size=12, weight=600, fill="#9aa6b3", family=MONO_STACK))
    return window_shell("".join(body), active_top="Tweaks", status_items=[("Verified lane", "ok"), ("Managed risk", "info"), ("0 pending", "neutral"), ("Rollback ready", "ok")])


def render_evidence_detail_drawer() -> str:
    body = []
    body.append(rect(48, 156, 184, 700, fill=PALETTE["sidebar"], stroke="#0e0e0e", rx=22))
    body.append(text(66, 184, "WORKSPACE", size=9, weight=700, fill="#555f6d", family=MONO_STACK))
    body.append(rail_item(54, 208, "NETWORK", "41", color=PALETTE["rail_purple"], active=True))
    body.append(rail_item(54, 254, "SYSTEM", "22", color=PALETTE["rail_green"]))
    body.append(rail_item(54, 300, "PRIVACY", "29", color=PALETTE["rail_orange"]))
    body.append(text(268, 186, "Evidence-first detail sheet", size=28, weight=700))
    body.append(text(268, 214, "The right-hand panel leads with plain language, then proof tabs, then bounded uncertainty.", size=15, fill=PALETTE["muted"]))
    body.append(tweak_card(268, 246, category="NETWORK", category_color=PALETTE["rail_purple"], name="NetworkThrottlingIndex", path=r"HKLM\Software\Microsoft\Windows NT\CurrentVersion\Multimedia", status="DRAFT", tier="C", active=True))
    body.append(rect(584, 246, 960, 540, fill="#080808", stroke="#1a1a1a", rx=24))
    body.append(text(620, 286, "NETWORK • Scheduler", size=11, weight=800, fill=PALETTE["rail_purple"], family=MONO_STACK))
    body.append(text(620, 322, "NetworkThrottlingIndex", size=25, weight=700))
    body.append(badge(1254, 296, "INTENTIONAL HOLD", fill_color="#1a0000", border_color="#3a0000", text_color="#f44336", width=156))
    body.append(badge(1422, 296, "TIER C", fill_color=PALETTE["bronze"], border_color=PALETTE["bronze"], text_color="#000000", width=80))
    body.append(text(620, 356, "WHAT IT DOES", size=10, weight=800, fill="#495360", family=MONO_STACK))
    body.append(paragraph(620, 384, ["The value is writable and reversible.", "The repo has a control-surface contract and rollback story.", "What is still blocked is stable runtime behavior on current builds."], size=15, fill=PALETTE["soft"], line_height=24))
    body.append(text(620, 474, "PROOF", size=10, weight=800, fill="#495360", family=MONO_STACK))
    body.append(chip(620, 492, "DOCS", fill_color="#1c1210", border_color=PALETTE["amber"], width=70))
    body.append(chip(700, 492, "RUNTIME", fill_color="#1a1a1a", border_color="#3a3a3a", width=94))
    body.append(chip(804, 492, "SOURCE", fill_color="#151e2f", border_color=PALETTE["blue"], width=82))
    body.append(chip(896, 492, "ROLLBACK", fill_color="#151e2f", border_color=PALETTE["blue"], width=98))
    body.append(rect(620, 536, 430, 170, fill="#10161d", stroke="#243140", rx=18))
    body.append(text(646, 566, "What we know", size=14, weight=700, fill="#a9b7c8"))
    body.append(paragraph(646, 594, ["Primary docs still map the surface.", "The app path and rollback path are exact.", "Promotion stays blocked until runtime proof improves."], size=14, fill=PALETTE["soft"], line_height=22))
    body.append(rect(1078, 536, 420, 170, fill="#10161d", stroke="#243140", rx=18))
    body.append(text(1104, 566, "What we do not claim", size=14, weight=700, fill="#a9b7c8"))
    body.append(paragraph(1104, 594, ["No exact runtime trace on 25H2 yet.", "No semantics overclaim from source mirrors.", "Research-only language stays close to the action."], size=14, fill=PALETTE["soft"], line_height=22))
    body.append(stat_bar(620, 724, "DOCS", "partial", fill_color=PALETTE["amber"], progress=55))
    body.append(stat_bar(890, 724, "RUNTIME", "pending", fill_color="#666666", progress=24))
    body.append(stat_bar(1160, 724, "SOURCE", "partial", fill_color=PALETTE["blue"], progress=46))
    return window_shell("".join(body), active_top="Tweaks", status_items=[("Blocked from Apply", "warning"), ("Research-only", "info"), ("Runtime pending", "warning"), ("Rollback documented", "ok")])


def render_recovery_surface() -> str:
    body = []
    body.append(rect(48, 156, 184, 700, fill=PALETTE["sidebar"], stroke="#0e0e0e", rx=22))
    body.append(text(66, 184, "RECOVERY", size=9, weight=700, fill="#555f6d", family=MONO_STACK))
    body.append(rail_item(54, 208, "ALL ACTIONS", "14", color=PALETTE["red"], active=True))
    body.append(rail_item(54, 254, "SYSTEM", "4", color=PALETTE["rail_green"]))
    body.append(rail_item(54, 300, "NETWORK", "3", color=PALETTE["rail_purple"]))
    body.append(rail_item(54, 346, "PRIVACY", "5", color=PALETTE["rail_orange"]))
    body.append(text(268, 186, "Rollback and cleanup stay first-class", size=28, weight=700))
    body.append(text(268, 214, "The Recovery surface is list-first, but it keeps rollback history and safety posture close enough to feel operational.", size=15, fill=PALETTE["muted"]))
    body.append(rect(268, 246, 620, 540, fill=PALETTE["panel"], stroke=PALETTE["border"], rx=24))
    body.append(text(300, 286, "Queued recovery actions", size=20, weight=700))
    for index, label in enumerate([
        "Restore previous GameDVR capture policy",
        "Re-enable Windows Search service",
        "Clear pending rollback snapshot",
        "Remove stale AppSmoke payload",
    ]):
        y = 326 + index * 104
        body.append(rect(300, y, 556, 84, fill="#111111", stroke="#1e1e1e", rx=18))
        body.append(text(324, y + 32, label, size=16, weight=700))
        body.append(text(324, y + 58, "Preview, execute, and verify from the same calm queue.", size=13, fill="#9aa6b3"))
        body.append(chip(710, y + 20, "ROLLBACK", fill_color="#151e2f", border_color=PALETTE["blue"], width=98))
    body.append(rect(918, 246, 626, 540, fill=PALETTE["panel"], stroke=PALETTE["border"], rx=24))
    body.append(text(950, 286, "Recovery history", size=20, weight=700))
    body.append(badge(1338, 262, "READY", fill_color=PALETTE["green_bg"], border_color=PALETTE["green"], text_color="#4caf50", width=88))
    history = [
        "2026-04-28 18:03  ·  Preview captured  ·  vm-safety-bench",
        "2026-04-28 18:04  ·  Apply verified    ·  elevated host",
        "2026-04-28 18:06  ·  Rollback tested   ·  restore path",
        "2026-04-28 18:08  ·  Cleanup finished  ·  queue cleared",
    ]
    for idx, row in enumerate(history):
        body.append(text(950, 336 + idx * 38, row, size=14, family=MONO_STACK, fill=PALETTE["soft"]))
    body.append(rect(950, 520, 562, 164, fill="#10161d", stroke="#243140", rx=18))
    body.append(text(976, 552, "Why this matters", size=14, weight=700, fill="#a9b7c8"))
    body.append(paragraph(976, 580, ["RegProbe already tracks rollback deeply in the repo.", "The refreshed Recovery surface now lets that trust model stay visible in-product.", "Users should not have to imagine whether undo exists."], size=15, fill=PALETTE["soft"], line_height=24))
    body.append(button(950, 716, "Restore previous", variant="primary", width=176))
    body.append(button(1138, 716, "Open log", variant="secondary", width=120))
    body.append(button(1268, 716, "Cleanup", variant="secondary", width=120))
    return window_shell("".join(body), active_top="Recovery", status_items=[("Recovery", "neutral"), ("14 actions", "neutral"), ("Rollback ready", "ok"), ("No blockers", "ok")])


def render_diagnostics_surface() -> str:
    body = []
    body.append(text(72, 186, "Diagnostics keeps the shell honest", size=28, weight=700))
    body.append(text(72, 214, "Version, runtime context, repository pointer, and log access live in one calm utility page.", size=15, fill=PALETTE["muted"]))
    body.append(rect(72, 248, 1456, 186, fill=PALETTE["panel"], stroke=PALETTE["border"], rx=24))
    body.append(rect(100, 278, 144, 144, fill="#111111", stroke="#242424", rx=18))
    body.append(text(172, 360, "RP", size=42, weight=800, fill="#f3f7fd", anchor="middle", family=MONO_STACK))
    body.append(text(282, 314, "RegProbe", size=26, weight=700))
    body.append(text(282, 348, "Evidence-backed Windows configuration work, packaged as a calmer desktop shell.", size=16, fill=PALETTE["soft"]))
    body.append(chip(282, 376, "Reversible", fill_color="#111111", border_color="#232323", width=96))
    body.append(chip(390, 376, "Logged", fill_color="#111111", border_color="#232323", width=72))
    body.append(chip(474, 376, "Research-backed", fill_color="#111111", border_color="#232323", width=128))
    body.append(rect(72, 460, 720, 360, fill=PALETTE["panel"], stroke=PALETTE["border"], rx=24))
    body.append(text(104, 500, "Version and host context", size=20, weight=700))
    metrics = [("Version", "v2.0.0"), ("Runtime", ".NET 8.0 / win-x64"), ("Framework", "WPF MVVM"), ("Architecture", "x64 desktop shell"), ("Log size", "128 KB")]
    for idx, (label, value) in enumerate(metrics):
        y = 540 + idx * 54
        body.append(line(104, y - 12, 760, y - 12, stroke="#1f2b37"))
        body.append(text(104, y, label, size=12, weight=700, fill="#9aa6b3", family=MONO_STACK))
        body.append(text(280, y, value, size=14, weight=600, fill=PALETTE["soft"]))
    body.append(rect(824, 460, 704, 164, fill=PALETTE["panel"], stroke=PALETTE["border"], rx=24))
    body.append(text(856, 500, "Repository", size=20, weight=700))
    body.append(paragraph(856, 534, ["Canonical source, release history, and implementation context stay one click away.", "This page is a utility surface, not a marketing page."], size=15, fill=PALETTE["soft"], line_height=24))
    body.append(text(856, 602, "https://github.com/siklone/RegProbe", size=13, weight=600, fill="#9aa6b3", family=MONO_STACK))
    body.append(button(1328, 566, "Open repository", variant="secondary", width=164))
    body.append(rect(824, 656, 704, 164, fill=PALETTE["panel"], stroke=PALETTE["border"], rx=24))
    body.append(text(856, 696, "Logs", size=20, weight=700))
    body.append(paragraph(856, 730, ["Recent commands, detections, and failures are easy to open when something feels off.", "Diagnostics stays useful without turning the shell into a dashboard zoo."], size=15, fill=PALETTE["soft"], line_height=24))
    body.append(button(1328, 734, "Open tweak log", variant="primary", width=164))
    return window_shell("".join(body), active_top="Diagnostics", status_items=[("Diagnostics", "neutral"), ("v2.0.0", "neutral"), ("Windows 11 25H2", "info"), ("Logs available", "ok")])


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

    return window_shell(
        "".join(body),
        active_top="Tweaks",
        status_items=[("Preview lane", "info"), ("Managed risk", "info"), ("Verification", "ok"), ("Rollback ready", "ok")],
    )


def render_png_from_svg(svg_path: Path, png_path: Path) -> None:
    subprocess.run(
        ["magick", str(svg_path), "-resize", "1600x900", str(png_path)],
        check=True,
        cwd=ROOT,
    )


def write_svg(name: str, content: str) -> Path:
    path = PRODUCT_DIR / name
    path.write_text(content, encoding="utf-8")
    png_path = path.with_suffix(".png")
    render_png_from_svg(path, png_path)
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
    write_svg("diagnostics-surface.svg", render_diagnostics_surface())

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
