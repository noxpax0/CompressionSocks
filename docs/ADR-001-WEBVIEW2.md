# ADR-001: Retain WebView2 for Mercury 2.0

Status: accepted for 2.0; review annually.

Mercury retains WinForms plus the installed WebView2 Runtime because the mature form, printing, accessibility semantics, and embedded offline assets reduce migration risk.

WebView2 uses sandboxed subprocesses and cannot meet a 50 MB total-process target. The budget is approximately 300–350 MB private memory with near-zero idle CPU. Consider a pure native rewrite only if measured deployment hardware cannot meet that budget; it requires separate accessibility and printer validation.
