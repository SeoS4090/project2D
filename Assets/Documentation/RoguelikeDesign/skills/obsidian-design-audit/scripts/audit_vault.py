"""Read-only checks for this project's Obsidian design notes. Python 3 stdlib."""
import argparse
from collections import Counter
import json
from pathlib import Path
import re

HIDDEN = {'.git', '.obsidian', '.agents'}
ROLES = {'기준', '검토', '현황', '기록', '운영', '목차', '템플릿', '참고자료'}
DESIGN = {'미검토', '검토 중', '규칙 결정', '일부 결정', '해당 없음'}
VALIDATION = {'미실시', '진행 중', '검증 완료', '해당 없음'}


def scalar(value):
    value = value.strip()
    if len(value) >= 2 and value[0] == value[-1] and value[0] in "\"'":
        value = value[1:-1]
    return value


def metadata(text):
    match = re.match(r'\A---\r?\n(.*?)\r?\n---(?:\r?\n|$)', text, re.S)
    if not match:
        return {}, [], text
    fields, aliases = {}, []
    in_aliases = False
    for line in match[1].splitlines():
        key = re.match(r'^(\w+):\s*(.*)$', line)
        if key:
            name, value = key.groups()
            fields[name] = scalar(value)
            in_aliases = name == 'aliases'
            if in_aliases and value.strip():
                if value.strip().startswith('['):
                    aliases.extend(scalar(x) for x in value.strip()[1:-1].split(',') if x.strip())
                else:
                    aliases.append(scalar(value))
        elif in_aliases:
            item = re.match(r'^\s+-\s+(.+)$', line)
            if item:
                aliases.append(scalar(item[1]))
    return fields, aliases, text[match.end():]


def without_examples(text, inline=True):
    """Keep line positions so diagnostics refer to original source lines."""
    result, fence = [], None
    for line in text.splitlines():
        marker = re.match(r'^\s*(?:>\s*)*(`{3,}|~{3,})', line)
        if marker:
            token = marker[1]
            if fence is None:
                fence = token
            elif token[0] == fence[0] and len(token) >= len(fence):
                fence = None
            result.append('')
        elif fence:
            result.append('')
        else:
            result.append(line)
    text = '\n'.join(result)
    text = re.sub(r'%%.*?%%|<!--.*?-->', lambda m: '\n' * m[0].count('\n'), text, flags=re.S)
    return re.sub(r'(`+)[^`\n]*?\1', '', text) if inline else text


def heading_text(value):
    value = re.sub(r'\[([^\]]+)\]\([^)]*\)', r'\1', value)
    value = re.sub(r'\[\[([^]|]+)(?:\|([^]]+))?\]\]', lambda m: m[2] or m[1], value)
    return re.sub(r'[*_`]', '', value).strip().rstrip('#').strip()


