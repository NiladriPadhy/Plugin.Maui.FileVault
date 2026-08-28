# Changelog

## 1.0.4

- Rebrand package metadata and catalog references to MauiEssentials.


## 1.0.3

- LLM-friendly README, llms.txt, AGENTS.md, and improved NuGet title/tags for coding-agent discoverability.

## 1.0.2

- Add the NuGet package link and badge to the README

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
