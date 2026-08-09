document.querySelectorAll(".copy").forEach(function (btn) {
  btn.addEventListener("click", function () {
    var v = btn.getAttribute("data-copy") || "";
    if (!v || v === "-" || v === "—") return;
    if (!navigator.clipboard) return;
    navigator.clipboard.writeText(v).then(function () {
      btn.classList.add("ok");
      setTimeout(function () {
        btn.classList.remove("ok");
      }, 1200);
    }).catch(function () {});
  });
});
