# Security policy

Only the latest stable Mercury release receives security fixes. Dependencies are reviewed weekly by Dependabot and CI audit.

Report vulnerabilities through GitHub private vulnerability reporting. Never include patient data, credentials, private logs, or exploit details in a public issue. Include the Mercury and Windows versions and reproduce with synthetic data.

Mercury is a local single-user application. Its encrypted database is bound to the current Windows account. It does not replace Windows account security, full-disk encryption, endpoint protection, backups, or clinical-data governance. WhatsApp and printers become separate data processors after data is sent. Production releases must be Authenticode-signed and timestamped; Mercury rejects unsigned updates.
