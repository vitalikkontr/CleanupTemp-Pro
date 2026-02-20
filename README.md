<div align="center"><img width="1160" height="820" alt="2026-02-20_011017" src="https://github.com/user-attachments/assets/d07db14c-4a75-4c82-bfaa-9d783d56aa79" />

# 🧹 Cleanup Temp Pro

**Красивая утилита очистки системы на WPF (.NET 8)**
</div>

</div>
<div align="center">
<a href="https://dotnet.microsoft.com/en-us/download/dotnet/8.0" target="_blank">

<img src="https://img.shields.io/badge/.NET-8.0-blue" alt=".NET 8.0">
</a>
<a href="https://www.microsoft.com/windows" target="_blank">
<img src="https://img.shields.io/badge/Windows-10%2F11-brightgreen" alt="Windows 10/11">
</a>
<a href="https://learn.microsoft.com/en-us/dotnet/desktop/winforms/" target="_blank">
<img src="https://img.shields.io/badge/UI-Windows%20Forms-orange" alt="Windows Forms">
</a>
</div>

## ✨ Возможности

- 🔍 **Автоматическое сканирование** временных файлов, кэша браузеров, корзины
- 🧹 **Одним кликом** очищает все найденные файлы
- 📊 **Красивый GUI** с тёмной темой, градиентами и анимациями
- 💽 **Все диски системы** — C:, D:, E:, флешки определяются автоматически
- 📂 **Категории очистки:**
  - Временные файлы пользователя (`%TEMP%`)
  - Windows Temp (`C:\Windows\Temp`)
  - Windows Update кэш
  - Корзина (на всех дисках)
  - Кэш Chrome, Edge, Firefox, Brave, Opera, Яндекс, Vivaldi
  - Thumbnails кэш
  - IE / Edge Cache
  - Microsoft Office кэш
  - Prefetch файлы
  - Логи событий Windows
  - Диски D:, E:, флешки — Temp, `$RECYCLE.BIN`, кэши браузеров
- 🛡️ **Защита важных данных** — мессенджеры, облачные хранилища, личные папки не затрагиваются
- 📈 **Статистика** в реальном времени
- ⏹ **Остановка** сканирования/очистки в любой момент
- 🕐 **История очисток** — последние 20 сессий с датой и объёмом

## 📥 Установка

1. [Скачайте последнюю версию](https://github.com/vitalikkontr/CleanupTemp-Pro/releases/latest)
2. Запустите установщик CleanupTemp-Professional-Setup-v3.0.exe
3. Следуйте инструкциям мастера установки

## 🚀 Запуск

### Требования
- Windows 10/11
- .NET 8 SDK или Runtime ([скачать](https://dotnet.microsoft.com/download/dotnet/8.0))

### Сборка и запуск
```bash
# Открыть папку проекта
cd CleanupTemp_Pro

# Сборка
dotnet build

# Запуск
dotnet run --project CleanupTemp_Pro/CleanupTemp_Pro.csproj
```

### Открыть в Visual Studio
Открыть `CleanupTemp_Pro.sln` в Visual Studio 2022+, нажать F5.

---

## ⚠️ Важно

> Рекомендуется запускать **от имени администратора** для очистки системных папок (Windows\Temp, Prefetch, Логи событий).

---

## 🎨 Дизайн

| Элемент | Описание |
|---|---|
| Тема | Тёмная (Dark UI) |
| Акцент | Синий / Фиолетовый / Циановый |
| Шрифт | Segoe UI |
| Эффекты | Glow, градиенты, анимации |
| Окно | Кастомное (без системной рамки) |

## 👨‍💻 Автор

Разработано
- GitHub: [@vitalikkontr](https://github.com/vitalikkontr)

## ⚖️ Лицензия

Для личного использования.

---

💡 Нашли баг? [Создайте Issue](https://github.com/vitalikkontr/CleanupTemp-Pro/issues)

⭐ Если проект был полезен, поставьте звезду на GitHub!

<div align="center">

## Сделано с ❤️ в Украине

</div>
