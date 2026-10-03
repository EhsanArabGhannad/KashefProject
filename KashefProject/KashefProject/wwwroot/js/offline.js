document.querySelector("[data-retry]").addEventListener("click", () => window.location.reload());
const connection = document.querySelector("[data-connection]");
const updateConnection = () => {
  connection.textContent = navigator.onLine
    ? "Your device reports a connection. Try again to reach the shop."
    : "Your device is offline. Reconnect, then try again.";
};
window.addEventListener("online", updateConnection);
window.addEventListener("offline", updateConnection);
updateConnection();
