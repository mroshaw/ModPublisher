# Mod Publisher

## Changelog

### Unreleased

- Reference Newtonsoft.Json directly instead of the Plastic SCM copy in publishing clients and tests.
- Replace an existing GitHub release's details and all assets for the requested tag with the current publishing values and ZIP, preserving the tag.
- Moved the GitHub owner and repository into the project's ModVersionSettings asset to prevent publishing to another project's repository and allow version control. Configure the destination once per project after upgrading.
- Included the repository and requested tag in GitHub publishing errors.

### Version 1.1.1

- Changed Publishing Connections to a foldout that is collapsed by default.
- Moved the GitHub repository name from individual mods into the shared publishing connection.

### Version 1.1.0

- Added GitHub Releases publishing with ZIP asset uploads.
- Added per-mod publishing-site selection for Nexus Mods and GitHub.
- Added configurable GitHub tag and release-title templates.
- Moved publishing orchestration behind a common target interface for easier addition of more hosting sites.

### Version 1.0.0

- Initial release

