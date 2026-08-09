# Fynexpay

محطة دفع عراقية (Aggregator) — .NET + MySQL + Vue.

## المشاريع

| المجلد | الوصف | المنفذ الافتراضي |
|--------|--------|------------------|
| `backend/` | ASP.NET Core API + Swagger | http://localhost:5080 |
| `dashboard/` | لوحة تاجر/أدمن (عربي RTL) | http://localhost:5173 |
| `web/` | الموقع الرئيسي | http://localhost:5174 |

## تشغيل سريع

### 1) قاعدة البيانات
أنشئ قاعدة `fynexpay` على MySQL/MariaDB وحدّث `ConnectionStrings:Default` في:

`backend/src/Fynexpay.Api/appsettings.json`

### 2) API
```bash
cd backend
dotnet run --project src/Fynexpay.Api
```
Swagger: http://localhost:5080/swagger

حساب الأدمن الافتراضي بعد الـ seed:
- Email: `admin@fynexpay.iq`
- Password: `Admin@12345`

### 3) Dashboard
```bash
cd dashboard
npm install
npm run dev
```

### 4) الموقع
```bash
cd web
npm install
npm run dev -- --port 5174
```

## تدفق التاجر

1. سجّل من لوحة التحكم
2. الأدمن يفعّل التاجر ويضبط نسبة العمولة
3. التاجر ينشئ API Key
4. يستدعي `POST /v1/payments` مع `X-Api-Key`
5. بدون credentials للمزودين يعمل النظام بوضع **Mock**
6. لإكمال دفعة Mock: `POST /api/webhooks/mock/complete/{paymentId}`

## مزودو الدفع

ضع بيانات الاعتماد في `PaymentProviders` داخل appsettings:
- FIB (`ClientId`, `ClientSecret`)
- ZainCash (`MerchantId`, `Secret`, `Msisdn`)
- QI (`Username`, `Password`, `TerminalId`)

عند غياب البيانات و`UseMockWhenMissingCredentials=true` يُستخدم مزود تجريبي.
