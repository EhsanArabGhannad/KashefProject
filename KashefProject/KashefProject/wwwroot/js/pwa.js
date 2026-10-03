(() => {
  const standalone = window.matchMedia("(display-mode: standalone)").matches || window.navigator.standalone === true;
  const installButton = document.querySelector("[data-install-app]");
  const status = document.querySelector("[data-install-status]");
  let installPrompt;

  if (standalone) {
    document.querySelectorAll("[data-app-link]").forEach((link) => { link.hidden = true; });
    if (status) status.textContent = "You're already using the Craftisma app.";
  }

  window.addEventListener("beforeinstallprompt", (event) => {
    if (standalone || !installButton) return;
    event.preventDefault();
    installPrompt = event;
    installButton.hidden = false;
    if (status) status.textContent = "Craftisma is ready to add to your home screen.";
  });

  installButton?.addEventListener("click", async () => {
    if (!installPrompt) return;
    const prompt = installPrompt;
    installPrompt = null;
    installButton.hidden = true;
    try {
      await prompt.prompt();
      const choice = await prompt.userChoice;
      if (status) status.textContent = choice.outcome === "accepted"
        ? "Follow your browser's installation steps, then open Craftisma from your home screen."
        : "You can add Craftisma later from your browser's menu.";
    } catch {
      if (status) status.textContent = "Use your browser's menu to add Craftisma to your home screen.";
    }
  });

  window.addEventListener("appinstalled", () => {
    installPrompt = null;
    if (installButton) installButton.hidden = true;
    if (status) status.textContent = "Craftisma has been added to your home screen.";
  });

  if ("serviceWorker" in navigator && window.isSecureContext) {
    navigator.serviceWorker.register("/service-worker.js", { scope: "/", updateViaCache: "none" })
      .catch((error) => console.warn("Craftisma offline support is unavailable.", error));
  }
})();
