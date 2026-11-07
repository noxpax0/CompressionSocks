# Threat model

Protected assets are patient/order content, update integrity, and operator workflow. Threats include local file access, partial writes, malicious release replacement, unsafe navigation, oversized WebView messages, accidental clipboard/print/WhatsApp disclosure, and unsupported dependencies.

Controls include Windows-user DPAPI encryption, atomic replacement and backup, payload limits, strict message kinds and URL allowlists, SHA-256 plus Authenticode trust, update rollback, single-instance access, dependency auditing, CodeQL, signed releases, and least-privilege workflows.

Residual risks include compromise of the active Windows account, screenshots, printer/clipboard history, recipient mistakes, physical access, and WebView2 vulnerabilities. Mercury cannot protect data after the operating system or user account is compromised.
