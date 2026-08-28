document.addEventListener("DOMContentLoaded", () => {
  const navToggle = document.querySelector("[data-nav-toggle]");
  const closeNavigation = () => {
    document.body.classList.remove("nav-open");
    navToggle?.setAttribute("aria-expanded", "false");
  };
  navToggle?.addEventListener("click", () => {
    const isOpen = document.body.classList.toggle("nav-open");
    navToggle.setAttribute("aria-expanded", String(isOpen));
  });
  document.querySelectorAll(".nav-links a").forEach((link) => link.addEventListener("click", closeNavigation));

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

  let cart = [];
  try {
    const savedCart = JSON.parse(localStorage.getItem("craftisma-cart") || "[]");
    if (Array.isArray(savedCart)) cart = savedCart.slice(0, 20);
  } catch {
    cart = [];
  }
  const cartCount = document.querySelector("[data-cart-count]");
  const cartItems = document.querySelector("[data-cart-items]");
  const cartDrawer = document.querySelector(".cart-drawer");

  const renderCart = () => {
    if (!cartItems || !cartCount) return;
    cartCount.textContent = String(cart.length);
    localStorage.setItem("craftisma-cart", JSON.stringify(cart));
    if (!cart.length) {
      cartItems.innerHTML = '<div class="empty-cart"><span>0</span><h3>Your bag is empty</h3><p>Pick something distinctive to get started.</p></div>';
      return;
    }
    cartItems.innerHTML = cart.map((item, index) => `
      <div class="cart-line">
        <div><strong>${item.name}</strong><span>${item.price}</span></div>
        <b>${index + 1}</b>
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
    if (event.key === "Escape") {
      closeCart();
      closeNavigation();
    }
  });

  document.querySelectorAll("[data-add]").forEach((button) => {
    button.addEventListener("click", () => {
      cart.push({ name: button.dataset.add, price: button.dataset.price });
      renderCart();
      showToast(`${button.dataset.add} was added to your bag.`);
    });
  });

  const preview = document.querySelector(".custom-preview");
  document.querySelectorAll(".swatches").forEach((group) => {
    group.querySelectorAll(".swatch").forEach((swatch) => {
      swatch.addEventListener("click", () => {
        group.querySelectorAll(".swatch").forEach((item) => item.classList.remove("is-active"));
        swatch.classList.add("is-active");
        if (swatch.dataset.color) preview?.style.setProperty("--piece-color", swatch.dataset.color);
        const selectedColor = document.querySelector("[data-selected-color]");
        if (selectedColor && swatch.dataset.colorName) selectedColor.textContent = swatch.dataset.colorName;
      });
    });
  });

  const galleryMain = document.querySelector("[data-gallery-main]");
  const galleryMainImage = document.querySelector("[data-gallery-main-image]");
  const galleryLabel = document.querySelector("[data-view-label]");
  document.querySelectorAll("[data-gallery-image]").forEach((button) => {
    button.addEventListener("click", () => {
      document.querySelectorAll("[data-gallery-image]").forEach((item) => item.classList.remove("is-active"));
      button.classList.add("is-active");
      if (galleryMainImage && button.dataset.galleryImage) galleryMainImage.src = button.dataset.galleryImage;
      galleryMain?.classList.add("is-changing");
      setTimeout(() => galleryMain?.classList.remove("is-changing"), 220);
      if (galleryLabel) galleryLabel.textContent = button.dataset.galleryLabel || "PRODUCT VIEW";
    });
  });

  document.querySelector("[data-contact-form]")?.addEventListener("submit", (event) => {
    event.preventDefault();
    event.currentTarget.reset();
    showToast("Thanks — your demo inquiry is ready to send.");
  });

  renderCart();
});
