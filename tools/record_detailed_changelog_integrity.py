"""Append SHA-256 records for protected Calradia Forge changelog revisions.

This utility never edits existing ledger lines or DOCX files. It verifies the
existing hash chain, each document digest, and Word read-only protection before
appending records for any later, unrecorded revision.
"""
from datetime import datetime, timezone
from pathlib import Path
import hashlib
import json
import os
import re
import sys
import xml.etree.ElementTree as ET
import zipfile

ROOT = Path(__file__).resolve().parents[1]
DOCS = ROOT / "docs"
LEDGER = DOCS / "CalradiaForge-Registro-Mejoras.integrity.jsonl"
LOCK = DOCS / ".CalradiaForge-Registro-Mejoras.integrity.lock"
NAME_RE = re.compile(r"CalradiaForge-Registro-Mejoras-Rev(\d{3})\.docx$")
W_NS = "http://schemas.openxmlformats.org/wordprocessingml/2006/main"


def canonical_hash(record):
    payload = {key: value for key, value in record.items() if key != "record_hash"}
    data = json.dumps(payload, sort_keys=True, separators=(",", ":"), ensure_ascii=False).encode("utf-8")
    return hashlib.sha256(data).hexdigest()


def file_hash(path):
    digest = hashlib.sha256()
    with path.open("rb") as stream:
        for chunk in iter(lambda: stream.read(1024 * 1024), b""):
            digest.update(chunk)
    return digest.hexdigest()


def require_read_only_docx(path):
    with zipfile.ZipFile(path) as package:
        settings = ET.fromstring(package.read("word/settings.xml"))
    protection = settings.find(f"{{{W_NS}}}documentProtection")
    if protection is None:
        raise ValueError(f"El DOCX no declara protección: {path.name}")
    edit = protection.get(f"{{{W_NS}}}edit")
    enforcement = protection.get(f"{{{W_NS}}}enforcement")
    if edit != "readOnly" or enforcement not in ("1", "true", "on"):
        raise ValueError(f"El DOCX no está protegido como solo lectura: {path.name}")


def read_and_verify_ledger():
    if not LEDGER.exists():
        return []
    records = []
    for number, line in enumerate(LEDGER.read_text(encoding="utf-8").splitlines(), 1):
        if not line.strip():
            continue
        record = json.loads(line)
        if record.get("record_hash") != canonical_hash(record):
            raise ValueError(f"Huella de registro incorrecta en la línea {number}")
        expected_revision = len(records) + 1
        if record.get("revision") != expected_revision:
            raise ValueError(f"Secuencia no contigua en la línea {number}: se esperaba Rev{expected_revision:03}")
        expected_name = f"CalradiaForge-Registro-Mejoras-Rev{expected_revision:03}.docx"
        if record.get("file") != expected_name:
            raise ValueError(f"La línea {number} no apunta al archivo de su revisión: {expected_name}")
        expected_previous = records[-1]["record_hash"] if records else None
        if record.get("previous_record_hash") != expected_previous:
            raise ValueError(f"La cadena no enlaza en la línea {number}")
        path = (DOCS / record["file"]).resolve()
        if path.parent != DOCS.resolve():
            raise ValueError(f"La línea {number} apunta fuera de docs")
        if not path.is_file() or file_hash(path) != record.get("sha256"):
            raise ValueError(f"El archivo no coincide con la huella registrada: {record['file']}")
        records.append(record)
    return records


def main():
    try:
        lock_fd = os.open(LOCK, os.O_CREAT | os.O_EXCL | os.O_WRONLY)
    except FileExistsError as exc:
        raise RuntimeError(f"Hay un registrador activo o un lock que requiere revisión manual: {LOCK}") from exc
    os.write(lock_fd, f"pid={os.getpid()}\n".encode("ascii"))
    os.close(lock_fd)
    try:
        append_missing_records()
    finally:
        if LOCK.exists():
            LOCK.unlink()


def append_missing_records():
    docs = {}
    for path in DOCS.glob("CalradiaForge-Registro-Mejoras-Rev*.docx"):
        match = NAME_RE.fullmatch(path.name)
        if match:
            docs[int(match.group(1))] = path
    if not docs:
        raise ValueError("No hay revisiones DOCX para registrar")

    records = read_and_verify_ledger()
    if sorted(docs) != list(range(1, max(docs) + 1)):
        raise ValueError("Hay huecos en las revisiones DOCX; se rehúsa construir una cadena ambigua")
    if len(records) > max(docs):
        raise ValueError("El ledger incluye una revisión sin DOCX correspondiente")

    previous = records[-1]["record_hash"] if records else None
    appended = []
    for revision in range(len(records) + 1, max(docs) + 1):
        path = docs[revision]
        require_read_only_docx(path)
        record = {
            "revision": revision,
            "file": path.name,
            "sha256": file_hash(path),
            "previous_record_hash": previous,
            "recorded_utc": datetime.now(timezone.utc).isoformat(timespec="seconds"),
        }
        record["record_hash"] = canonical_hash(record)
        line = json.dumps(record, ensure_ascii=False, separators=(",", ":")) + "\n"
        with LEDGER.open("a", encoding="utf-8", newline="\n") as stream:
            stream.write(line)
            stream.flush()
            os.fsync(stream.fileno())
        appended.append(record)
        previous = record["record_hash"]

    print(json.dumps({"verified_records": len(records), "appended": appended}, ensure_ascii=False, indent=2))


if __name__ == "__main__":
    try:
        main()
    except Exception as exc:
        print(f"ERROR: {exc}", file=sys.stderr)
        raise SystemExit(1)
