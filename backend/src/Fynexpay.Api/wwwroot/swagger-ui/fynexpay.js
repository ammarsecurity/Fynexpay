(function () {
  document.documentElement.lang = "ar";
  var link = document.querySelector("link[rel*='icon']") || document.createElement("link");
  link.rel = "icon";
  link.href = "/swagger-ui/icon-logo.png";
  document.head.appendChild(link);
})();
