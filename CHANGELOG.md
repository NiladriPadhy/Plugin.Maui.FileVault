# Changelog

## 1.0.1

- Include the Buy Me a Coffee support section in the package README

## 1.0.0

- AES-256-GCM encryption for vault files on iOS and Android
- Master key stored in the platform secure store (iOS Keychain / Android Keystore via `SecureStorage`)
- Optional passphrase wrapping with PBKDF2-SHA256
- File lifecycle: write, read, update, delete, expire, idle timeout, purge
- LRU / oldest eviction when a vault size quota is set
- Secure delete, backup exclusion, and platform file protection
- Lock on background and auto-purge on resume
- .NET MAUI support for iOS and Android (`net10.0-ios`, `net10.0-android`)
