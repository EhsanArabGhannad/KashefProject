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
  document.addEventListener("click", (event) => {
    if (!event.target.closest(".site-header")) closeNavigation();
  });
  document.addEventListener("focusin", (event) => {
    if (!event.target.closest(".site-header")) closeNavigation();
  });
  window.matchMedia("(min-width: 981px)").addEventListener("change", closeNavigation);

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
      const wasOpen = document.body.classList.contains("nav-open");
      closeNavigation();
      if (wasOpen) navToggle?.focus();
    }
  });

  const galleryMain = document.querySelector("[data-gallery-main]");
  const galleryMainImage = document.querySelector("[data-gallery-main-image]");
  const galleryLabel = document.querySelector("[data-view-label]");
  document.querySelectorAll("[data-gallery-image]").forEach((button) => {
    button.addEventListener("click", () => {
      document.querySelectorAll("[data-gallery-image]").forEach((item) => {
        item.classList.remove("is-active");
        item.setAttribute("aria-pressed", "false");
      });
      button.classList.add("is-active");
      button.setAttribute("aria-pressed", "true");
      const thumbnailImage = button.querySelector("img");
      if (galleryMainImage && thumbnailImage) {
        galleryMainImage.src = thumbnailImage.currentSrc || thumbnailImage.src;
        galleryMainImage.alt = button.getAttribute("aria-label")?.replace(/^Show /, "") || galleryMainImage.alt;
      }
      galleryMain?.classList.add("is-changing");
      setTimeout(() => galleryMain?.classList.remove("is-changing"), 220);
      if (galleryLabel) galleryLabel.textContent = button.dataset.galleryLabel || "PRODUCT VIEW";
    });
  });

  // Keep native and unobtrusive validation in charge before locking a valid submission.
  const pendingForms = new Map();
  document.addEventListener("submit", (event) => {
    const form = event.target;
    if (!form.matches("[data-submit-once]") || event.defaultPrevented) return;
    if (pendingForms.has(form)) {
      event.preventDefault();
      return;
    }
    const button = event.submitter || form.querySelector("button[type=submit]");
    if (!button || !form.checkValidity()) return;
    pendingForms.set(form, { button, content: [...button.childNodes].map((node) => node.cloneNode(true)) });
    button.disabled = true;
    button.textContent = button.dataset.pendingLabel || "Please wait…";
    form.setAttribute("aria-busy", "true");
  });
  // Back/forward restoration must leave the form usable after a redirect or payment cancellation.
  window.addEventListener("pageshow", () => {
    pendingForms.forEach(({ button, content }, form) => {
      button.disabled = false;
      button.replaceChildren(...content);
      form.removeAttribute("aria-busy");
    });
    pendingForms.clear();
  });
  const errors = document.querySelector(".commerce-shell .validation-summary-errors");
  if (errors) {
    errors.setAttribute("tabindex", "-1");
    errors.setAttribute("role", "alert");
    errors.focus();
  }
});