def audit(vault, files=None):
    vault = Path(vault).resolve()
    if not vault.is_dir():
        raise ValueError('Vault directory does not exist')
    available = [p for p in vault.rglob('*') if p.is_file()
                 and not any(part in HIDDEN for part in p.relative_to(vault).parts)]
    notes = [p for p in available if p.suffix.lower() == '.md']
    contents = {p: p.read_text(encoding='utf-8-sig') for p in notes}
    index = {p.relative_to(vault).as_posix(): p for p in available}
    names = {}
    for p in available:
        names.setdefault(p.name, set()).add(p)
        if p.suffix.lower() == '.md':
            names.setdefault(p.stem, set()).add(p)
            for alias in metadata(contents[p])[1]:
                names.setdefault(alias, set()).add(p)
    if files is None:
        selected = [p for p in notes if p.relative_to(vault).parts[0] not in {'80 참고자료', '90 템플릿'}
                    and p.name not in {'README.md', 'AGENTS.md'}]
    else:
        selected = []
        for name in files:
            p = (vault/name).resolve()
            if not p.is_relative_to(vault) or p not in contents:
                raise ValueError(f'Not a Markdown file inside this Vault: {name}')
            parts = p.relative_to(vault).parts
            if parts[0] in {'80 참고자료', '90 템플릿'}:
                raise ValueError(f'Reference/template excluded from checks: {name}')
            selected.append(p)
    selected = sorted(set(selected))
    findings, roles = [], Counter()

    def report(p, level, code, message, line=1):
        findings.append({'file': p.relative_to(vault).as_posix(), 'line': line,
                         'level': level, 'code': code, 'message': message})

    for p in selected:
        text = contents[p]
        props, _, body = metadata(text)
        role = props.get('status', '')
        roles[role or '(없음)'] += 1
        if not props:
            report(p, 'warning', 'missing-properties', '노트 속성이 없음')
        if props.get('title') and props['title'] != p.stem:
            report(p, 'warning', 'title-filename', 'title과 파일명이 다름')
        h1 = re.search(r'^#\s+(.+)$', without_examples(body, inline=False), re.M)
        if h1 and props.get('title') and heading_text(h1[1]) != props['title']:
            report(p, 'warning', 'title-h1', 'title과 첫 H1이 다름')
        if role not in ROLES:
            report(p, 'warning', 'legacy-role', f'현재 역할 체계와 다른 status: {role or "없음"}')
        for key, allowed in [('design_status', DESIGN), ('validation_status', VALIDATION)]:
            if key in props and props[key] not in allowed:
                report(p, 'error', 'invalid-status', f'{key}: {props[key]}')
        if role == '기준' and any(x in p.stem for x in ['미정', '미확정', '미검토', '설계안']):
            report(p, 'warning', 'status-in-title', '기준 문서 제목에 검토 상태가 남음')
        cleaned = without_examples(text)
        if p.stem == '현재 검토할 기획 질문':
            # Quoted historical notes are not active checkboxes.
            for i, line in enumerate(cleaned.splitlines(), 1):
                if re.match(r'^\s*[-*]\s+\[[xX]\]', line):
                    report(p, 'warning', 'completed-active-question', '활성 질문 목록에 완료 체크가 남음', i)
        for match in re.finditer(r'\[\[([^]\n]+)\]\]', cleaned):
            raw = match[1].replace('\\|', '|').split('|')[0]
            target, _, anchor = raw.partition('#')
            line = cleaned[:match.start()].count('\n') + 1
            if not target:
                candidates = {p}
            else:
                candidates = set()
                for option in [target, target + '.md']:
                    if option in index:
                        candidates.add(index[option])
                    relative = (p.parent/option).resolve()
                    if relative.is_relative_to(vault) and relative in available:
                        candidates.add(relative)
                if not candidates:
                    candidates = names.get(target, set())
            if not candidates:
                report(p, 'error', 'missing-link', raw, line)
                continue
            if len(candidates) > 1:
                report(p, 'warning', 'ambiguous-link', raw, line)
                continue
            destination = next(iter(candidates))
            if anchor and destination in contents:
                dest = without_examples(metadata(contents[destination])[2], inline=False)
                if anchor.startswith('^'):
                    exists = bool(re.search(r'(?:^|\s)'+re.escape(anchor)+r'\s*$', dest, re.M))
                else:
                    headings = [heading_text(h) for h in re.findall(r'^\s*(?:>\s*)*#{1,6}\s+(.+)$', dest, re.M)]
                    exists = heading_text(anchor) in headings
                if not exists:
                    report(p, 'error', 'missing-anchor', raw, line)
    return {'checked_notes': len(selected), 'roles': dict(roles),
            'errors': sum(f['level'] == 'error' for f in findings),
            'warnings': sum(f['level'] == 'warning' for f in findings), 'findings': findings}


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument('--vault', required=True)
    parser.add_argument('--files', nargs='+')
    parser.add_argument('--format', choices=['text', 'json'], default='text')
    args = parser.parse_args()
    try:
        result = audit(args.vault, args.files)
    except (ValueError, OSError, UnicodeError) as exc:
        parser.error(str(exc))
    if args.format == 'json':
        print(json.dumps(result, ensure_ascii=False, indent=2))
    else:
        print(f"노트 {result['checked_notes']}개: 오류 {result['errors']}개, 경고 {result['warnings']}개")
        for item in result['findings']:
            print(f"{item['level']} {item['file']}:{item['line']} [{item['code']}] {item['message']}")
    return 1 if result['errors'] else 0


if __name__ == '__main__':
    raise SystemExit(main())
