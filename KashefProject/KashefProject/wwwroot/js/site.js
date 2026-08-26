document.addEventListener("DOMContentLoaded", () => {
  const toFa = (value) => String(value).replace(/\d/g, (digit) => "۰۱۲۳۴۵۶۷۸۹"[digit]);
  const revealItems = document.querySelectorAll(".reveal");
  const reducedMotion = window.matchMedia("(prefers-reduced-motion: reduce)").matches;

  if (reducedMotion || !("IntersectionObserver" in window)) {
    revealItems.forEach((item) => item.classList.add("is-visible"));
  } else {
    const observer = new IntersectionObserver((entries) => {
      entries.forEach((entry) => {
        if (entry.isIntersecting) {
          entry.target.classList.add("is-visible");
          observer.unobserve(entry.target);
        }
      });
    }, { threshold: 0.13 });
    revealItems.forEach((item) => observer.observe(item));
  }

  const toast = document.querySelector("[data-toast]");
  let toastTimer;
  const showToast = (message) => {
    if (!toast) return;
    toast.textContent = message;
    toast.classList.add("is-visible");
    clearTimeout(toastTimer);
    toastTimer = setTimeout(() => toast.classList.remove("is-visible"), 2400);
  };

  const cart = [];
  const cartCount = document.querySelector("[data-cart-count]");
  const cartItems = document.querySelector("[data-cart-items]");
  const cartDrawer = document.querySelector(".cart-drawer");

  const renderCart = () => {
    if (!cartItems || !cartCount) return;
    cartCount.textContent = toFa(cart.length);
    if (!cart.length) {
      cartItems.innerHTML = '<div class="empty-cart"><span>۰</span><h3>سبدت هنوز خالیه</h3><p>یه چیز خاص برای شروع انتخاب کن.</p></div>';
      return;
    }
    cartItems.innerHTML = cart.map((item, index) => `
      <div class="cart-line">
        <div><strong>${item.name}</strong><span>${item.price}</span></div>
        <b>${toFa(index + 1)}</b>
      </div>`).join("");
  };

  const openCart = () => {
    document.body.classList.add("drawer-open");
    cartDrawer?.setAttribute("aria-hidden", "false");
  };
  const closeCart = () => {
    document.body.classList.remove("drawer-open");
    cartDrawer?.setAttribute("aria-hidden", "true");
  };

  document.querySelectorAll("[data-cart-open]").forEach((button) => button.addEventListener("click", openCart));
  document.querySelectorAll("[data-cart-close]").forEach((button) => button.addEventListener("click", closeCart));
  document.addEventListener("keydown", (event) => {
    if (event.key === "Escape") closeCart();
  });

  document.querySelectorAll("[data-add]").forEach((button) => {
    button.addEventListener("click", () => {
      cart.push({ name: button.dataset.add, price: button.dataset.price });
      renderCart();
      showToast(`${button.dataset.add} به سبد اضافه شد.`);
    });
  });

  const preview = document.querySelector(".custom-preview");
  document.querySelectorAll(".swatch").forEach((swatch) => {
    swatch.addEventListener("click", () => {
      document.querySelectorAll(".swatch").forEach((item) => item.classList.remove("is-active"));
      swatch.classList.add("is-active");
      preview?.style.setProperty("--piece-color", swatch.dataset.color);
    });
  });

  document.querySelector("[data-custom-order]")?.addEventListener("click", () => {
    showToast("فرم سفارش اختصاصی در نسخه‌ی بعدی فعال می‌شود.");
  });

  renderCart();
});
