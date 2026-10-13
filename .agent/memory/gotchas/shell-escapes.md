# Shell escapes (agent tooling)
Updated: 2026-10-13
Related: [tooling](../project/tooling.md)

- Bash tool + `python - <<'EOF'` heredoc: `\\` arrives in Python as `\`, so `"\\b"`, `"\\v"`, `"\\t"` become backspace, vertical tab and tab. This silently corrupted Windows paths in features.md (2026-10-13).
- Text with backslashes (Windows paths): write it with the Write/Edit tools, or put the script in a file via Write and run that file.
- After scripted text edits, scan for control characters (ord < 32 except \n, \r).
