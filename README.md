# Mod Publisher

## Summary

Mod Publisher is a Unity Editor tool for managing mod versions and meta data, and publishing new and updated mods to various mod hosting platforms.

Initially, the tool supports only Nexus Mods.

Open the tool from **Tools > Mod Publisher**.

## Installation

Install the package directly from its Git repository using Unity's Package Manager:

1. Ensure Git is installed and available on the system path.
2. In Unity, open **Window > Package Manager**.
3. Open the **+** menu and select **Add package from git URL**.
4. Enter the following URL:

   ```text
   https://github.com/mroshaw/ModPublisher.git
   ```

5. Select **Add** and allow Unity to download and compile the package.
6. Open the tool from **Tools > Mod Publisher**.

When Odin Inspector is installed and `ODIN_INSPECTOR` is defined, the tool uses a dedicated `OdinEditorWindow` interface with grouped properties, native path drawers, validation, tooltips, state-aware buttons, progress, status, and a read-only report. It falls back to a complete standard Unity Editor interface when Odin is unavailable. Define `DEBUG_NO_ODIN_INSPECTOR` to force this fallback while Odin remains installed, which is useful for testing the standard interface. Both interfaces use the same centrally defined labels, descriptions, and tooltips.

## Using the Tool

1. Open **Tools > Mod Publisher**.
2. 
