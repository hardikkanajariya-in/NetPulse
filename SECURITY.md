# Security Policy

## Supported Versions

Security fixes are provided for the latest release on the `main` branch.

| Version | Supported |
| ------- | --------- |
| Latest stable | Yes |
| Older releases | No |

## Reporting a Vulnerability

If you discover a security issue, please do not open a public issue.

Use one of these channels instead:

- GitHub Security Advisories for private disclosure
- Contact the maintainer through the repository owner profile on GitHub

Please include:

- A clear description of the issue
- Steps to reproduce
- Potential impact
- Any suggested mitigation or patch

You can expect an acknowledgement within 7 days for valid reports. Confirmed issues will be investigated and fixed as quickly as practical.

## Scope

This project is a local-only desktop utility. Relevant security concerns include:

- Unsafe handling of local files or paths
- Registry modification issues
- Crashes caused by malformed or unexpected local state
- Dependency vulnerabilities

Reports about unsupported environments, local misconfiguration, or social engineering are generally out of scope unless they expose a concrete product flaw.
