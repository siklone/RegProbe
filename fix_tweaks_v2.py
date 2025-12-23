#!/usr/bin/env python3
"""
Fix remaining compilation errors - Phase 2
Add missing using System.Diagnostics;
"""

import os
import re
from pathlib import Path

def fix_file(filepath):
    """Fix a single C# file"""
    try:
        with open(filepath, 'r', encoding='utf-8') as f:
            content = f.read()

        original_content = content
        changes = []

        # Add using System.Diagnostics; if Process.Start is used
        if 'Process.Start' in content or 'ProcessStartInfo' in content:
            if 'using System.Diagnostics;' not in content:
                # Add after "using System;"
                content = re.sub(
                    r'(using System;)\n',
                    r'\1\nusing System.Diagnostics;\n',
                    content
                )
                changes.append("Added using System.Diagnostics")

        # Only write if something changed
        if content != original_content:
            with open(filepath, 'w', encoding='utf-8') as f:
                f.write(content)
            return True, changes

        return False, []

    except Exception as e:
        print(f"ERROR processing {filepath}: {e}")
        return False, []

def main():
    base = Path('/home/deniz/WPF-Windows-optimizer-with-safe-reversible-tweaks/WindowsOptimizer.Engine/Tweaks')

    targets = ['Commands/Cleanup']

    total_fixed = 0

    for target in targets:
        target_path = base / target
        if not target_path.exists():
            continue

        print(f"\n📁 Processing {target}/")
        print("-" * 60)

        for cs_file in target_path.glob('*.cs'):
            fixed, changes = fix_file(cs_file)
            if fixed:
                total_fixed += 1
                print(f"✅ {cs_file.name}")
                for change in changes:
                    print(f"   - {change}")

    print(f"\n{'='*60}")
    print(f"✨ Total files fixed: {total_fixed}")
    print(f"{'='*60}\n")

if __name__ == '__main__':
    main()
