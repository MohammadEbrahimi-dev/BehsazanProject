<div dir="rtl">

# بهسازان

[خانه](README.md) | [English](README.en.md) | **فارسی**

سامانه تحت وب برای مدیریت مشتری، پروژه، فاکتور، واریزی و پیگیری مالی پروژه.

## مسئله‌ای که حل می‌شود

قبل از این سامانه، کار روزمره روی فایل Excel، ورود دستی داده، ساخت دستی فاکتور و تهیه جداگانه PDF بود. پیگیری مانده مالی هر پروژه سخت بود و کار به حضور یک نفر مسلط وابسته می‌ماند.

بهسازان این گردش‌کار پراکنده را با یک سامانه متمرکز تحت مرورگر جایگزین می‌کند:

```text
مشتری ← پروژه ← فاکتور ← پرداخت (واریزی) ← دفتر کل پروژه / گزارش مالی
```

این نرم‌افزار ERP یا حسابداری سازمانی کامل نیست. برای این کارها ساخته شده است:

- مدیریت مشتریان و شماره تماس
- مدیریت پروژه‌ها (نوع تیرچه و آدرس)
- صدور فاکتور با اقلام، شماره‌گذاری خودکار، خروجی Excel و PDF
- ثبت واریزی روی پروژه
- دفتر کل پروژه (بدهکار / بستانکار / مانده)
- داشبورد و نمودارهای مالی

## تکنولوژی‌ها

| بخش | تکنولوژی |
|---|---|
| زبان و بستر | C# و .NET 9 |
| رابط وب | ASP.NET Core و Blazor Server |
| کامپوننت UI | MudBlazor (فارسی و راست‌به‌چپ) |
| معماری | Clean Architecture (Domain، Application، Infrastructure، Presentation) |
| پایگاه داده | SQL Server و Entity Framework Core 9 |
| ورود | JWT و BCrypt |
| Excel | ClosedXML |
| PDF | QuestPDF |

## پیش‌نیازها

- نصب [.NET 9 SDK](https://dotnet.microsoft.com/download)
- نصب SQL Server (LocalDB، Express یا نسخه کامل)
- ابزار EF Core برای migration:

```bash
dotnet tool install --global dotnet-ef
```

## ساخت و اجرا (محلی)

۱. مخزن را کلون کنید و در ریشه پروژه ترمینال باز کنید.

۲. رشته اتصال SQL Server را در `src/Presentation/appsettings.json` تنظیم کنید:

```json
"ConnectionStrings": {
  "DefaultConnection": "Server=.;Database=BehsazanDb;Trusted_Connection=True;MultipleActiveResultSets=true;TrustServerCertificate=True"
}
```

اگر نام اینستنس فرق دارد، `Server=.` را عوض کنید (مثلاً `Server=.\\SQLEXPRESS`).

۳. دیتابیس را بسازید و migration را اعمال کنید:

```bash
dotnet ef database update --project src/Infrastructure --startup-project src/Presentation
```

۴. برنامه را اجرا کنید:

```bash
dotnet run --project src/Presentation
```

یا فایل `Behsazan.slnx` را در Visual Studio باز کنید و پروفایل **https** را اجرا کنید.

۵. آدرس‌ها:

- HTTP: `http://localhost:5214`
- HTTPS: `https://localhost:7172`

۶. ورود (کاربر پیش‌فرض در اولین اجرا ساخته می‌شود):

| نام کاربری | رمز عبور |
|---|---|
| `admin` | `Admin@123` |

در محیط واقعی این رمز را تغییر دهید.

## استقرار (Deploy)

اسکریپت خودکار یا Docker در پروژه نیست. ابتدا publish بگیرید، بعد روی IIS یا Kestrel میزبانی کنید.

### ۱. Publish

```bash
dotnet publish src/Presentation/Behsazan.Presentation.csproj -c Release -o ./publish
```

### ۲. تنظیمات Production

در پوشه publish این مقادیر را برای محیط واقعی بگذارید (یا از متغیر محیطی / `appsettings.Production.json` استفاده کنید):

- `ConnectionStrings:DefaultConnection` — SQL Server سرور
- `Jwt:Key` — یک کلید طولانی و یکتا (کلید محیط توسعه را استفاده نکنید)

Migration را روی دیتابیس سرور اعمال کنید:

```bash
dotnet ef database update --project src/Infrastructure --startup-project src/Presentation
```

### ۳. IIS (ویندوز سرور)

۱. [.NET 9 Hosting Bundle](https://dotnet.microsoft.com/download/dotnet/9.0) را نصب کنید.
۲. پوشه `publish` را روی سرور کپی کنید (مثلاً `C:\inetpub\behsazan`).
۳. در IIS یک سایت بسازید و به همان پوشه اشاره دهید.
۴. Application Pool را روی **No Managed Code** بگذارید.
۵. در صورت نیاز به پوشه دسترسی بدهید.
۶. دسترسی برنامه به SQL Server را بررسی کنید.
۷. آدرس سایت را باز کنید و وارد شوید.

### ۴. Kestrel (ساده)

روی سرور:

```bash
cd publish
dotnet Behsazan.Presentation.dll --urls "http://0.0.0.0:8080"
```

اگر HTTPS می‌خواهید، جلوی آن IIS یا Nginx بگذارید.

</div>
