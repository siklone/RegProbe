#!/usr/bin/env python3
"""
Automatically fix compilation errors in newly added tweaks:
1. Fix RegistryValueBatchEntry parameter order
2. Add using System.IO; where needed
3. Replace TweakStatus.PartiallyApplied with TweakStatus.Applied
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

        # Fix 1: Add using System.IO; if System.IO.Path/File/Directory is used
        if ('System.IO.Path' in content or 'System.Diagnostics' in content):
            if 'using System.IO;' not in content:
                # Add after "using System;"
                content = re.sub(
                    r'(using System;)\n',
                    r'\1\nusing System.IO;\n',
                    content
                )
                changes.append("Added using System.IO")

        # Fix 2: Replace TweakStatus.PartiallyApplied with TweakStatus.Applied
        if 'TweakStatus.PartiallyApplied' in content:
            content = content.replace('TweakStatus.PartiallyApplied', 'TweakStatus.Applied')
            changes.append("Fixed PartiallyApplied → Applied")

        # Fix 3: Fix RegistryValueBatchEntry parameter order
        # Pattern: new RegistryValueBatchEntry(
        #     RegistryHive.X,
        #     RegistryView.Y,    <- WRONG POSITION!
        #     @"KeyPath",
        # Should be:
        #     RegistryHive.X,
        #     @"KeyPath",        <- KeyPath comes second
        #     "ValueName",
        #     RegistryValueKind.Z,
        #     value,
        #     RegistryView.Y)    <- View is last (optional)

        # Find all RegistryValueBatchEntry instances
        entry_pattern = r'new RegistryValueBatchEntry\(((?:[^()]|\([^()]*\))*)\)'

        def fix_entry_params(match):
            params_str = match.group(1)

            # Split by comma, respecting nested parentheses
            params = []
            depth = 0
            current = []
            for char in params_str:
                if char == '(' or char == '<':
                    depth += 1
                    current.append(char)
                elif char == ')' or char == '>':
                    depth -= 1
                    current.append(char)
                elif char == ',' and depth == 0:
                    params.append(''.join(current).strip())
                    current = []
                else:
                    current.append(char)
            if current:
                params.append(''.join(current).strip())

            # Check if this looks like wrong order (has RegistryView as 2nd param)
            if len(params) >= 6:
                # Check if 2nd param is RegistryView
                if 'RegistryView' in params[1]:
                    # Reorder: Hive, KeyPath, ValueName, Kind, TargetValue, View
                    hive = params[0]
                    view = params[1]
                    keypath = params[2]
                    valuename = params[3]
                    kind = params[4]
                    value = params[5]

                    # New order
                    new_params = [hive, keypath, valuename, kind, value, view]
                    return f'new RegistryValueBatchEntry({", ".join(new_params)})'

            # Return unchanged if not matching pattern
            return match.group(0)

        new_content = re.sub(entry_pattern, fix_entry_params, content, flags=re.DOTALL)
        if new_content != content:
            content = new_content
            changes.append("Fixed RegistryValueBatchEntry parameter order")

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

    # Target directories
    targets = [
        'Misc',
        'Peripheral',
        'Power',
        'Commands/Cleanup'
    ]

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
