# Contributing

1. Use a short-lived branch and never use real patient data in development or tests.
2. Build with warnings as errors and run `Mercury.Tests`.
3. Check keyboard-only operation, 200% scaling, High Contrast, printing, draft recovery, and update failure behaviour.
4. Open a pull request and wait for Quality Gate and CodeQL.
5. Update `CHANGELOG.md`; merge only after review.

For a release, update the version, confirm trusted signing secrets, tag `vMAJOR.MINOR.PATCH`, and verify signature, checksum, provenance, clean startup, and rollback before deployment.
