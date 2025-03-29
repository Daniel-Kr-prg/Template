# 🎮 Unity Project Template

A clean and maintainable **Unity project template**, designed to speed up initial setup and ensure consistency across projects.  
It contains preconfigured project settings, addressables, package dependencies, and asset folders — all ready to go!

> ⚙️ Perfect for **studio pipelines**, **individual projects**, or **collaborative setups**.

---

## 🧹 Integrate as Git Submodule

This template is meant to be added to any Unity project as a **Git submodule**.  
It allows you to keep the template logic versioned and separate from the main project.

### 📦 Add to your Unity project:

```bash
git submodule add <your-git-repo-url> Assets/Template
git submodule update --init --recursive
```

After that, you’ll see the template in `Assets/Template`.

---

## 🚀 Initial Setup

**For initial setup use `setup.bat`. It will copy all settings and initial files into the project!**

To initialize your project with the template defaults, just run:

```bash
Assets/Template/setup.bat
```

This script will:

- Copy **ProjectSettings**
- Merge and update **Packages/manifest.json**
- Sync **packages-lock.json**
- Copy `.gitignore`
- Copy `Settings/` and `AddressableAssetsData/` into your `Assets/`

> ⚠️ **Warning**: This operation will **overwrite existing settings**. You will be prompted before proceeding.

---

## 📁 Folder Structure

```
Assets/
├── Template/               ← Template submodule lives here
├── Settings/               ← Synced from template
├── AddressableAssetsData/  ← Synced from template
```

---

## 🧼 Clean & Modular

This template avoids unnecessary clutter. All config logic is encapsulated in one place (`setup.bat`) and reusable across multiple Unity projects.

---

## 💡 Tips

- Re-run `setup.bat` anytime you want to re-sync project settings.
- Update the submodule when the template gets improvements:
  ```bash
  cd Assets/Template
  git pull origin main
  cd ../..
  ```

---

Happy coding! 🎯

