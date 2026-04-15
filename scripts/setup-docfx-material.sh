#!/usr/bin/env bash
set -euo pipefail

repo_root="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"
tmp_dir="${TMPDIR:-/tmp}/docfx-material"

echo "Installing docfx-material template into ${repo_root}/templates/material"
rm -rf "${repo_root}/templates/material" "${tmp_dir}"
git clone --depth 1 https://github.com/ovasquez/docfx-material.git "${tmp_dir}"
mkdir -p "${repo_root}/templates"
cp -R "${tmp_dir}/material" "${repo_root}/templates/material"

echo "docfx-material template installed."