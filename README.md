# HOTFI

HOTFI یک اپلیکیشن دسکتاپ برای ویندوز است که مدیریت هات‌اسپات موبایل (Mobile Hotspot) را ساده و یک‌کلیکی می‌کند: روشن/خاموش کردن سریع، تنظیم نام و رمز شبکه، اتصال آنی با اسکن QR، و نمایش زنده‌ی دستگاه‌های متصل و میزان ترافیک مصرفی.

## امکانات

- ⚡ روشن/خاموش کردن هات‌اسپات با یک کلیک
- 🔐 تنظیم نام شبکه (SSID) و رمز عبور
- 📱 لیست دستگاه‌های متصل به همراه نام، آدرس IP، آدرس MAC و زمان اتصال
- 📊 نمایش سرعت لحظه‌ای و مجموع دیتای آپلود/دانلود مصرفی
- 🔗 اتصال خودکار با اسکن QR کد (بدون نیاز به وارد کردن دستی رمز)
- 🎨 طراحی Aura با تم کهکشانی و پشتیبانی کامل از راست‌به‌چپ
- 📦 نصب تک‌فایلی و خودکار (شورتکات دسکتاپ و Start Menu به‌صورت خودکار ساخته می‌شود)

## تصاویر

| خانه | دستگاه‌های متصل |
|---|---|
| ![Home](screenshots/home.png) | ![Devices](screenshots/devices.png) |

| تنظیمات | اتصال با QR |
|---|---|
| ![Settings](screenshots/settings.png) | ![QR](screenshots/qr.png) |

## نصب

1. آخرین نسخه‌ی `HOTFI-Setup.exe` را از بخش [Releases](../../releases) دانلود کنید.
2. آن را اجرا کنید — برنامه به‌صورت خودکار در مسیر کاربری شما نصب شده و شورتکات دسکتاپ/استارت‌منو می‌سازد.
3. از طریق شورتکات `HOTFI` اجرا کنید.

## ساخت از سورس

```bash
dotnet publish -c Release -r win-x64 --self-contained true ^
  -p:PublishSingleFile=true ^
  -p:IncludeNativeLibrariesForSelfExtract=true ^
  -p:EnableCompressionInSingleFile=true ^
  -o publish
```

## تکنولوژی

- .NET 8 / WPF
- Windows.Networking.NetworkOperators (Mobile Hotspot API)
- QRCoder
- فونت Vazirmatn

---

اگه این پروژه به دردتون خورد، یه ⭐ بدید!
