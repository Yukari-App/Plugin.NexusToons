<div align="center">
  <h1>
    <img src="https://nexustoons.com/icon.png" alt="NexusToons Logo" width="46" valign="middle"/> Yukari.Plugin.NexusToons
  </h1>
</div>

<div align="center">
  
  ![GitHub Repo stars](https://img.shields.io/github/stars/Yukari-App/Plugin.NexusToons?style=for-the-badge&color=E3434F)
  ![GitHub last commit](https://img.shields.io/github/last-commit/Yukari-App/Plugin.NexusToons?style=for-the-badge&color=E6E6E6)
  ![GitHub repo size](https://img.shields.io/github/repo-size/Yukari-App/Plugin.NexusToons?style=for-the-badge&color=E3434F)
</div>

<div align="center">
    <h2>📖 Overview</h2>

**Yukari.Plugin.NexusToons** is a community plugin implementing the `IComicSource` interface from **[Yukari.Core](https://github.com/Yukari-App/Core)**.

It connects to the **[NexusToons](https://nexustoons.com)** official API with filter support (content rating, type, status, genres...).

Only **Brazilian Portuguese** language is available.

Built for the **[Yukari](https://github.com/Yukari-App/Yukari)** Windows reader app.

</div>

<div align="center">
    <h2>📚 Comic Source Installation</h2>

**Yukari** doesn't come with pre-installed **Comic Sources** for legal reasons. You add the sources you want through **community plugins**.

</div>

- Go to **Releases** and download the `.dll` file from the latest version;
  - If your installed **Yukari** is not up to date, download a **compatible** plugin.
- Inside **Yukari**, go to **Settings** and look for **Sources**;
- Click on **Add New Source** and select the downloaded `.dll` Plugin;
- Done. Now you can go to **Discover** and search for **Comics** in that source.

<div align="center">
    <h2>🗒️ Notes</h2>
</div>

- No login required.
- **Filters**: Supports content rating, type, status and genres.
- **Languages**: Only Brazilian Portuguese.
- **Page count per chapter**: The total number of pages isn't available until a chapter is opened. A `?` is shown in the chapter list until then.
- **Errors**: Returns empty results instead of crashing on API issues (e.g., invalid ID).

<div align="center">
  <h2>⚠️ Disclaimer</h2>

This is a **community plugin** and is not affiliated with, endorsed by, or sponsored by NexusToons.  
Use at your own discretion. The plugin does **not** host or distribute any copyrighted content – it merely provides an interface to access content that is publicly available on the NexusToons website.
</div>

<div align="center">
    <h2>🤝 Contributing</h2>
  
Contributions are welcome! You can help improve **Yukari.Plugin.NexusToons** in several ways:
</div>

- 🐛 **Report issues**: Found a bug or unexpected behavior? Open an [issue](../../issues) describing the problem.
- ✨ **Suggest features**: Have an idea to make **Yukari.Plugin.NexusToons** better? Share it in the issues tab.
- 🔧 **Submit pull requests**: Fix bugs, improve code quality, or add new features.

<div align="center">
  <h2>📜 License</h2>

This project is licensed under the **GPL-3.0**. See the [LICENSE](LICENSE) file for details.

</div>
