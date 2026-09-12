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

  document.addEventListener("keydown", (event) => {
    if (event.key === "Escape") {
      closeNavigation();
    }
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
      const thumbnailImage = button.querySelector("img");
      if (galleryMainImage && thumbnailImage) {
        galleryMainImage.src = thumbnailImage.currentSrc || thumbnailImage.src;
      }
      galleryMain?.classList.add("is-changing");
      setTimeout(() => galleryMain?.classList.remove("is-changing"), 220);
      if (galleryLabel) galleryLabel.textContent = button.dataset.galleryLabel || "PRODUCT VIEW";
    });
  });

});
