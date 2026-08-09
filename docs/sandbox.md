# Sandbox الحقيقي للمزودين

## تحميل سريع من اللوحة
1. ادخل كأدمن → **المزودون**
2. اضغط **تحميل بيانات Sandbox الرسمية**
3. تأكد أن البيئة النشطة = **Test**
4. من حساب تاجر مفعّل افتح **تجربة الدفع**

## QI Gate (بطاقات)
المصدر: https://developers-gate.qi.iq/docs/api-auth/sandbox-test

| الحقل | القيمة |
|------|--------|
| Base URL | `https://uat-sandbox-3ds-api.qi.iq/api/v1` |
| Username | `paymentgatewaytest` |
| Password | `WHaNFE5C3qlChqNbAzH4` |
| Terminal ID | `237984` |

## SuperQi (Pay with SuperQi)
المصدر: https://developers-gate.qi.iq/docs/category/pay-with-superqi

SuperQi يظهر كمزود مستقل في صفحة الدفع. التكامل عبر **نفس QI Gate API** مع تمييز طريقة ALIPAY في الطلب.
بيانات التست الافتراضية هي نفسها بيانات QI Gate أعلاه (يمكن تخصيص Terminal منفصل لاحقاً).

## ZainCash PG API v2 UAT
المصدر: docs.zaincash.iq

| الحقل | القيمة |
|------|--------|
| Base URL | `https://pg-api-uat.zaincash.iq` |
| Client ID | `758055f4a8044779a35f6ceb69f858b3` |
| Client Secret | `bibLCGTxVAig5To3OLLKPJQMlRR7Pefp` |

زبون اختبار شائع: `9647802999569` / PIN `1111` / OTP `111111`

## FIB Stage
لا توجد credentials عامة — سجّل عبر https://fib.iq/integrations/web-payments/
