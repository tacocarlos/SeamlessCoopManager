using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Microsoft.Win32;

public static class SteamGameLocator
{
	/// <summary>
	/// Attempts to find the install path for a Steam game given its App ID.
	/// Returns null if not found.
	/// </summary>
	public static string? GetInstallPath(int appId)
	{
		// 1. Try the direct per-game uninstall registry entry (fastest path)
		string? path = TryGetFromUninstallKey(appId);
		if (!string.IsNullOrEmpty(path) && Directory.Exists(path))
			return path;

		// 2. Fall back to Steam library folders + manifest parsing
		string? steamPath = GetSteamInstallPath();
		if (string.IsNullOrEmpty(steamPath))
			return null;

		foreach (string libraryPath in GetLibraryFolders(steamPath))
		{
			string manifestPath = Path.Combine(
				libraryPath, "steamapps", $"appmanifest_{appId}.acf");

			if (!File.Exists(manifestPath))
				continue;

			string? installDir = ParseInstallDirFromManifest(manifestPath);
			if (string.IsNullOrEmpty(installDir))
				continue;

			string fullPath = Path.Combine(
				libraryPath, "steamapps", "common", installDir);

			if (Directory.Exists(fullPath))
				return fullPath;
		}

		return null;
	}

	private static string? TryGetFromUninstallKey(int appId)
	{
		string keyPath =
			$@"SOFTWARE\Microsoft\Windows\CurrentVersion\Uninstall\Steam App {appId}";

		using RegistryKey? key = Registry.LocalMachine.OpenSubKey(keyPath)
			?? Registry.LocalMachine.OpenSubKey(
				$@"SOFTWARE\WOW6432Node\Microsoft\Windows\CurrentVersion\Uninstall\Steam App {appId}");

		return key?.GetValue("InstallLocation") as string;
	}

	private static string? GetSteamInstallPath()
	{
		// 64-bit Steam entries live under WOW6432Node on 64-bit OS
		using RegistryKey? key =
			Registry.LocalMachine.OpenSubKey(@"SOFTWARE\WOW6432Node\Valve\Steam")
			?? Registry.LocalMachine.OpenSubKey(@"SOFTWARE\Valve\Steam")
			?? Registry.CurrentUser.OpenSubKey(@"Software\Valve\Steam");

		if (key == null)
			return null;

		return (key.GetValue("InstallPath") as string)
			?? (key.GetValue("SteamPath") as string);
	}

	private static IEnumerable<string> GetLibraryFolders(string steamInstallPath)
	{
		// The main install path is always a library folder itself
		yield return steamInstallPath;

		string vdfPath = Path.Combine(
			steamInstallPath, "steamapps", "libraryfolders.vdf");

		if (!File.Exists(vdfPath))
			yield break;

		string content = File.ReadAllText(vdfPath);

		// Match lines like: "path"		"D:\\SteamLibrary"
		var matches = System.Text.RegularExpressions.Regex.Matches(
			content, "\"path\"\\s*\"([^\"]+)\"");

		foreach (System.Text.RegularExpressions.Match match in matches)
		{
			string raw = match.Groups[1].Value.Replace(@"\\", @"\");
			if (!string.Equals(raw, steamInstallPath, StringComparison.OrdinalIgnoreCase))
				yield return raw;
		}
	}

	private static string? ParseInstallDirFromManifest(string manifestPath)
	{
		string content = File.ReadAllText(manifestPath);

		var match = System.Text.RegularExpressions.Regex.Match(
			content, "\"installdir\"\\s*\"([^\"]+)\"");

		return match.Success ? match.Groups[1].Value : null;
	}
}