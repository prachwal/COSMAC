#!/usr/bin/env python3
"""Convert PDF reference documents to Markdown using pymupdf4llm."""

from __future__ import annotations

import argparse
import sys
from pathlib import Path

import pymupdf4llm


def convert(pdf_path: Path, output_path: Path | None = None) -> Path:
    if not pdf_path.exists():
        raise FileNotFoundError(pdf_path)

    target = output_path or pdf_path.with_suffix(".md")
    markdown = pymupdf4llm.to_markdown(str(pdf_path))
    target.write_text(markdown, encoding="utf-8")
    return target


def main(argv: list[str] | None = None) -> int:
    parser = argparse.ArgumentParser(description="Convert PDF files to Markdown.")
    parser.add_argument("pdfs", nargs="+", type=Path, help="PDF file(s) to convert")
    parser.add_argument(
        "-o",
        "--output",
        type=Path,
        help="Output .md path (only valid for a single input PDF)",
    )
    args = parser.parse_args(argv)

    if args.output and len(args.pdfs) != 1:
        parser.error("--output requires exactly one input PDF")

    for pdf in args.pdfs:
        out = convert(pdf, args.output)
        lines = out.read_text(encoding="utf-8").count("\n") + 1
        print(f"{pdf.name} -> {out} ({lines} lines)")

    return 0


if __name__ == "__main__":
    sys.exit(main())